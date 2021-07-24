using System;
using System.Collections.Generic;
using System.Configuration;
using IFramework.DependencyInjection;
using IFramework.Event;
using IFramework.Event.Impl;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Caching;
using IFramework.Infrastructure.Caching.Impl;
using IFramework.Infrastructure.Mailboxes;
using IFramework.Infrastructure.Mailboxes.Impl;
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

      

        //public Configuration UseDataContractJson()
        //{
        //    ObjectProviderFactory.Instance
        //                         .RegisterInstance(typeof(IJsonConvert),
        //                                           new DataContractJsonConvert());
        //    return this;
        //}


        //public Configuration UseNoneLogger()
        //{
        //    ObjectProviderFactory.Instance.RegisterType<ILoggerLevelController, LoggerLevelController>(ServiceLifetime.Singleton);
        //    ObjectProviderFactory.Instance.RegisterInstance(typeof(ILoggerFactory)
        //                                         , new MockLoggerFactory());
        //    return this;
        //}

        

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
            if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
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

        public string Get(string key)
        {
            return Instance.ConfigurationCore?[key] ?? ConfigurationManager.AppSettings[key];;
        }

        private T GetAppConfig<T>(string appSetting)
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