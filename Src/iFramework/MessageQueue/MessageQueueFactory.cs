using IFramework.Command;
using IFramework.Command.Impl;
using IFramework.Config;
using IFramework.Event.Impl;
using IFramework.IoC;
using IFramework.Message;

namespace IFramework.MessageQueue
{
    public static class MessageQueueFactory
    {

        public static ICommandBus GetCommandBus()
        {
            return IoCFactory.Resolve<ICommandBus>();
        }

        /// <summary>
        /// GetMessagePublisher returns a singleton instance of message publisher
        /// you can call it everywhere to get a message publisher to publish messages
        /// </summary>
        /// <returns></returns>
        public static IMessagePublisher GetMessagePublisher()
        {
            return IoCFactory.Resolve<IMessagePublisher>();
        }

        public static IMessageConsumer CreateCommandConsumer(string commandQueue, string consumerId, params string[] handlerProvierNames)
        {
            var container = IoCFactory.Instance.CurrentContainer;
            var messagePublisher = container.Resolve<IMessagePublisher>();
            var handlerProvider = new CommandHandlerProvider(handlerProvierNames);
            var messageQueueClient = IoCFactory.Resolve<IMessageQueueClient>();
            var commandConsumer = new CommandConsumer(messageQueueClient, messagePublisher, handlerProvider, commandQueue, consumerId);
            return commandConsumer;
        }

        public static IMessageConsumer CreateEventSubscriber(string topic, string subscription, string consumerId, params string[] handlerProviderNames)
        {
            if (!string.IsNullOrEmpty(FrameworkConfigurationExtension.AppName))
            {
                subscription = $"{FrameworkConfigurationExtension.AppName}.{subscription}";
            }
            var container = IoCFactory.Instance.CurrentContainer;
            var handlerProvider = new EventSubscriberProvider(handlerProviderNames);
            var commandBus = GetCommandBus();
            var messagePublisher = GetMessagePublisher();
            var messageQueueClient = IoCFactory.Resolve<IMessageQueueClient>();

            var eventSubscriber = new EventSubscriber(messageQueueClient, handlerProvider, commandBus, messagePublisher, subscription, topic, consumerId);
            return eventSubscriber;
        }
    }
}
