using System;
using System.Collections.Generic;
using System.Linq;
using IFramework.Event;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using IFramework.UnitOfWork;

namespace IFramework.EntityFramework
{
    public class AppUnitOfWork: UnitOfWork, IAppUnitOfWork
    {
        protected List<MessageState> _anywayPublishEventMessageStates;
        protected List<MessageState> _eventMessageStates;
        protected IMessagePublisher _messagePublisher;
        protected IMessageQueueClient _messageQueueClient;
        protected IMessageStore _messageStore;

        public AppUnitOfWork(IEventBus eventBus,
                             ILoggerFactory loggerFactory,
                             IMessagePublisher eventPublisher,
                             IMessageQueueClient messageQueueClient,
                             IMessageStore messageStore)
            : base(eventBus, loggerFactory)
        {
            _messageStore = messageStore;
            _messagePublisher = eventPublisher;
            _messageQueueClient = messageQueueClient;
            _eventMessageStates = new List<MessageState>();
            _anywayPublishEventMessageStates = new List<MessageState>();
        }

        protected override void BeforeCommit()
        {
            base.BeforeCommit();
            _eventMessageStates.Clear();
            _eventBus.GetEvents()
                     .ForEach(@event =>
                     {
                         var topic = @event.GetFormatTopic();
                         var eventContext = _messageQueueClient.WrapMessage(@event, null, topic, @event.Key);
                         _eventMessageStates.Add(new MessageState(eventContext));
                     });

            _eventBus.GetToPublishAnywayMessages()
                     .ForEach(@event =>
                     {
                         var topic = @event.GetFormatTopic();
                         var eventContext = _messageQueueClient.WrapMessage(@event, null, topic, @event.Key);
                         _anywayPublishEventMessageStates.Add(new MessageState(eventContext));
                     });
            var allMessageStates = _eventMessageStates.Union(_anywayPublishEventMessageStates)
                                                      .ToList();
            if (allMessageStates.Count > 0)
            {
                _messageStore.SaveCommand(null, null, allMessageStates.Select(s => s.MessageContext).ToArray());
            }
        }

        protected override void AfterCommit()
        {
            base.AfterCommit();
            if (_exception == null)
            {
                try
                {
                    var allMessageStates = _eventMessageStates.Union(_anywayPublishEventMessageStates)
                                                              .ToList();

                    if (allMessageStates.Count > 0)
                    {
                        _messagePublisher.SendAsync(_eventMessageStates.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"_messagePublisher SendAsync error", ex);
                }
            }
            else
            {
                _messageStore.Rollback();
                if (_exception is DomainException)
                {
                    var exceptionEvent = ((DomainException)_exception).DomainExceptionEvent;
                    if (exceptionEvent != null)
                    {
                        var topic = exceptionEvent.GetFormatTopic();

                        var exceptionMessage = _messageQueueClient.WrapMessage(exceptionEvent,
                                                                               null,
                                                                               topic);
                        _anywayPublishEventMessageStates.Add(new MessageState(exceptionMessage));
                    }
                }

                try
                {
                    if (_anywayPublishEventMessageStates.Count > 0)
                    {
                        _messageStore.SaveFailedCommand(null,
                                                        _exception,
                                                        _anywayPublishEventMessageStates.Select(s => s.MessageContext)
                                                                                        .ToArray());
                        _messagePublisher.SendAsync(_anywayPublishEventMessageStates.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"_messagePublisher SendAsync error", ex);
                }

            }
            _eventBus.ClearMessages();
        }
    }
}