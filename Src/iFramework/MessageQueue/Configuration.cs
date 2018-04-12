using System;
using IFramework.Command;
using IFramework.Command.Impl;
using IFramework.DependencyInjection;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using Microsoft.Extensions.DependencyInjection;

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
        //    ObjectProviderFactory.Instance.CurrentContainer.RegisterType<IEventBus, EventBus>(Lifetime.Hierarchical);
        //    return configuration;
        //}


        public static Configuration UseMockMessageQueueClient(this Configuration configuration)
        {
            ObjectProviderFactory.Instance.RegisterType<IMessageQueueClient, MockMessageQueueClient>(ServiceLifetime.Singleton);
            return configuration;
        }

        public static Configuration UseMockMessagePublisher(this Configuration configuration)
        {
            ObjectProviderFactory.Instance.RegisterType<IMessagePublisher, MockMessagePublisher>(ServiceLifetime.Singleton);
            return configuration;
        }

        public static Configuration UseMessagePublisher(this Configuration configuration, string defaultTopic)
        {
            var builder = ObjectProviderFactory.Instance.ObjectProviderBuilder;

            builder.Register<IMessagePublisher>(provider =>
            {
                var messageQueueClient = ObjectProviderFactory.GetService<IMessageQueueClient>();
                configuration.SetDefaultTopic(defaultTopic);
                defaultTopic = configuration.FormatAppName(defaultTopic);
                var messagePublisher = new MessagePublisher(messageQueueClient, defaultTopic);
                return messagePublisher;
            }, ServiceLifetime.Singleton);
            return configuration;
        }

        public static Configuration UseMockCommandBus(this Configuration configuration)
        {
            ObjectProviderFactory.Instance.RegisterType<ICommandBus, MockCommandBus>(ServiceLifetime.Singleton);
            return configuration;
        }

        public static Configuration UseCommandBus(this Configuration configuration,
                                                  string consumerId,
                                                  string replyTopic = "replyTopic",
                                                  string replySubscription = "replySubscription",
                                                  ILinearCommandManager linerCommandManager = null,
                                                  ConsumerConfig consumerConfig = null)
        {
            var builder = ObjectProviderFactory.Instance.ObjectProviderBuilder;

            builder.Register<ICommandBus>(provider =>
            {
                if (linerCommandManager == null)
                {
                    linerCommandManager = new LinearCommandManager();
                }
                var messageQueueClient = provider.GetService<IMessageQueueClient>();
                var commandBus = new CommandBus(messageQueueClient, linerCommandManager, consumerId, replyTopic,
                                                replySubscription, consumerConfig);
                return commandBus;
            }, ServiceLifetime.Singleton);
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
            var debug = Configuration.Instance.Get<bool>("Debug");
            if (!onlyInDebug || debug)
            {
                configuration.SetMessageQueueNameFormat(Environment.MachineName + ".{0}");
            }
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