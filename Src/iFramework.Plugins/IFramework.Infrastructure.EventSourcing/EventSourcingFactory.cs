using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Command.Impl;
using IFramework.DependencyInjection;
using IFramework.Event;
using IFramework.MessageQueue;
using IFramework.Config;
using IFramework.Event.Impl;

namespace IFramework.Infrastructure.EventSourcing
{
    public class EventSourcingFactory
    {
        public static IMessageProcessor CreateCommandConsumer(string commandQueue,
                                                                           string consumerId,
                                                                           string[] handlerProviderNames,
                                                                           ConsumerConfig consumerConfig = null)
        {
            var container = ObjectProviderFactory.Instance.ObjectProvider;
            var messagePublisher = container.GetService<IMessagePublisher>();
            var handlerProvider = new CommandHandlerProvider( );
            var messageQueueClient = ObjectProviderFactory.GetService<IMessageQueueClient>();
            var eventStore = ObjectProviderFactory.GetService<IEventStore>();
            var commandConsumer = new EventSourcingCommandProcessor(messageQueueClient,
                                                                    messagePublisher,
                                                                    handlerProvider,
                                                                    eventStore,
                                                                    commandQueue,
                                                                    consumerId,
                                                                    consumerConfig);
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
            var eventSubscriber = CreateEventSubscriber(new[] {new TopicSubscription(topic, tagFilter)},
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
                                                              ConsumerConfig consumerConfig = null)
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
                                                      consumerConfig);
            MessageQueueFactory.MessageProcessors.Add(eventSubscriber);
            return eventSubscriber;
        }
    }
}
