using System.Data;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Infrastructure;
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
                    local aggregateCommandKey = string.format('ag:{%s}:%s', @aggregateId, @commandId)
                    local aggregateEventKey = string.format('ag:{%s}:events', @aggregateId)
                    
                    local version = redis.call('JSON.GET', aggregateCommandKey, '.version')
                    if version then
                        return redis.call('JSON.GET', aggregateCommandKey, '.')
                    end
                    local expectedVersion = redis.call('ZREVRANGE', aggregateEventKey, '0', '0', 'WITHSCORES')
                    if #expectedVersion == 0 or expectedVersion[2] == @expectedVersion then
                        local newVersion = @expectedVersion + 1
                        redis.call('JSON.SET', aggregateCommandKey, '.', @result)     
                        redis.call('ZADD', aggregateEventKey, newVersion, @events)
                        return newVersion
                    else
                        return -2
                    end
                ";
            }

            _appendEventsLuaScript = LuaScript.Prepare(appendEventsLuaScript);

            var getEventsLuaScript = _eventStoreOptions.GetEventsLuaScript;
            if (string.IsNullOrWhiteSpace(getEventsLuaScript))
            {
                getEventsLuaScript = @"
                local aggregateCommandKey = string.format('ag:{%s}:%s', @aggregateId, @commandId)
                local aggregateEventKey = string.format('ag:{%s}:events', @aggregateId) 
                local version = redis.call('JSON.GET', aggregateCommandKey, '.version')
                return redis.call('ZRANGEBYSCORE',aggregateEventKey, version, version)
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

        public async Task AppendEvents(string aggregateId, long expectedVersion, string correlationId, object commandResult, object sagaResult, params IEvent[] events)
        {
            var eventsBody = events.Select(e => new ObjectPayload(e, _messageTypeProvider.GetMessageCode(e.GetType())))
                                   .ToJson();
            var commandResultPayload = commandResult != null ? new ObjectPayload(commandResult, _messageTypeProvider.GetMessageCode(commandResult.GetType())) : new ObjectPayload();
            var sagaResultPayload = sagaResult != null ? new ObjectPayload(sagaResult, _messageTypeProvider.GetMessageCode(sagaResult.GetType())) : new ObjectPayload();
            var resultPayload = new MessageAttachment(commandResultPayload,
                                                      sagaResultPayload,
                                                      expectedVersion + 1).ToJson(useCamelCase: true);
            var redisResult = await _db.ScriptEvaluateAsync(_appendEventsLuaScript,
                                                            new
                                                            {
                                                                aggregateId = (RedisKey) aggregateId,
                                                                commandId = correlationId,
                                                                expectedVersion,
                                                                result = resultPayload,
                                                                events = eventsBody
                                                            })
                                       .ConfigureAwait(false);
            _logger.LogDebug($"redisResult:{redisResult} aggregateId:{aggregateId} expectedVersion:{expectedVersion} correlationId:{correlationId} events:{eventsBody}");
            if (redisResult.Type == ResultType.BulkString || redisResult.Type == ResultType.SimpleString)
            {
                var messageAttachment = ((string) redisResult).ToJsonObject<MessageAttachment>();
                if (!string.IsNullOrWhiteSpace(messageAttachment.CommandResult?.Payload))
                {
                    commandResult = messageAttachment.CommandResult
                                                     .Payload
                                                     .ToJsonObject(_messageTypeProvider.GetMessageType(messageAttachment.CommandResult.Code));
                }

                if (!string.IsNullOrWhiteSpace(messageAttachment.SagaResult?.Payload))
                {
                    sagaResult = messageAttachment.SagaResult
                                                  .Payload
                                                  .ToJsonObject(_messageTypeProvider.GetMessageType(messageAttachment.SagaResult.Code));
                }

                events = await GetEvents(aggregateId, correlationId).ConfigureAwait(false);
                throw new MessageDuplicatelyHandled(correlationId, aggregateId, commandResult, sagaResult, events);
            }

            if ((int) redisResult == -2)
            {
                throw new DBConcurrencyException($"aggregateId:{aggregateId} expectedVersion:{expectedVersion} concurrency conflict");
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
            var events = ((string) redisResult)
                         .ToJsonObject<ObjectPayload[]>()
                         .Select(ep => ep.Payload
                                         .ToJsonObject(_messageTypeProvider.GetMessageType(ep.Code)) as IEvent)
                         .ToArray();
            return events;
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