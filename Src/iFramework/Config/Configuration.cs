using System;
using System.Collections.Generic;
using System.Configuration;
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
        public IConfiguration ConfigurationCore = new ConfigurationBuilder().Build();

        private Configuration()
        {

        }

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
            ObjectProviderFactory.Instance.RegisterInstance(typeof(Configuration), this);
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
            ObjectProviderFactory.Instance
                      .RegisterType<ILoggerFactory, NullLoggerFactory>(ServiceLifetime.Singleton);
            return this;
        }

        public Configuration RegisterExceptionManager<TExceptionManager>() where TExceptionManager : class, IExceptionManager
        {
            ObjectProviderFactory.Instance
                      .RegisterType<IExceptionManager, TExceptionManager>(ServiceLifetime.Singleton);
            return this;
        }

        private Configuration UserDataContractJson()
        {
            ObjectProviderFactory.Instance
                      .RegisterInstance(typeof(IJsonConvert)
                                        , new DataContractJsonConvert());
            return this;
        }

        public Configuration UseMemoryCahce(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            ObjectProviderFactory.Instance.RegisterType<ICacheManager, MemoryCacheManager>(lifetime);
            return this;
        }

        /// <summary>
        /// if sameIntanceAsBusinessDbContext is true, TMessageStore must be registerd before object provider to be built!
        /// </summary>
        /// <typeparam name="TMessageStore"></typeparam>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public Configuration UseMessageStore<TMessageStore>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TMessageStore : class, IMessageStore
        {
            NeedMessageStore = typeof(TMessageStore) != typeof(MockMessageStore);
            if (NeedMessageStore)
            {
                ObjectProviderFactory.Instance.RegisterType<TMessageStore, TMessageStore>(lifetime);
                ObjectProviderFactory.Instance.RegisterType<IMessageStore>(provider => provider.GetService<TMessageStore>(), lifetime);
            }
            else
            {
                ObjectProviderFactory.Instance.RegisterType<IMessageStore, MockMessageStore>(lifetime);
            }
            return this;
        }

        //public Configuration UseNoneLogger()
        //{
        //    ObjectProviderFactory.Instance.RegisterType<ILoggerLevelController, LoggerLevelController>(ServiceLifetime.Singleton);
        //    ObjectProviderFactory.Instance.RegisterInstance(typeof(ILoggerFactory)
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
            ObjectProviderFactory.Instance.RegisterInstance(provider);
            return this;
        }

        public Configuration RegisterDefaultEventBus(IObjectProviderBuilder builder, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            builder = builder ?? ObjectProviderFactory.Instance.ObjectProviderBuilder;
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

        public T Get<T>(string key)
        {
            T appSetting = default(T);
            if (typeof(T).IsPrimitive)
            {
                appSetting = ConfigurationCore.GetValue<T>(key);
            }
            else
            {
                var configSection = GetSection(key);
                if (configSection.Exists())
                {
                    appSetting = Activator.CreateInstance<T>();
                    configSection.Bind(appSetting);
                }
            }
            if (appSetting == null || appSetting.Equals(default(T)))
            {
                appSetting = GetAppConfig<T>(ConfigurationManager.AppSettings[key]);
            }
            return appSetting;
        }

        public string GetConnectionString(string name)
        {
            return ConfigurationCore.GetConnectionString(name)??
                   Get<ConnectionStringSettings>($"ConnectionStrings:{name}")?.ConnectionString ?? 
                   ConfigurationManager.ConnectionStrings[name]?.ConnectionString;
        }

        public static string Get(string key)
        {
            return Instance.ConfigurationCore?[key] ?? ConfigurationManager.AppSettings[key];;
        }

        private static T GetAppConfig<T>(string appSetting)
        {
            var val = default(T);
            try
            {
                if (typeof(T).IsEquivalentTo(typeof(Guid)))
                {
                    val = (T) Convert.ChangeType(new Guid(appSetting), typeof(T));
                }
                else
                {
                    val = (T) Convert.ChangeType(appSetting, typeof(T));
                }
            }
            catch (Exception) { }
            return val;
        }
    }
}