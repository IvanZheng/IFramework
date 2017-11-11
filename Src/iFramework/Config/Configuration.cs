using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Configuration;
using System.Xml.Linq;
using IFramework.Event;
using IFramework.Event.Impl;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Caching;
using IFramework.Infrastructure.Caching.Impl;
using IFramework.Infrastructure.Logging;
using IFramework.IoC;
using IFramework.Message;
using IFramework.Message.Impl;

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
            IoCFactory.Instance.CurrentContainer
                      .RegisterInstance(typeof(IJsonConvert)
                                        , new DataContractJsonConvert());
            return this;
        }

        public Configuration UseMemoryCahce(Lifetime lifetime = Lifetime.Hierarchical)
        {
            IoCFactory.Instance.CurrentContainer.RegisterType<ICacheManager, MemoryCacheManager>(lifetime);
            return this;
        }

        public Configuration UseMessageStore<TMessageStore>(Lifetime lifetime = Lifetime.Hierarchical)
            where TMessageStore : IMessageStore
        {
            NeedMessageStore = typeof(TMessageStore) != typeof(MockMessageStore);
            IoCFactory.Instance.CurrentContainer.RegisterType<IMessageStore, TMessageStore>(lifetime);
            return this;
        }

        public Configuration UseNoneLogger()
        {
            IoCFactory.Instance.CurrentContainer.RegisterType<ILoggerLevelController, LoggerLevelController>(Lifetime.Singleton);
            IoCFactory.Instance.CurrentContainer
                      .RegisterInstance(typeof(ILoggerFactory)
                                        , new MockLoggerFactory());
            return this;
        }

        public Configuration RegisterDefaultEventBus(Lifetime lifetime = Lifetime.Hierarchical)
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
            IoCFactory.Instance.CurrentContainer
                      .RegisterInstance(provider);
            return this;
        }

        public Configuration RegisterDefaultEventBus(IContainer contaienr, Lifetime lifetime = Lifetime.Hierarchical)
        {
            var container = contaienr ?? IoCFactory.Instance.CurrentContainer;
            container.RegisterInstance(new SyncEventSubscriberProvider());
            container.RegisterType<IEventBus, EventBus>(lifetime);
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

        public static CompilationSection GetCompliationSection()
        {
            return ConfigurationManager.GetSection("system.web/compilation") as CompilationSection;
        }

        public static string GetConnectionString(string name)
        {
            return ConfigurationManager.ConnectionStrings[name]?.ConnectionString;
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
            catch (Exception) { }
            return val;
        }

        public static string GetAppConfig(string keyname, string configPath = "Config")
        {
            var config = ConfigurationManager.AppSettings[keyname];
            try
            {
                if (string.IsNullOrWhiteSpace(config))
                {
                    var filePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, configPath);
                    if (File.Exists(filePath))
                    {
                        using (TextReader reader = new StreamReader(filePath))
                        {
                            var xml = XElement.Load(filePath);
                            if (xml != null)
                            {
                                var element = xml.Elements()
                                                 .SingleOrDefault(e => e.Attribute("key") != null &&
                                                                       e.Attribute("key").Value.Equals(keyname));
                                if (element != null)
                                {
                                    config = element.Attribute("value").Value;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                config = string.Empty;
            }
            return config;
        }
    }
}