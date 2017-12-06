using System;
using IFramework.Command;
using IFramework.Command.Impl;
using IFramework.Event;
using IFramework.Event.Impl;
using IFramework.IoC;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;

namespace IFramework.Config
{
    public static class FrameworkConfigurationExtension
    {
        private static string _MessageQueueNameFormat = string.Empty;
        private static string _appNameFormat = string.Empty;
        private static TimeSpan _ReceiveMessageTimeout = new TimeSpan(0, 0, 10);

        private static string _defaultTopic = string.Empty;
        public static string AppName { get; private set; }

        public static Configuration SetDefaultTopic(this Configuration configuration, string defaultTopic)
        {
            _defaultTopic = defaultTopic;
            return configuration;
        }

        public static string GetDefaultTopic(this Configuration configuration)
        {
            return _defaultTopic;
        }


        public static Configuration UseMessageQueue(this Configuration configuration, string appName = null)
        {
            AppName = appName;
            var appNameFormat = string.IsNullOrEmpty(appName) ? "{0}" : appName + ".{0}";
            configuration.SetAppNameFormat(appNameFormat)
                         .UseMockCommandBus()
                         .UseMockMessagePublisher();
            return configuration;
        }

        //public static Configuration UseDefaultEventBus(this Configuration configuration)
        //{
        //    IoCFactory.Instance.CurrentContainer.RegisterType<IEventBus, EventBus>(Lifetime.Hierarchical);
        //    return configuration;
        //}


        public static Configuration UseMockMessageQueueClient(this Configuration configuration)
        {
            IoCFactory.Instance.CurrentContainer.RegisterType<IMessageQueueClient, MockMessageQueueClient>(Lifetime
                                                                                                               .Singleton);
            return configuration;
        }

        public static Configuration UseMockMessagePublisher(this Configuration configuration)
        {
            IoCFactory.Instance.CurrentContainer.RegisterType<IMessagePublisher, MockMessagePublisher>(Lifetime
                                                                                                           .Singleton);
            return configuration;
        }

        public static Configuration UseMessagePublisher(this Configuration configuration, string defaultTopic)
        {
            var container = IoCFactory.Instance.CurrentContainer;
            var messageQueueClient = IoCFactory.Resolve<IMessageQueueClient>();
            configuration.SetDefaultTopic(defaultTopic);
            defaultTopic = configuration.FormatAppName(defaultTopic);
            var messagePublisher = new MessagePublisher(messageQueueClient, defaultTopic);
            container.RegisterInstance<IMessagePublisher>(messagePublisher);
            return configuration;
        }

        public static Configuration UseMockCommandBus(this Configuration configuration)
        {
            IoCFactory.Instance.CurrentContainer.RegisterType<ICommandBus, MockCommandBus>(Lifetime.Singleton);
            return configuration;
        }

        public static Configuration UseCommandBus(this Configuration configuration, string consumerId,
                                                  string replyTopic = "replyTopic", string replySubscription = "replySubscription",
                                                  ILinearCommandManager linerCommandManager = null,
                                                  ConsumerConfig consumerConfig = null)
        {
            var container = IoCFactory.Instance.CurrentContainer;
            if (linerCommandManager == null)
                linerCommandManager = new LinearCommandManager();
            var messageQueueClient = IoCFactory.Resolve<IMessageQueueClient>();
            var commandBus = new CommandBus(messageQueueClient, linerCommandManager, consumerId, replyTopic,
                                            replySubscription, consumerConfig);
            container.RegisterInstance<ICommandBus>(commandBus);
            return configuration;
        }

        public static TimeSpan GetMessageQueueReceiveMessageTimeout(this Configuration configuration)
        {
            return _ReceiveMessageTimeout;
        }

        public static Configuration SetMessageQueueReceiveMessageTimeout(this Configuration configuration,
                                                                         TimeSpan timeout)
        {
            _ReceiveMessageTimeout = timeout;
            return configuration;
        }

        public static Configuration SetAppNameFormat(this Configuration configuration, string format)
        {
            _appNameFormat = format;
            return configuration;
        }

        public static Configuration SetMessageQueueNameFormat(this Configuration configuration, string format)
        {
            _MessageQueueNameFormat = format;
            return configuration;
        }

        public static Configuration MessageQueueUseMachineNameFormat(this Configuration configuration,
                                                                     bool onlyInDebug = true)
        {
            //TODO: 
            //var compliationSection = Configuration.GetCompliationSection();
            //if (!onlyInDebug || compliationSection != null && compliationSection.Debug)
            //{
            //    configuration.SetMessageQueueNameFormat(Environment.MachineName + ".{0}");
            //}
            return configuration;
        }

        public static string FormatAppName(this Configuration configuration, string topic)
        {
            return string.IsNullOrEmpty(_appNameFormat) ? topic : string.Format(_appNameFormat, topic);
        }

        public static string FormatMessageQueueName(this Configuration configuration, string name)
        {
            return string.IsNullOrEmpty(_MessageQueueNameFormat) ? name : string.Format(_MessageQueueNameFormat, name);
        }
    }
}