using System;
using System.Collections.Generic;
using System.Linq;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using IFramework.UnitOfWork;

namespace IFramework.EntityFramework
{
    public class AppUnitOfWork : UnitOfWork, IAppUnitOfWork
    {
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
            _dbContexts = new List<MSDbContext>();
            _eventBus = eventBus;
            _messageStore = messageStore;
            _messagePublisher = eventPublisher;
            _messageQueueClient = messageQueueClient;
            _eventMessageStates = new List<MessageState>();
        }

        protected override void BeforeCommit()
        {
            base.BeforeCommit();
            _eventMessageStates.Clear();
            _eventBus.GetEvents().ForEach(@event =>
            {
                var topic = @event.GetFormatTopic();
                var eventContext = _messageQueueClient.WrapMessage(@event, null, topic, @event.Key);
                _eventMessageStates.Add(new MessageState(eventContext));
            });

            _eventBus.GetToPublishAnywayMessages().ForEach(@event =>
            {
                var topic = @event.GetFormatTopic();
                var eventContext = _messageQueueClient.WrapMessage(@event, null, topic, @event.Key);
                _eventMessageStates.Add(new MessageState(eventContext));
            });
            _messageStore.SaveCommand(null, null, _eventMessageStates.Select(s => s.MessageContext).ToArray());
        }

        protected override void AfterCommit()
        {
            base.AfterCommit();

            _eventBus.ClearMessages();
            try
            {
                if (_messagePublisher != null && _eventMessageStates.Count > 0)
                    _messagePublisher.SendAsync(_eventMessageStates.ToArray());
            }
            catch (Exception ex)
            {
                _logger.Error($"_messagePublisher SendAsync error", ex);
            }
        }
    }
}