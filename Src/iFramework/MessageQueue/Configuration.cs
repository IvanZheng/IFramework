using IFramework.Command;
using IFramework.Command.Impl;
using IFramework.Event;
using IFramework.Event.Impl;
using IFramework.IoC;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using System;

namespace IFramework.Config
{
    public static class FrameworkConfigurationExtension
    {
        static string _MessageQueueNameFormat = string.Empty;
        static string _appNameFormat = string.Empty;
        static TimeSpan _ReceiveMessageTimeout = new TimeSpan(0, 0, 10);
        public static string AppName { get; private set; }
        public static Configuration UseMessageQueue(this Configuration configuration, string appName = null)
        {
            AppName = appName;
            var appNameFormat = string.IsNullOrEmpty(appName) ? "{0}" : appName + ".{0}";
            configuration.SetAppNameFormat(appNameFormat)
                         .UseDefaultEventBus()
                         .UseMockCommandBus()
                         .UseMockMessageStore()
                         .UseMockMessagePublisher();
            return configuration;
        }

        public static Configuration UseDefaultEventBus(this Configuration configuration)
        {
            IoCFactory.Instance.CurrentContainer.RegisterType<IEventBus, EventBus>(Lifetime.Hierarchical);
            return configuration;
        }

        public static Configuration UseMockMessageStore(this Configuration configuration)
        {
            IoCFactory.Instance.CurrentContainer.RegisterType<IMessageStore, MockMessageStore>(Lifetime.Singleton);
            return configuration;
        }
        public static Configuration UseMessageStore<TMessageStore>(this Configuration configuration)
            where TMessageStore : IMessageStore
        {
            IoCFactory.Instance.CurrentContainer.RegisterType<IMessageStore, TMessageStore>(Lifetime.Hierarchical);
            return configuration;
        }

        public static Configuration UseMockMessagePublisher(this Configuration configuration)
        {
            IoCFactory.Instance.CurrentContainer.RegisterType<IMessagePublisher, MockMessagePublisher>(Lifetime.Singleton);
            return configuration;
        }
        public static Configuration UseMessagePublisher(this Configuration configuration, string defaultTopic, bool needMessageStore = false)
        {
            var container = IoCFactory.Instance.CurrentContainer;
            var messageQueueClient = IoCFactory.Resolve<IMessageQueueClient>();
            var messagePublisher = new MessagePublisher(messageQueueClient, defaultTopic, needMessageStore);
            container.RegisterInstance<IMessagePublisher>(messagePublisher);
            return configuration;
        }

        public static Configuration UseMockCommandBus(this Configuration configuration)
        {
            IoCFactory.Instance.CurrentContainer.RegisterType<ICommandBus, MockCommandBus>(Lifetime.Singleton);
            return configuration;
        }
        public static Configuration UseCommandBus(this Configuration configuration, string replyTopic = "replyTopic", string replySubscription = "replySubscription", bool needMessageStore = false, ILinearCommandManager linerCommandManager = null)
        {
            var container = IoCFactory.Instance.CurrentContainer;
            if (linerCommandManager == null)
            {
                linerCommandManager = new LinearCommandManager();
            }
            var messageQueueClient = IoCFactory.Resolve<IMessageQueueClient>();
            var commandBus = new CommandBus(messageQueueClient, linerCommandManager, replyTopic, replySubscription, needMessageStore);
            container.RegisterInstance<ICommandBus>(commandBus);
            return configuration;
        }

        public static TimeSpan GetMessageQueueReceiveMessageTimeout(this Configuration configuration)
        {
            return _ReceiveMessageTimeout;
        }

        public static Configuration SetMessageQueueReceiveMessageTimeout(this Configuration configuration, TimeSpan timeout)
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

        public static Configuration MessageQueueUseMachineNameFormat(this Configuration configuration, bool onlyInDebug = true)
        {
            var compliationSection = Configuration.GetCompliationSection();
            if (!onlyInDebug || (compliationSection != null && compliationSection.Debug))
            {
                configuration.SetMessageQueueNameFormat(Environment.MachineName + ".{0}");
            }
            return configuration;
        }

        public static string FormatAppName(this Configuration configuration, string topic)
        {
            return string.IsNullOrEmpty(_appNameFormat) ?
                          topic :
                          string.Format(_appNameFormat, topic);
        }

        public static string FormatMessageQueueName(this Configuration configuration, string name)
        {
            return string.IsNullOrEmpty(_MessageQueueNameFormat) ?
                          name :
                          string.Format(_MessageQueueNameFormat, name);
        }


    }
}
