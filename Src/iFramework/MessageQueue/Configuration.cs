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
        private static string _messageQueueNameFormat = string.Empty;
        private static string _appNameFormat = string.Empty;
        private static TimeSpan _receiveMessageTimeout = new TimeSpan(0, 0, 10);

        private static string _defaultTopic = string.Empty;
        public static string AppName { get; private set; }
        public static string QueueNameSplit { get; private set; } = ".";
        public static Configuration SetDefaultTopic(this Configuration configuration, string defaultTopic)
        {
            _defaultTopic = defaultTopic;
            return configuration;
        }

        public static string GetDefaultTopic(this Configuration configuration)
        {
            return _defaultTopic;
        }


        public static IServiceCollection AddMessageQueue(this IServiceCollection services, string appName = null, string queueNameSplit = ".")
        {
            AppName = appName;
            QueueNameSplit = queueNameSplit;
            var appNameFormat = string.IsNullOrEmpty(appName) ? "{0}" : appName + QueueNameSplit + "{0}";
            services.SetAppNameFormat(appNameFormat)
                    .AddMockCommandBus()
                    .AddMockMessagePublisher();
            return services;
        }

        //public static Configuration UseDefaultEventBus(this Configuration configuration)
        //{
        //    ObjectProviderFactory.Instance.CurrentContainer.RegisterType<IEventBus, EventBus>(Lifetime.Hierarchical);
        //    return configuration;
        //}


      
        

        public static IServiceCollection AddMessagePublisher(this IServiceCollection services, string defaultTopic)
        {
           
            services.AddSingleton<IMessagePublisher>(provider =>
            {
                var messageQueueClient = provider.GetService<IMessageQueueClient>();
                Configuration.Instance.SetDefaultTopic(defaultTopic);
                defaultTopic = Configuration.Instance.FormatAppName(defaultTopic);
                var messagePublisher = new MessagePublisher(messageQueueClient, defaultTopic);
                return messagePublisher;
            });
            return services;
        }

        public static IServiceCollection AddMockCommandBus(this IServiceCollection services)
        {
            services.AddService<ICommandBus, MockCommandBus>(ServiceLifetime.Singleton);
            return services;
        }

        public static IServiceCollection AddCommandBus(this IServiceCollection services,
                                                  string consumerId,
                                                  string replyTopic = "replyTopic",
                                                  string replySubscription = "replySubscription",
                                                  ISerialCommandManager serialCommandManager = null,
                                                  ConsumerConfig consumerConfig = null)
        {
     
            services.AddService(typeof(ICommandBus), provider =>
            {
                if (serialCommandManager == null)
                {
                    serialCommandManager = new SerialCommandManager();
                }
                var messageQueueClient = provider.GetService<IMessageQueueClient>();
                var commandBus = new CommandBus(messageQueueClient, serialCommandManager, consumerId, replyTopic,
                                                replySubscription, consumerConfig);
                return commandBus;
            }, ServiceLifetime.Singleton);
            return services;
        }

        public static TimeSpan GetMessageQueueReceiveMessageTimeout(this Configuration configuration)
        {
            return _receiveMessageTimeout;
        }

        public static Configuration SetMessageQueueReceiveMessageTimeout(this Configuration configuration,
                                                                         TimeSpan timeout)
        {
            _receiveMessageTimeout = timeout;
            return configuration;
        }

        public static IServiceCollection SetAppNameFormat(this IServiceCollection services, string format)
        {
            _appNameFormat = format;
            return services;
        }

        public static IServiceCollection SetMessageQueueNameFormat(this IServiceCollection services, string format)
        {
            _messageQueueNameFormat = format;
            return services;
        }

        public static IServiceCollection MessageQueueUseMachineNameFormat(this IServiceCollection services,
                                                                     bool onlyInDebug = true)
        {
            var debug = Configuration.Instance.Get<bool>("Debug");
            if (!onlyInDebug || debug)
            {
                services.SetMessageQueueNameFormat(Environment.MachineName + QueueNameSplit + "{0}");
            }
            return services;
        }

        public static string FormatAppName(this Configuration configuration, string topic)
        {
            return string.IsNullOrEmpty(_appNameFormat) ? topic : string.Format(_appNameFormat, topic);
        }

        public static string FormatMessageQueueName(this Configuration configuration, string name)
        {
            return string.IsNullOrEmpty(_messageQueueNameFormat) ? name : string.Format(_messageQueueNameFormat, name);
        }
    }
}