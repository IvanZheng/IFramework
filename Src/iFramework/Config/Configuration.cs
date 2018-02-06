using System;
using System.Collections.Generic;
using IFramework.DependencyInjection;
using IFramework.Event;
using IFramework.Event.Impl;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Caching;
using IFramework.Infrastructure.Caching.Impl;
using IFramework.Message;
using IFramework.Message.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace IFramework.Config
{
    public class Configuration : IConfiguration
    {
        public static readonly Configuration Instance = new Configuration();
        public IConfiguration ConfigurationCore;

        private Configuration() { }

        public bool NeedMessageStore { get; protected set; }


        private bool CommitPerMessage { get; set; }


        public IConfigurationSection GetSection(string key)
        {
            return Instance.ConfigurationCore?.GetSection(key);
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return Instance.ConfigurationCore?.GetChildren();
        }

        public IChangeToken GetReloadToken()
        {
            return Instance.ConfigurationCore?.GetReloadToken();
        }

        public string this[string key]
        {
            get => Instance.ConfigurationCore?[key];
            set => Instance.ConfigurationCore[key] = value;
        }


        public Configuration UseConfiguration(IConfiguration configuration)
        {
            ConfigurationCore = configuration ?? throw new ArgumentNullException(nameof(configuration));
            return this;
        }

        public Configuration RegisterCommonComponents()
        {
            UseNullLogger();
            UseMemoryCahce();
            UserDataContractJson();
            UseMessageStore<MockMessageStore>();
            this.UseMockMessageQueueClient();
            this.UseMockMessagePublisher();
            RegisterDefaultEventBus();
            RegisterExceptionManager<ExceptionManager>();
            return this;
        }

        public Configuration UseNullLogger()
        {
            IoCFactory.Instance
                      .RegisterType<ILoggerFactory, NullLoggerFactory>(ServiceLifetime.Singleton);
            return this;
        }

        public Configuration RegisterExceptionManager<TExceptionManager>() where TExceptionManager : class, IExceptionManager
        {
            IoCFactory.Instance
                      .RegisterType<IExceptionManager, TExceptionManager>(ServiceLifetime.Singleton);
            return this;
        }

        private Configuration UserDataContractJson()
        {
            IoCFactory.Instance
                      .RegisterInstance(typeof(IJsonConvert)
                                        , new DataContractJsonConvert());
            return this;
        }

        public Configuration UseMemoryCahce(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            IoCFactory.Instance.RegisterType<ICacheManager, MemoryCacheManager>(lifetime);
            return this;
        }

        public Configuration UseMessageStore<TMessageStore>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TMessageStore : class, IMessageStore
        {
            NeedMessageStore = typeof(TMessageStore) != typeof(MockMessageStore);
            IoCFactory.Instance.RegisterType<IMessageStore, TMessageStore>(lifetime);
            return this;
        }

        //public Configuration UseNoneLogger()
        //{
        //    IoCFactory.Instance.RegisterType<ILoggerLevelController, LoggerLevelController>(ServiceLifetime.Singleton);
        //    IoCFactory.Instance.RegisterInstance(typeof(ILoggerFactory)
        //                                         , new MockLoggerFactory());
        //    return this;
        //}

        public Configuration RegisterDefaultEventBus(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return RegisterDefaultEventBus(null, lifetime);
        }

        /// <summary>
        ///     should use after RegisterCommonComponents
        /// </summary>
        /// <param name="eventSubscriberProviders"></param>
        /// <returns></returns>
        public Configuration UseSyncEventSubscriberProvider(params string[] eventSubscriberProviders)
        {
            var provider = new SyncEventSubscriberProvider(eventSubscriberProviders);
            IoCFactory.Instance.RegisterInstance(provider);
            return this;
        }

        public Configuration RegisterDefaultEventBus(IObjectProviderBuilder builder, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            builder = builder ?? IoCFactory.Instance.ObjectProviderBuilder;
            builder.RegisterInstance(new SyncEventSubscriberProvider());
            builder.RegisterType<IEventBus, EventBus>(lifetime);
            return this;
        }

        public bool GetCommitPerMessage()
        {
            return CommitPerMessage;
        }

        public Configuration SetCommitPerMessage(bool commitPerMessage = false)
        {
            CommitPerMessage = commitPerMessage;
            return this;
        }

        public static T GetAppConfig<T>(string key)
        {
            var val = default(T);
            try
            {
                var value = GetAppConfig(key);
                if (typeof(T).IsEquivalentTo(typeof(Guid)))
                {
                    val = (T) Convert.ChangeType(new Guid(value), typeof(T));
                }
                else
                {
                    val = (T) Convert.ChangeType(value, typeof(T));
                }
            }
            catch (Exception)
            {
                // ignored
            }
            return val;
        }

        public static string GetConnectionString(string name)
        {
            return Instance.ConfigurationCore
                           ?.GetConnectionString(name);
        }

        public static string GetAppConfig(string key)
        {
            return Instance.ConfigurationCore?[key];
        }
    }
}