using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using IFramework.Infrastructure;

namespace IFramework.DependencyInjection
{
    public abstract class ObjectProviderBase:IObjectProvider
    {
        protected Dictionary<string, object> ContextStore { get; } = new Dictionary<string, object>();
        public abstract object GetService(Type serviceType);
        public abstract object GetRequiredService(Type serviceType);
        public abstract void Dispose();
        public IObjectProvider Parent { get; set; }
        public abstract IObjectProvider CreateScope();
        public abstract IObjectProvider CreateScope(IServiceCollection serviceCollection);
        public abstract IObjectProvider CreateScope(Action<IObjectProviderBuilder> buildAction);
        public abstract object GetService(Type t, string name, Parameter[] parameters);
        public abstract object GetService(Type t, Parameter[] parameters);
        public abstract T GetService<T>(Parameter[] overrides) where T : class;
        public abstract T GetService<T>(string name, Parameter[] parameters) where T : class;
        public abstract IEnumerable<object> GetAllServices(Type type, Parameter[] parameters);
        public abstract IEnumerable<T> GetAllServices<T>(Parameter[] parameters) where T : class;

        public virtual void SetContextData(string key, object value)
        {
            ContextStore[key] = value;
        }

        public virtual T GetContextData<T>(string key)
        {
            return ContextStore.TryGetValue(key, out var data) ? (T)data : default;
        }
    }
}
