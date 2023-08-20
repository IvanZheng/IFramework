using System;
using IFramework.Command.Impl;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Event;
using IFramework.Event.Impl;
using IFramework.Message;
using IFramework.MessageQueue;

namespace IFramework.Infrastructure.EventSourcing
{
    public class EventSourcingFactory
    {
        public static IMessageProcessor CreateCommandConsumer(string commandQueue,
                                                              string consumerId,
                                                              string[] handlerProviderNames,
                                                              ConsumerConfig consumerConfig = null,
                                                              IMessageContextBuilder messageContextBuilder = null)
        {
            var container = ObjectProviderFactory.Instance.ObjectProvider;
            var messagePublisher = container.GetService<IMessagePublisher>();
            var handlerProvider = new CommandHandlerProvider(handlerProviderNames);
            var messageQueueClient = ObjectProviderFactory.GetService<IMessageQueueClient>();
            var eventStore = ObjectProviderFactory.GetService<IEventStore>();
            var commandConsumer = new EventSourcingCommandProcessor(messageQueueClient,
                                                                    messagePublisher,
                                                                    handlerProvider,
                                                                    eventStore,
                                                                    commandQueue,
                                                                    consumerId,
                                                                    consumerConfig,
                                                                    messageContextBuilder);
            MessageQueueFactory.MessageProcessors.Add(commandConsumer);
            return commandConsumer;
        }

        public static IMessageProcessor CreateEventSubscriber(string topic,
                                                              string subscription,
                                                              string consumerId,
                                                              string[] handlerProviderNames,
                                                              ConsumerConfig consumerConfig = null,
                                                              Func<string[], bool> tagFilter = null)
        {
            var eventSubscriber = CreateEventSubscriber(new[] { new TopicSubscription(topic, tagFilter) },
                                                        subscription,
                                                        consumerId,
                                                        handlerProviderNames,
                                                        consumerConfig);
            return eventSubscriber;
        }

        public static IMessageProcessor CreateEventSubscriber(TopicSubscription[] topicSubscriptions,
                                                              string subscription,
                                                              string consumerId,
                                                              string[] handlerProviderNames,
                                                              ConsumerConfig consumerConfig = null,
                                                              IMessageContextBuilder messageContextBuilder = null)
        {
            var eventStore = ObjectProviderFactory.GetService<IEventStore>();
            subscription = Configuration.Instance.FormatAppName(subscription);
            var handlerProvider = new EventSubscriberProvider(handlerProviderNames);
            var commandBus = MessageQueueFactory.GetCommandBus();
            var messagePublisher = MessageQueueFactory.GetMessagePublisher();
            var messageQueueClient = ObjectProviderFactory.GetService<IMessageQueueClient>();

            var eventSubscriber = new EventSourcingEventSubscriber(messageQueueClient,
                                                                   handlerProvider,
                                                                   commandBus,
                                                                   messagePublisher,
                                                                   subscription,
                                                                   topicSubscriptions,
                                                                   consumerId,
                                                                   eventStore,
                                                                   consumerConfig,
                                                                   messageContextBuilder);
            MessageQueueFactory.MessageProcessors.Add(eventSubscriber);
            return eventSubscriber;
        }
    }
}