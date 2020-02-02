using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Infrastructure.EventSourcing.Domain;
using IFramework.Message;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace IFramework.EventStore.Redis
{
    public class EventStore : IEventStore
    {
        private readonly RedisEventStoreOptions _eventStoreOptions;
        private readonly ILogger<EventStore> _logger;
        private readonly IMessageTypeProvider _messageTypeProvider;
        private LuaScript _appendEventsLuaScript;
        private ConnectionMultiplexer _connectionMultiplexer;
        private IDatabase _db;
        private LuaScript _getEventsLuaScript;
        private LuaScript _handleEventLuaScript;

        public EventStore(IOptions<RedisEventStoreOptions> eventStoreOptions, IMessageTypeProvider messageTypeProvider, ILogger<EventStore> logger)
        {
            _eventStoreOptions = eventStoreOptions.Value;
            _messageTypeProvider = messageTypeProvider;
            _logger = logger;
        }

        private object AsyncState { get; set; }


        public async Task Connect()
        {
            _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(_eventStoreOptions.ConnectionString)
                                                                .ConfigureAwait(false);
            _db = _connectionMultiplexer.GetDatabase(_eventStoreOptions.DatabaseName, AsyncState);

            var appendEventsLuaScript = _eventStoreOptions.AppendEventsLuaScript;
            if (string.IsNullOrWhiteSpace(appendEventsLuaScript))
            {
                appendEventsLuaScript = @"
                    local aggregateCommandKey = string.format('ag:{%s}:%s', @id, @commandId)
                    local result = redis.call('JSON.GET', aggregateCommandKey, '.')
                    if result then
                        return result
                    end
                    if tonumber(@expectedVersion) < 0 then
                        redis.call('JSON.SET', aggregateCommandKey, '.', @result)                     
                    else
                        local aggregateEventKey = string.format('ag:{%s}:events', @id)
                        local expectedVersion = redis.call('ZREVRANGE', aggregateEventKey, '0', '0', 'WITHSCORES')
                        if #expectedVersion == 0 or expectedVersion[2] == @expectedVersion then
                            local newVersion = @expectedVersion + 1
                            redis.call('JSON.SET', aggregateCommandKey, '.', @result) 
                            redis.call('JSON.SET', aggregateCommandKey, '.aggregateRootEventPayloads', @aggregateRootEvents) 
                            redis.call('ZADD', aggregateEventKey, newVersion, @aggregateRootEvents)
                            return newVersion
                        else
                            return -2
                        end
                    end
                ";
            }

            _appendEventsLuaScript = LuaScript.Prepare(appendEventsLuaScript);

            var getEventsLuaScript = _eventStoreOptions.GetEventsLuaScript;
            if (string.IsNullOrWhiteSpace(getEventsLuaScript))
            {
                getEventsLuaScript = @"
                local aggregateCommandKey = string.format('ag:{%s}:%s', @aggregateId, @commandId)
                return redis.call('JSON.GET', aggregateCommandKey, '.')
                ";
            }

            _getEventsLuaScript = LuaScript.Prepare(getEventsLuaScript);

            var handleEventLuaScript = _eventStoreOptions.HandleEventLuaScript;
            if (string.IsNullOrWhiteSpace(handleEventLuaScript))
            {
                handleEventLuaScript = @"
                local result = redis.call('HSETNX', @subscriber, @eventId, @messages)
                if result == 1 then
                    return 1                
                else
                    return redis.call('HGET', @subscriber, @eventId)
                end
                ";
            }

            _handleEventLuaScript = LuaScript.Prepare(handleEventLuaScript);
        }

        public async Task<IEvent[]> GetEvents(string id, long start = 0, long? end = null)
        {
            var agEventKey = $"ag:{{{id}}}:events";
            var results = await _db.SortedSetRangeByScoreWithScoresAsync(agEventKey,
                                                                         start,
                                                                         end ?? double.PositiveInfinity)
                                   .ConfigureAwait(false);
            return results?.SelectMany(r => r.Element
                                             .ToString()
                                             .ToJsonObject<ObjectPayload[]>()
                                             .Select(ep => ep.Payload
                                                             .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as IEvent))
                          .ToArray();
        }

        public async Task AppendEvents(string id,
                                       long expectedVersion,
                                       string correlationId,
                                       object result,
                                       object sagaResult,
                                       IEvent[] aggregateRootEvents,
                                       IEvent[] applicationEvents = null)
        {
            var aggregateRootEventPayloads = GetObjectPayloads(aggregateRootEvents);
            var applicationEventsPayloads = GetObjectPayloads(applicationEvents);
            var resultPayload = GetObjectPayload(result);
            var sagaResultPayload = GetObjectPayload(sagaResult);
            var commandResultJson = new CommandResult(resultPayload,
                                                      sagaResultPayload,
                                                      applicationEventsPayloads).ToJson(useCamelCase: true);

            var parameters = new
            {
                id = (RedisKey) id,
                commandId = correlationId,
                expectedVersion,
                result = commandResultJson,
                aggregateRootEvents = aggregateRootEventPayloads.ToJson(useCamelCase: true)
            };
            var redisResult = await _db.ScriptEvaluateAsync(_appendEventsLuaScript,
                                                            parameters)
                                       .ConfigureAwait(false);
            _logger.LogDebug($"redisResult:{redisResult} aggregateRootId: {id} expectedVersion: {expectedVersion} correlationId: {correlationId} aggregateRootEvents: {parameters.aggregateRootEvents}");
            if (!redisResult.IsNull && (redisResult.Type == ResultType.BulkString || redisResult.Type == ResultType.SimpleString))
            {
                var commandResult = ((string) redisResult).ToJsonObject<CommandResult>();
                if (!string.IsNullOrWhiteSpace(commandResult.Result?.Payload))
                {
                    result = commandResult.Result
                                          .Payload
                                          .ToJsonObject(_messageTypeProvider.GetMessageType(commandResult.Result.Code));
                }

                if (!string.IsNullOrWhiteSpace(commandResult.SagaResult?.Payload))
                {
                    sagaResult = commandResult.SagaResult
                                                  .Payload
                                                  .ToJsonObject(_messageTypeProvider.GetMessageType(commandResult.SagaResult.Code));
                }

                aggregateRootEvents = commandResult.AggregateRootEventPayloads
                                                       ?.Select(ep => ep.Payload
                                                                        .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as IEvent)
                                                       .ToArray();

                applicationEvents = commandResult.ApplicationEventPayloads
                                                     ?.Select(ep => ep.Payload
                                                                      .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as IEvent)
                                                     .ToArray();


                throw new MessageDuplicatelyHandled(correlationId, id, result, sagaResult, aggregateRootEvents, applicationEvents);
            }

            if ((int) redisResult == -2)
            {
                if (expectedVersion == 0)
                {
                    throw new AddDuplicatedAggregateRoot(id);
                }
                else
                {
                    throw new DBConcurrencyException($"aggregateId:{id} expectedVersion:{expectedVersion} concurrency conflict");
                }
            }
        }

        public async Task<IEvent[]> GetEvents(string id, string commandId)
        {
            var redisResult = await _db.ScriptEvaluateAsync(_getEventsLuaScript,
                                                            new
                                                            {
                                                                aggregateId = (RedisKey) id,
                                                                commandId
                                                            })
                                       .ConfigureAwait(false);
            var commandResult = ((string) redisResult).ToJsonObject<CommandResult>();
          
            List<IEvent> events = new List<IEvent>();
            var aggregateRootEvents = commandResult.AggregateRootEventPayloads
                                               ?.Select(ep => ep.Payload
                                                                .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as IEvent)
                                               .ToArray();
            if (aggregateRootEvents != null && aggregateRootEvents.Length > 0)
            {
                events.AddRange(aggregateRootEvents);
            }
            var applicationEvents = commandResult.ApplicationEventPayloads
                                             ?.Select(ep => ep.Payload
                                                              .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as IEvent)
                                             .ToArray();
            if (applicationEvents != null && applicationEvents.Length > 0)
            {
                events.AddRange(applicationEvents);
            }
            return events.ToArray();
        }

        public async Task<(ICommand[] commands, IEvent[] events, object sagaResult)> HandleEvent(string subscriber,
                                                                                                 string eventId,
                                                                                                 ICommand[] commands,
                                                                                                 IEvent[] events,
                                                                                                 object sagaResult,
                                                                                                 object eventResult)
        {
            var commandsBody = commands.Select(c => new ObjectPayload(c, _messageTypeProvider.GetMessageCode(c.GetType())))
                                       .ToJson();
            var eventsBody = events.Select(e => new ObjectPayload(e, _messageTypeProvider.GetMessageCode(e.GetType())))
                                   .ToJson();
            var sagaResultBody = sagaResult != null ? new ObjectPayload(sagaResult, _messageTypeProvider.GetMessageCode(sagaResult.GetType())) : new ObjectPayload();
            var eventResultBody = eventResult != null ? new ObjectPayload(eventResult, _messageTypeProvider.GetMessageCode(eventResult.GetType())) : new ObjectPayload();
            var redisResult = await _db.ScriptEvaluateAsync(_handleEventLuaScript, new
                                       {
                                           subscriber = (RedisKey) subscriber,
                                           eventId,
                                           messages = new HandledEventMessages(commandsBody,
                                                                               eventsBody,
                                                                               sagaResultBody.ToJson(),
                                                                               eventResultBody.ToJson()).ToJson()
                                       })
                                       .ConfigureAwait(false);


            if (redisResult.Type == ResultType.SimpleString || redisResult.Type == ResultType.BulkString)
            {
                var handledEventMessages = ((string) redisResult).ToJsonObject<HandledEventMessages>();

                commands = handledEventMessages.Commands
                                               .ToJsonObject<ObjectPayload[]>()
                                               .Select(ep => ep.Payload
                                                               .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as ICommand)
                                               .ToArray();

                events = handledEventMessages.Events
                                             .ToJsonObject<ObjectPayload[]>()
                                             .Select(ep => ep.Payload
                                                             .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as IEvent)
                                             .ToArray();
                var sagaResultPayload = handledEventMessages.SagaResult
                                                            .ToJsonObject<ObjectPayload>();
                sagaResult = sagaResultPayload.Payload.ToJsonObject(_messageTypeProvider.GetMessageType(sagaResultPayload.Code));
            }


            return (commands, events, sagaResult);
        }


        public ObjectPayload[] GetObjectPayloads<T>(T[] enumerable)
        {
            if (enumerable?.Length > 0)
            {
                return enumerable.Select(e => new ObjectPayload(e, _messageTypeProvider.GetMessageCode(e.GetType())))
                                 .ToArray();
            }

            return null;
        }

        public ObjectPayload GetObjectPayload(object obj)
        {
            return obj != null ? new ObjectPayload(obj, _messageTypeProvider.GetMessageCode(obj.GetType())) : null;
        }

        public class HandledEventMessages
        {
            public HandledEventMessages() { }

            public HandledEventMessages(string commands, string events, string sagaResult, string eventResult)
            {
                Commands = commands;
                Events = events;
                SagaResult = sagaResult;
                EventResult = eventResult;
            }

            public string Commands { get; set; }
            public string Events { get; set; }
            public string SagaResult { get; set; }
            public string EventResult { get; set; }
        }
    }
}