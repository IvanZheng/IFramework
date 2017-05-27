using System;

namespace Autofac.Configuration.Core
{
    public abstract class ConfigurationModule : Module
    {
        public IConfigurationRegistrar ConfigurationRegistrar { get; set; }

        public SectionHandler SectionHandler { get; protected set; }

        protected override void Load(ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");
            if (SectionHandler == null)
                throw new InvalidOperationException(ConfigurationSettingsReaderResources.InitializeSectionHandler);
            var configurationRegistrar = ConfigurationRegistrar ?? new ConfigurationRegistrar();
            configurationRegistrar.RegisterConfigurationSection(builder, SectionHandler);
        }
    }
}