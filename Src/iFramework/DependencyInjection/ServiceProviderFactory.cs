using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection
{
    public class ServiceProviderFactory : IServiceProviderFactory<IObjectProviderBuilder>
    {
        private readonly Action<IObjectProviderBuilder> _configurationAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderFactory"/> class.
        /// </summary>
        /// <param name="configurationAction"></param>
        public ServiceProviderFactory(Action<IObjectProviderBuilder> configurationAction = null)
        {
            _configurationAction = configurationAction ?? (builder => { });
        }

        /// <summary>
        /// Creates a container builder from an <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The collection of services.</param>
        /// <returns>A container builder that can be used to create an <see cref="IServiceProvider" />.</returns>
        public IObjectProviderBuilder CreateBuilder(IServiceCollection services)
        {
            return ObjectProviderFactory.Instance.ObjectProviderBuilder.Populate(services);
        }

        /// <summary>
        /// Creates an <see cref="IServiceProvider" /> from the container builder.
        /// </summary>
        /// <param name="containerBuilder">The container builder.</param>
        /// <returns>An <see cref="IServiceProvider" />.</returns>
        public IServiceProvider CreateServiceProvider(IObjectProviderBuilder containerBuilder)
        {
            if (containerBuilder == null) throw new ArgumentNullException(nameof(containerBuilder));

            
            return ObjectProviderFactory.Instance.Build();
        }
    }
}
