using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using IFramework.DependencyInjection;
using IFramework.Event;
using IFramework.Event.Impl;
using IFramework.EventStore;
using IFramework.EventStore.Impl;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Caching;
using IFramework.Infrastructure.Caching.Impl;
using IFramework.Infrastructure.Mailboxes;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IFramework.Config
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            Configuration.Instance.ConfigurationCore = configuration ?? throw new ArgumentNullException(nameof(configuration));
            services.AddSingleton(typeof(Configuration), Configuration.Instance)
                    .AddSingleton<IConfiguration>(Configuration.Instance);
            return services;
        }

        public static IServiceCollection AddCommonComponents(this IServiceCollection services, string app = null)
        {
            services.AddMemoryCache()
                    .AddMicrosoftJson()
                    .AddMessageStore<MockMessageStore>()
                    .AddMessageStoreDaemon<MockMessageStoreDaemon>()
                    .AddMockMessageQueueClient()
                    .AddMockMessagePublisher()
                    .AddDefaultEventBus()
                    .AddConcurrencyProcessor<ConcurrencyProcessor, UniqueConstrainExceptionParser>()
                    .AddMessageQueue(app)
                    .MessageQueueUseMachineNameFormat()
                    .AddMessageTypeProvider<MessageTypeProvider>()
                    .AddMailbox<MailboxProcessor, DefaultProcessingMessageScheduler>()
                    .AddSingleton<IEventSerializer, JsonEventSerializer>()
                    .AddSingleton<IEventDeserializer, JsonEventDeserializer>();
            return services;
        }
        
        private static IServiceCollection AddMailbox<TMailboxProcessor, TProcessingMessageScheduler>(this IServiceCollection services) 
            where TMailboxProcessor : class, IMailboxProcessor
            where TProcessingMessageScheduler: class, IProcessingMessageScheduler
        {
            services.AddService<IProcessingMessageScheduler, TProcessingMessageScheduler>(ServiceLifetime.Singleton);
            services.AddService<IMailboxProcessor, TMailboxProcessor>(ServiceLifetime.Singleton);
            return services;
        }

        public static IServiceCollection AddMessageTypeProvider<TMessageTypeProvider>(this IServiceCollection services)
            where TMessageTypeProvider :class, IMessageTypeProvider
        {
            services.AddService<IMessageTypeProvider, TMessageTypeProvider>(ServiceLifetime.Singleton);
            return services;
        }

        public static IServiceCollection AddConcurrencyProcessor<TConcurrencyProcessor, TUniqueConstrainExceptionParser>(this IServiceCollection services) 
            where TConcurrencyProcessor : class, IConcurrencyProcessor
            where TUniqueConstrainExceptionParser : class, IUniqueConstrainExceptionParser
        {
            services.AddService<IConcurrencyProcessor, TConcurrencyProcessor>(ServiceLifetime.Singleton)
                    .AddService<IUniqueConstrainExceptionParser, TUniqueConstrainExceptionParser>(ServiceLifetime.Singleton);
            return services;
        }

        public static IServiceCollection AddDefaultEventBus(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            services.AddSingleton(new SyncEventSubscriberProvider());
            services.AddService<IEventBus, EventBus>(lifetime);
            services.AddService<IMessageContext, EmptyMessageContext>(lifetime);
            return services;
        }
    

        public static IServiceCollection AddMockMessagePublisher(this IServiceCollection services)
        {
            services.AddService<IMessagePublisher, MockMessagePublisher>(ServiceLifetime.Singleton);
            return services;
        }

        public static IServiceCollection AddMockMessageQueueClient(this IServiceCollection services)
        {
            services.AddService<IMessageQueueClient, MockMessageQueueClient>(ServiceLifetime.Singleton);
            return services;
        }


        public static IServiceCollection AddMessageStoreDaemon<TMessageStoreDaemon>(this IServiceCollection services)
            where TMessageStoreDaemon : class, IMessageStoreDaemon
        {
            services.AddService<IMessageStoreDaemon, TMessageStoreDaemon>(ServiceLifetime.Singleton);
            return services;

        }

        /// <summary>
        /// if sameIntanceAsBusinessDbContext is true, TMessageStore must be registerd before object provider to be built!
        /// </summary>
        /// <typeparam name="TMessageStore"></typeparam>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static IServiceCollection AddMessageStore<TMessageStore>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TMessageStore : class, IMessageStore
        {
            Configuration.Instance.NeedMessageStore = typeof(TMessageStore) != typeof(MockMessageStore);
            if (Configuration.Instance.NeedMessageStore)
            {
                services.AddService<TMessageStore, TMessageStore>(lifetime);
                services.AddService(typeof(IMessageStore),provider => provider.GetService<TMessageStore>(), lifetime);
            }
            else
            {
                services.AddService<IMessageStore, MockMessageStore>(lifetime);
            }
            return services;
        }

        public static IServiceCollection AddMicrosoftJson(this IServiceCollection services)
        {
            services.AddSingleton<IJsonConvert>(new MicrosoftJsonConvert());
            return services;
        }

        public static IServiceCollection AddMemoryCache(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            services.AddService<ICacheManager, MemoryCacheManager>(lifetime);
            return services;
        }

        public static IServiceCollection AddNullLogger(this IServiceCollection services)
        {
            return services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        }
    }
}
