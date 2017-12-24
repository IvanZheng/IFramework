using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection
{
    public interface IObjectProvider : IServiceProvider, IDisposable
    {
        IObjectProvider Parent { get; }
        IObjectProvider CreateScope();
        IObjectProvider CreateScope(IServiceCollection serviceCollection);
        IObjectProvider CreateScope(Action<IObjectProviderBuilder> buildAction);
        object GetService(Type t, string name, params Parameter[] parameters);
        object GetService(Type t, params Parameter[] parameters);
        T GetService<T>(params Parameter[] overrides);
        T GetService<T>(string name, params Parameter[] overrides);
        IEnumerable<object> GetAllServices(Type type, params Parameter[] parameters);
        IEnumerable<T> GetAllServices<T>(params Parameter[] parameters);
    }
}