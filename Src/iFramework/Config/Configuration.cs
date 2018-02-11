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

        public Configuration UseCommonComponents(string app = null)
        {
            UseNullLogger();
            UseMemoryCahce();
            UserDataContractJson();
            UseMessageStore<MockMessageStore>();
            this.UseMockMessageQueueClient();
            this.UseMockMessagePublisher();
            RegisterDefaultEventBus();
            RegisterExceptionManager<ExceptionManager>();
            this.UseMessageQueue(app);
            this.MessageQueueUseMachineNameFormat();
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

        /// <summary>
        /// if sameIntanceAsBusinessDbContext is true, TMessageStore must be registerd before object provider to be built!
        /// </summary>
        /// <typeparam name="TMessageStore"></typeparam>
        /// <param name="sameIntanceAsBusinessDbContext"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public Configuration UseMessageStore<TMessageStore>(bool sameIntanceAsBusinessDbContext = true, ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TMessageStore : class, IMessageStore
        {
            NeedMessageStore = typeof(TMessageStore) != typeof(MockMessageStore);
            if (NeedMessageStore)
            {
                if (sameIntanceAsBusinessDbContext)
                {
                    IoCFactory.Instance.RegisterType<IMessageStore>(provider => provider.GetService<TMessageStore>(), lifetime);
                }
                else
                {
                    IoCFactory.Instance.RegisterType<IMessageStore, TMessageStore>(lifetime);
                }
            }
            else
            {
                IoCFactory.Instance.RegisterType<IMessageStore, MockMessageStore>(lifetime);
            }
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
            builder.Register<IEventBus, EventBus>(lifetime);
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

        public static T Get<T>(string key)
        {
            return Instance.ConfigurationCore != null ? Instance.ConfigurationCore.GetValue<T>(key) : default(T);
        }

        public static string GetConnectionString(string name)
        {
            return Instance.ConfigurationCore
                           ?.GetConnectionString(name);
        }

        public static string Get(string key)
        {
            return Instance.ConfigurationCore?[key];
        }

        public static string GetAppSetting(string key)
        {
            return Get($"AppSettings:{key}");
        }

        public static T GetAppSetting<T>(string key)
        {
            return Get<T>($"AppSettings:{key}");
        }
    }
}