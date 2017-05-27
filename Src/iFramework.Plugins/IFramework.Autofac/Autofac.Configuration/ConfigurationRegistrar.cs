using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Autofac.Builder;
using Autofac.Configuration.Elements;
using Autofac.Configuration.Util;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;

namespace Autofac.Configuration
{
    public class ConfigurationRegistrar : IConfigurationRegistrar
    {
        public virtual void RegisterConfigurationSection(ContainerBuilder builder, SectionHandler configurationSection)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");
            if (configurationSection == null)
                throw new ArgumentNullException("configurationSection");
            RegisterConfiguredModules(builder, configurationSection);
            RegisterConfiguredComponents(builder, configurationSection);
            RegisterReferencedFiles(builder, configurationSection);
        }

        private IEnumerable<Service> EnumerateComponentServices(ComponentElement component, Assembly defaultAssembly)
        {
            if (!string.IsNullOrEmpty(component.Service))
            {
                var type = LoadType(component.Service, defaultAssembly);
                if (!string.IsNullOrEmpty(component.Name))
                    yield return new KeyedService(component.Name, type);
                else
                    yield return new TypedService(type);
            }
            else
            {
                if (!string.IsNullOrEmpty(component.Name))
                    throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                        ConfigurationSettingsReaderResources.ServiceTypeMustBeSpecified, component.Name));
            }
            foreach (var current in component.Services)
            {
                var type2 = LoadType(current.Type, defaultAssembly);
                if (!string.IsNullOrEmpty(current.Name))
                    yield return new KeyedService(current.Name, type2);
                else
                    yield return new TypedService(type2);
            }
        }

        protected virtual void RegisterConfiguredComponents(ContainerBuilder builder,
            SectionHandler configurationSection)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");
            if (configurationSection == null)
                throw new ArgumentNullException("configurationSection");
            foreach (var current in configurationSection.Components)
            {
                var registrationBuilder =
                    builder.RegisterType(LoadType(current.Type, configurationSection.DefaultAssembly));
                var enumerable = EnumerateComponentServices(current, configurationSection.DefaultAssembly);
                foreach (var current2 in enumerable)
                    registrationBuilder.As(current2);
                foreach (var current3 in current.Parameters.ToParameters())
                    registrationBuilder.WithParameter(current3);
                foreach (var current4 in current.Properties.ToParameters())
                    registrationBuilder.WithProperty(current4);
                foreach (var current5 in current.Metadata)
                    registrationBuilder.WithMetadata(current5.Name,
                        TypeManipulation.ChangeToCompatibleType(current5.Value, Type.GetType(current5.Type), null));
                if (!string.IsNullOrEmpty(current.MemberOf))
                    MemberOf(registrationBuilder, current.MemberOf);
                SetLifetimeScope(registrationBuilder, current.InstanceScope);
                SetComponentOwnership(registrationBuilder, current.Ownership);
                SetInjectProperties(registrationBuilder, current.InjectProperties);
                SetAutoActivate(registrationBuilder, current.AutoActivate);
            }
        }

        public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle>
            MemberOf<TLimit, TActivatorData, TSingleRegistrationStyle>(
                IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
                string collectionName) where TSingleRegistrationStyle : SingleRegistrationStyle
        {
            if (registration == null)
                throw new ArgumentNullException("registration");
            Enforce.ArgumentNotNull(collectionName, "collectionName");
            registration.OnRegistered(delegate(ComponentRegisteredEventArgs e)
            {
                var metadata = e.ComponentRegistration.Metadata;
                if (metadata.ContainsKey("Autofac.CollectionRegistrationExtensions.MemberOf"))
                    metadata["Autofac.CollectionRegistrationExtensions.MemberOf"] =
                        ((IEnumerable<string>) metadata["Autofac.CollectionRegistrationExtensions.MemberOf"]).Union(
                            new[] {collectionName});
                else
                    metadata.Add("Autofac.CollectionRegistrationExtensions.MemberOf", new[] {collectionName});
            });
            return registration;
        }


        protected virtual void RegisterConfiguredModules(ContainerBuilder builder, SectionHandler configurationSection)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");
            if (configurationSection == null)
                throw new ArgumentNullException("configurationSection");
            foreach (var current in configurationSection.Modules)
            {
                var type = LoadType(current.Type, configurationSection.DefaultAssembly);
                IModule module = null;
                using (var reflectionActivator = new ReflectionActivator(type, new DefaultConstructorFinder(),
                    new MostParametersConstructorSelector(), current.Parameters.ToParameters(),
                    current.Properties.ToParameters()))
                {
                    module = (IModule) reflectionActivator.ActivateInstance(new ContainerBuilder().Build(0),
                        Enumerable.Empty<Parameter>());
                }
                builder.RegisterModule(module);
            }
        }

        protected virtual void RegisterReferencedFiles(ContainerBuilder builder, SectionHandler configurationSection)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");
            if (configurationSection == null)
                throw new ArgumentNullException("configurationSection");
            foreach (var current in configurationSection.Files)
            {
                var configurationSection2 = SectionHandler.Deserialize(current.Name, current.Section);
                RegisterConfigurationSection(builder, configurationSection2);
            }
        }

        protected virtual void SetInjectProperties<TReflectionActivatorData, TSingleRegistrationStyle>(
            IRegistrationBuilder<object, TReflectionActivatorData, TSingleRegistrationStyle> registrar,
            string injectProperties) where TReflectionActivatorData : ReflectionActivatorData
            where TSingleRegistrationStyle : SingleRegistrationStyle
        {
            if (registrar == null)
                throw new ArgumentNullException("registrar");
            if (string.IsNullOrWhiteSpace(injectProperties))
                return;
            string key;
            switch (key = injectProperties.Trim().ToUpperInvariant())
            {
                case "NO":
                case "N":
                case "FALSE":
                case "0":
                    return;
                case "YES":
                case "Y":
                case "TRUE":
                case "1":
                    registrar.PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies);
                    return;
            }
            throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                ConfigurationSettingsReaderResources.UnrecognisedInjectProperties, injectProperties));
        }

        protected virtual void SetAutoActivate<TReflectionActivatorData, TSingleRegistrationStyle>(
            IRegistrationBuilder<object, TReflectionActivatorData, TSingleRegistrationStyle> registrar,
            string autoActivate) where TReflectionActivatorData : ReflectionActivatorData
            where TSingleRegistrationStyle : SingleRegistrationStyle
        {
            if (registrar == null)
                throw new ArgumentNullException("registrar");
            if (string.IsNullOrWhiteSpace(autoActivate))
                return;
            string key;
            switch (key = autoActivate.Trim().ToUpperInvariant())
            {
                case "NO":
                case "N":
                case "FALSE":
                case "0":
                    return;
                case "YES":
                case "Y":
                case "TRUE":
                case "1":
                    registrar.AutoActivate();
                    return;
            }
            throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                ConfigurationSettingsReaderResources.UnrecognisedAutoActivate, autoActivate));
        }

        protected virtual void SetComponentOwnership<TReflectionActivatorData, TSingleRegistrationStyle>(
            IRegistrationBuilder<object, TReflectionActivatorData, TSingleRegistrationStyle> registrar,
            string ownership) where TReflectionActivatorData : ReflectionActivatorData
            where TSingleRegistrationStyle : SingleRegistrationStyle
        {
            if (registrar == null)
                throw new ArgumentNullException("registrar");
            if (string.IsNullOrWhiteSpace(ownership))
                return;
            string a;
            if ((a = ownership.Trim().ToUpperInvariant()) != null)
            {
                if (a == "LIFETIME-SCOPE" || a == "LIFETIMESCOPE")
                {
                    registrar.OwnedByLifetimeScope();
                    return;
                }
                if (a == "EXTERNAL" || a == "EXTERNALLYOWNED")
                {
                    registrar.ExternallyOwned();
                    return;
                }
            }
            throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                ConfigurationSettingsReaderResources.UnrecognisedOwnership, ownership));
        }

        protected virtual void SetLifetimeScope<TReflectionActivatorData, TSingleRegistrationStyle>(
            IRegistrationBuilder<object, TReflectionActivatorData, TSingleRegistrationStyle> registrar,
            string lifetimeScope) where TReflectionActivatorData : ReflectionActivatorData
            where TSingleRegistrationStyle : SingleRegistrationStyle
        {
            if (registrar == null)
                throw new ArgumentNullException("registrar");
            if (string.IsNullOrWhiteSpace(lifetimeScope))
                return;
            string key;
            switch (key = lifetimeScope.Trim().ToUpperInvariant())
            {
                case "SINGLEINSTANCE":
                case "SINGLE-INSTANCE":
                    registrar.SingleInstance();
                    return;
                case "INSTANCE-PER-LIFETIME-SCOPE":
                case "INSTANCEPERLIFETIMESCOPE":
                case "PER-LIFETIME-SCOPE":
                case "PERLIFETIMESCOPE":
                    registrar.InstancePerLifetimeScope();
                    return;
                case "INSTANCE-PER-DEPENDENCY":
                case "INSTANCEPERDEPENDENCY":
                case "PER-DEPENDENCY":
                case "PERDEPENDENCY":
                    registrar.InstancePerDependency();
                    return;
            }
            throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                ConfigurationSettingsReaderResources.UnrecognisedScope, lifetimeScope));
        }

        protected virtual Type LoadType(string typeName, Assembly defaultAssembly)
        {
            if (typeName == null)
                throw new ArgumentNullException("typeName");
            if (typeName.Length == 0)
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture,
                        ConfigurationSettingsReaderResources.ArgumentMayNotBeEmpty, "type name"), "typeName");
            var type = Type.GetType(typeName);
            if (type == null && defaultAssembly != null)
                type = defaultAssembly.GetType(typeName, false);
            if (type == null)
                throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                    ConfigurationSettingsReaderResources.TypeNotFound, typeName));
            return type;
        }
    }
}