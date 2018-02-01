using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using IFramework.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace IFramework.EntityFrameworkCore.UnitOfWorks
{
    public class AppUnitOfWork : UnitOfWork, IAppUnitOfWork
    {
        protected List<MessageState> AnywayPublishEventMessageStates;
        protected List<MessageState> EventMessageStates;
        protected IMessagePublisher MessagePublisher;
        protected IMessageQueueClient MessageQueueClient;
        protected IMessageStore MessageStore;

        public AppUnitOfWork(IEventBus eventBus,
                             ILoggerFactory loggerFactory,
                             IMessagePublisher eventPublisher,
                             IMessageQueueClient messageQueueClient,
                             IMessageStore messageStore)
            : base(eventBus, loggerFactory)
        {
            MessageStore = messageStore;
            MessagePublisher = eventPublisher;
            MessageQueueClient = messageQueueClient;
            EventMessageStates = new List<MessageState>();
            AnywayPublishEventMessageStates = new List<MessageState>();
        }

        protected override void BeforeCommit()
        {
            base.BeforeCommit();
            EventMessageStates.Clear();
            EventBus.GetEvents()
                    .ForEach(@event =>
                    {
                        var topic = @event.GetFormatTopic();
                        var eventContext = MessageQueueClient.WrapMessage(@event, null, topic, @event.Key);
                        EventMessageStates.Add(new MessageState(eventContext));
                    });

            EventBus.GetToPublishAnywayMessages()
                    .ForEach(@event =>
                    {
                        var topic = @event.GetFormatTopic();
                        var eventContext = MessageQueueClient.WrapMessage(@event, null, topic, @event.Key);
                        AnywayPublishEventMessageStates.Add(new MessageState(eventContext));
                    });
            var allMessageStates = EventMessageStates.Union(AnywayPublishEventMessageStates)
                                                     .ToList();
            if (allMessageStates.Count > 0)
            {
                MessageStore.SaveCommand(null, null, allMessageStates.Select(s => s.MessageContext).ToArray());
            }
        }

        protected override void AfterCommit()
        {
            base.AfterCommit();
            if (Exception == null)
            {
                try
                {
                    var allMessageStates = EventMessageStates.Union(AnywayPublishEventMessageStates)
                                                             .ToList();

                    if (allMessageStates.Count > 0)
                    {
                        MessagePublisher.SendAsync(CancellationToken.None, EventMessageStates.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"_messagePublisher SendAsync error");
                }
            }
            else
            {
                MessageStore.Rollback();
                if (Exception is DomainException exception)
                {
                    var exceptionEvent = exception.DomainExceptionEvent;
                    if (exceptionEvent != null)
                    {
                        var topic = exceptionEvent.GetFormatTopic();

                        var exceptionMessage = MessageQueueClient.WrapMessage(exceptionEvent,
                                                                              null,
                                                                              topic);
                        AnywayPublishEventMessageStates.Add(new MessageState(exceptionMessage));
                    }
                }

                try
                {
                    if (AnywayPublishEventMessageStates.Count > 0)
                    {
                        MessageStore.SaveFailedCommand(null,
                                                       Exception,
                                                       AnywayPublishEventMessageStates.Select(s => s.MessageContext)
                                                                                      .ToArray());
                        MessagePublisher.SendAsync(CancellationToken.None,
                                                   AnywayPublishEventMessageStates.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"_messagePublisher SendAsync error");
                }
            }
            EventBus.ClearMessages();
        }
    }
}