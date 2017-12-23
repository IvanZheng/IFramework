using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using IFramework.Event;
using IFramework.Event.Impl;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Caching;
using IFramework.Infrastructure.Caching.Impl;
using IFramework.Infrastructure.Logging;
using IFramework.DependencyInjection;
using IFramework.Message;
using IFramework.Message.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.Config
{
    public class Configuration
    {
        public static readonly Configuration Instance = new Configuration();

        private Configuration() { }

        public bool NeedMessageStore { get; protected set; }


        private bool CommitPerMessage { get; set; }

        public Configuration RegisterCommonComponents()
        {
            UseNoneLogger();
            UseMemoryCahce();
            UserDataContractJson();
            UseMessageStore<MockMessageStore>();
            this.UseMockMessageQueueClient();
            this.UseMockMessagePublisher();
            RegisterDefaultEventBus();
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
            where TMessageStore : IMessageStore
        {
            NeedMessageStore = typeof(TMessageStore) != typeof(MockMessageStore);
            IoCFactory.Instance.RegisterType<IMessageStore, TMessageStore>(lifetime);
            return this;
        }

        public Configuration UseNoneLogger()
        {
            IoCFactory.Instance.RegisterType<ILoggerLevelController, LoggerLevelController>(ServiceLifetime.Singleton);
            IoCFactory.Instance.RegisterInstance(typeof(ILoggerFactory)
                                        , new MockLoggerFactory());
            return this;
        }

        public Configuration RegisterDefaultEventBus(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return RegisterDefaultEventBus(null, lifetime);
        }

        /// <summary>
        /// should use after RegisterCommonComponents
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

        public static string GetAppConfig(string keyname, string configPath = "Config")
        {
            //TODO:
            var config = string.Empty;
            //var config = ConfigurationManager.AppSettings[keyname];
            //try
            //{
            //    if (string.IsNullOrWhiteSpace(config))
            //    {
            //        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath);
            //        if (File.Exists(filePath))
            //        {
            //            using (TextReader reader = new StreamReader(filePath))
            //            {
            //                var xml = XElement.Load(filePath);
            //                var element = xml?.Elements()
            //                                 .SingleOrDefault(e => e.Attribute("key") != null &&
            //                                                       e.Attribute("key").Value.Equals(keyname));
            //                if (element != null)
            //                {
            //                    config = element.Attribute("value").Value;
            //                }
            //            }
            //        }
            //    }
            //}
            //catch (Exception)
            //{
            //    config = string.Empty;
            //}
            return config;
        }
    }
}