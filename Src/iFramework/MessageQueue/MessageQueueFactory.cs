using System;
using System.Collections.Concurrent;
using IFramework.Command;
using IFramework.Command.Impl;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Event.Impl;
using IFramework.Message;
using IFramework.Message.Impl;

namespace IFramework.MessageQueue
{
    public static class MessageQueueFactory
    {
        public static ConcurrentBag<IMessageProcessor> MessageProcessors = new ConcurrentBag<IMessageProcessor>();

        public static ICommandBus GetCommandBus()
        {
            return ObjectProviderFactory.GetService<ICommandBus>();
        }

        /// <summary>
        ///     GetMessagePublisher returns a singleton instance of message publisher
        ///     you can call it everywhere to get a message publisher to publish messages
        /// </summary>
        /// <returns></returns>
        public static IMessagePublisher GetMessagePublisher()
        {
            return ObjectProviderFactory.GetService<IMessagePublisher>();
        }

        public static IMessageProcessor CreateCommandConsumer(string commandQueue,
                                                              string consumerId,
                                                              string[] handlerProviderNames,
                                                              ConsumerConfig consumerConfig = null)
        {
            var container = ObjectProviderFactory.Instance.ObjectProvider;
            var messagePublisher = container.GetService<IMessagePublisher>();
            var handlerProvider = new CommandHandlerProvider(handlerProviderNames);
            var messageQueueClient = ObjectProviderFactory.GetService<IMessageQueueClient>();
            var commandConsumer = new CommandProcessor<PayloadMessage>(messageQueueClient,
                                                                       messagePublisher,
                                                                       handlerProvider,
                                                                       commandQueue,
                                                                       consumerId,
                                                                       consumerConfig);
            MessageProcessors.Add(commandConsumer);
            return commandConsumer;
        }

        public static IMessageProcessor CreateCommandConsumer<TPayloadMessage>(string commandQueue,
                                                                               string consumerId,
                                                                               string[] handlerProviderNames,
                                                                               ConsumerConfig consumerConfig = null,
                                                                               IMessageContextBuilder<TPayloadMessage> messageContextBuilder = null)
        {
            var container = ObjectProviderFactory.Instance.ObjectProvider;
            var messagePublisher = container.GetService<IMessagePublisher>();
            var handlerProvider = new CommandHandlerProvider(handlerProviderNames);
            var messageQueueClient = ObjectProviderFactory.GetService<IMessageQueueClient>();
            var commandConsumer = new CommandProcessor<TPayloadMessage>(messageQueueClient,
                                                                        messagePublisher,
                                                                        handlerProvider,
                                                                        commandQueue,
                                                                        consumerId,
                                                                        consumerConfig,
                                                                        messageContextBuilder);
            MessageProcessors.Add(commandConsumer);
            return commandConsumer;
        }

        public static IMessageProcessor CreateEventSubscriber(string topic,
                                                              string subscription,
                                                              string consumerId,
                                                              string[] handlerProviderNames,
                                                              ConsumerConfig consumerConfig = null,
                                                              Func<string[], bool> tagFilter = null)
        {
            var eventSubscriber = CreateEventSubscriber<PayloadMessage>(new[] { new TopicSubscription(topic, tagFilter) },
                                                                        subscription,
                                                                        consumerId,
                                                                        handlerProviderNames,
                                                                        consumerConfig);
            return eventSubscriber;
        }

        public static IMessageProcessor CreateEventSubscriber<TPayloadMessage>(string topic,
                                                                               string subscription,
                                                                               string consumerId,
                                                                               string[] handlerProviderNames,
                                                                               ConsumerConfig consumerConfig = null,
                                                                               Func<string[], bool> tagFilter = null,
                                                                               IMessageContextBuilder<TPayloadMessage> messageContextBuilder = null)
        {
            var eventSubscriber = CreateEventSubscriber(new[] { new TopicSubscription(topic, tagFilter) },
                                                        subscription,
                                                        consumerId,
                                                        handlerProviderNames,
                                                        consumerConfig,
                                                        messageContextBuilder);
            return eventSubscriber;
        }

        public static IMessageProcessor CreateEventSubscriber(TopicSubscription[] topicSubscriptions,
                                                              string subscription,
                                                              string consumerId,
                                                              string[] handlerProviderNames,
                                                              ConsumerConfig consumerConfig = null)
        {
            return CreateEventSubscriber<PayloadMessage>(topicSubscriptions,
                                                         subscription,
                                                         consumerId,
                                                         handlerProviderNames,
                                                         consumerConfig);
        }

        public static IMessageProcessor CreateEventSubscriber<TPayloadMessage>(TopicSubscription[] topicSubscriptions,
                                                                               string subscription,
                                                                               string consumerId,
                                                                               string[] handlerProviderNames,
                                                                               ConsumerConfig consumerConfig = null,
                                                                               IMessageContextBuilder<TPayloadMessage> messageContextBuilder = null)
        {
            subscription = Configuration.Instance.FormatAppName(subscription);
            var handlerProvider = new EventSubscriberProvider(handlerProviderNames);
            var commandBus = GetCommandBus();
            var messagePublisher = GetMessagePublisher();
            var messageQueueClient = ObjectProviderFactory.GetService<IMessageQueueClient>();

            var eventSubscriber = new EventSubscriber<TPayloadMessage>(messageQueueClient,
                                                                       handlerProvider,
                                                                       commandBus,
                                                                       messagePublisher,
                                                                       subscription,
                                                                       topicSubscriptions,
                                                                       consumerId,
                                                                       consumerConfig,
                                                                       messageContextBuilder);
            MessageProcessors.Add(eventSubscriber);
            return eventSubscriber;
        }
    }
}