using IFramework.Command;
using IFramework.Command.Impl;
using IFramework.Config;
using IFramework.Event.Impl;
using IFramework.DependencyInjection;
using IFramework.Message;

namespace IFramework.MessageQueue
{
    public static class MessageQueueFactory
    {
        public static ICommandBus GetCommandBus()
        {
            return IoCFactory.GetService<ICommandBus>();
        }

        /// <summary>
        ///     GetMessagePublisher returns a singleton instance of message publisher
        ///     you can call it everywhere to get a message publisher to publish messages
        /// </summary>
        /// <returns></returns>
        public static IMessagePublisher GetMessagePublisher()
        {
            return IoCFactory.GetService<IMessagePublisher>();
        }

        public static IMessageProcessor CreateCommandConsumer(string commandQueue, string consumerId,
                                                             string[] handlerProvierNames, 
                                                             ConsumerConfig consumerConfig = null)
        {
            var container = IoCFactory.Instance.ObjectProvider;
            var messagePublisher = container.GetService<IMessagePublisher>();
            var handlerProvider = new CommandHandlerProvider(handlerProvierNames);
            var messageQueueClient = IoCFactory.GetService<IMessageQueueClient>();
            var commandConsumer = new CommandProcessor(messageQueueClient, messagePublisher, handlerProvider,
                                                      commandQueue, consumerId, consumerConfig);
            return commandConsumer;
        }

        public static IMessageProcessor CreateEventSubscriber(string topic, string subscription, string consumerId,
                                                             string[] handlerProviderNames,
                                                             ConsumerConfig consumerConfig = null)
        {
            subscription = Configuration.Instance.FormatAppName(subscription);
            var handlerProvider = new EventSubscriberProvider(handlerProviderNames);
            var commandBus = GetCommandBus();
            var messagePublisher = GetMessagePublisher();
            var messageQueueClient = IoCFactory.GetService<IMessageQueueClient>();

            var eventSubscriber = new EventSubscriber(messageQueueClient, handlerProvider, commandBus, messagePublisher,
                                                      subscription, topic, consumerId, consumerConfig);
            return eventSubscriber;
        }
    }
}