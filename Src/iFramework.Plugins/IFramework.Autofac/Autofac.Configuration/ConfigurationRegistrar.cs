using Autofac.Builder;
using Autofac.Configuration.Elements;
using Autofac.Configuration.Util;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
namespace Autofac.Configuration
{
	public class ConfigurationRegistrar : IConfigurationRegistrar
	{
		private IEnumerable<Service> EnumerateComponentServices(ComponentElement component, Assembly defaultAssembly)
		{
			if (!string.IsNullOrEmpty(component.Service))
			{
				Type type = this.LoadType(component.Service, defaultAssembly);
				if (!string.IsNullOrEmpty(component.Name))
				{
					yield return new KeyedService(component.Name, type);
				}
				else
				{
					yield return new TypedService(type);
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(component.Name))
				{
					throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.ServiceTypeMustBeSpecified, new object[]
					{
						component.Name
					}));
				}
			}
			foreach (ServiceElement current in component.Services)
			{
				Type type2 = this.LoadType(current.Type, defaultAssembly);
				if (!string.IsNullOrEmpty(current.Name))
				{
					yield return new KeyedService(current.Name, type2);
				}
				else
				{
					yield return new TypedService(type2);
				}
			}
			yield break;
		}
		public virtual void RegisterConfigurationSection(ContainerBuilder builder, SectionHandler configurationSection)
		{
			if (builder == null)
			{
				throw new ArgumentNullException("builder");
			}
			if (configurationSection == null)
			{
				throw new ArgumentNullException("configurationSection");
			}
			this.RegisterConfiguredModules(builder, configurationSection);
			this.RegisterConfiguredComponents(builder, configurationSection);
			this.RegisterReferencedFiles(builder, configurationSection);
		}
		protected virtual void RegisterConfiguredComponents(ContainerBuilder builder, SectionHandler configurationSection)
		{
			if (builder == null)
			{
				throw new ArgumentNullException("builder");
			}
			if (configurationSection == null)
			{
				throw new ArgumentNullException("configurationSection");
			}
			foreach (ComponentElement current in configurationSection.Components)
			{
				IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> registrationBuilder = RegistrationExtensions.RegisterType(builder, this.LoadType(current.Type, configurationSection.DefaultAssembly));
				IEnumerable<Service> enumerable = this.EnumerateComponentServices(current, configurationSection.DefaultAssembly);
				foreach (Service current2 in enumerable)
				{
					registrationBuilder.As(new Service[]
					{
						current2
					});
				}
				foreach (Parameter current3 in current.Parameters.ToParameters())
				{
					RegistrationExtensions.WithParameter<object, ConcreteReflectionActivatorData, SingleRegistrationStyle>(registrationBuilder, current3);
				}
				foreach (Parameter current4 in current.Properties.ToParameters())
				{
					RegistrationExtensions.WithProperty<object, ConcreteReflectionActivatorData, SingleRegistrationStyle>(registrationBuilder, current4);
				}
				foreach (MetadataElement current5 in current.Metadata)
				{
					registrationBuilder.WithMetadata(current5.Name, TypeManipulation.ChangeToCompatibleType(current5.Value, Type.GetType(current5.Type), null));
				}
                if (!string.IsNullOrEmpty(current.MemberOf))
                {
                    MemberOf<object, ConcreteReflectionActivatorData, SingleRegistrationStyle>(registrationBuilder, current.MemberOf);
                }
                this.SetLifetimeScope<ConcreteReflectionActivatorData, SingleRegistrationStyle>(registrationBuilder, current.InstanceScope);
				this.SetComponentOwnership<ConcreteReflectionActivatorData, SingleRegistrationStyle>(registrationBuilder, current.Ownership);
				this.SetInjectProperties<ConcreteReflectionActivatorData, SingleRegistrationStyle>(registrationBuilder, current.InjectProperties);
				this.SetAutoActivate<ConcreteReflectionActivatorData, SingleRegistrationStyle>(registrationBuilder, current.AutoActivate);
			}
		}

        public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> MemberOf<TLimit, TActivatorData, TSingleRegistrationStyle>(IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration, string collectionName) where TSingleRegistrationStyle : SingleRegistrationStyle
        {
            if (registration == null)
            {
                throw new ArgumentNullException("registration");
            }
            Enforce.ArgumentNotNull(collectionName, "collectionName");
            registration.OnRegistered<TLimit, TActivatorData, TSingleRegistrationStyle>(delegate (ComponentRegisteredEventArgs e)
            {
                IDictionary<string, object> metadata = e.ComponentRegistration.Metadata;
                if (metadata.ContainsKey("Autofac.CollectionRegistrationExtensions.MemberOf"))
                {
                    metadata["Autofac.CollectionRegistrationExtensions.MemberOf"] = ((IEnumerable<string>)metadata["Autofac.CollectionRegistrationExtensions.MemberOf"]).Union<string>(new string[] { collectionName });
                }
                else
                {
                    metadata.Add("Autofac.CollectionRegistrationExtensions.MemberOf", new string[] { collectionName });
                }
            });
            return registration;
        }



        protected virtual void RegisterConfiguredModules(ContainerBuilder builder, SectionHandler configurationSection)
		{
			if (builder == null)
			{
				throw new ArgumentNullException("builder");
			}
			if (configurationSection == null)
			{
				throw new ArgumentNullException("configurationSection");
			}
			foreach (ModuleElement current in configurationSection.Modules)
			{
				Type type = this.LoadType(current.Type, configurationSection.DefaultAssembly);
				IModule module = null;
				using (ReflectionActivator reflectionActivator = new ReflectionActivator(type, new DefaultConstructorFinder(), new MostParametersConstructorSelector(), current.Parameters.ToParameters(), current.Properties.ToParameters()))
				{
					module = (IModule)reflectionActivator.ActivateInstance(new ContainerBuilder().Build(0), Enumerable.Empty<Parameter>());
				}
				ModuleRegistrationExtensions.RegisterModule(builder, module);
			}
		}
		protected virtual void RegisterReferencedFiles(ContainerBuilder builder, SectionHandler configurationSection)
		{
			if (builder == null)
			{
				throw new ArgumentNullException("builder");
			}
			if (configurationSection == null)
			{
				throw new ArgumentNullException("configurationSection");
			}
			foreach (FileElement current in configurationSection.Files)
			{
				SectionHandler configurationSection2 = SectionHandler.Deserialize(current.Name, current.Section);
				this.RegisterConfigurationSection(builder, configurationSection2);
			}
		}
		protected virtual void SetInjectProperties<TReflectionActivatorData, TSingleRegistrationStyle>(IRegistrationBuilder<object, TReflectionActivatorData, TSingleRegistrationStyle> registrar, string injectProperties) where TReflectionActivatorData : ReflectionActivatorData where TSingleRegistrationStyle : SingleRegistrationStyle
		{
			if (registrar == null)
			{
				throw new ArgumentNullException("registrar");
			}
			if (string.IsNullOrWhiteSpace(injectProperties))
			{
				return;
			}
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
			throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.UnrecognisedInjectProperties, new object[]
			{
				injectProperties
			}));
		}
		protected virtual void SetAutoActivate<TReflectionActivatorData, TSingleRegistrationStyle>(IRegistrationBuilder<object, TReflectionActivatorData, TSingleRegistrationStyle> registrar, string autoActivate) where TReflectionActivatorData : ReflectionActivatorData where TSingleRegistrationStyle : SingleRegistrationStyle
		{
			if (registrar == null)
			{
				throw new ArgumentNullException("registrar");
			}
			if (string.IsNullOrWhiteSpace(autoActivate))
			{
				return;
			}
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
				RegistrationExtensions.AutoActivate<object, TReflectionActivatorData, TSingleRegistrationStyle>(registrar);
				return;
			}
			throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.UnrecognisedAutoActivate, new object[]
			{
				autoActivate
			}));
		}
		protected virtual void SetComponentOwnership<TReflectionActivatorData, TSingleRegistrationStyle>(IRegistrationBuilder<object, TReflectionActivatorData, TSingleRegistrationStyle> registrar, string ownership) where TReflectionActivatorData : ReflectionActivatorData where TSingleRegistrationStyle : SingleRegistrationStyle
		{
			if (registrar == null)
			{
				throw new ArgumentNullException("registrar");
			}
			if (string.IsNullOrWhiteSpace(ownership))
			{
				return;
			}
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
			throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.UnrecognisedOwnership, new object[]
			{
				ownership
			}));
		}
		protected virtual void SetLifetimeScope<TReflectionActivatorData, TSingleRegistrationStyle>(IRegistrationBuilder<object, TReflectionActivatorData, TSingleRegistrationStyle> registrar, string lifetimeScope) where TReflectionActivatorData : ReflectionActivatorData where TSingleRegistrationStyle : SingleRegistrationStyle
		{
			if (registrar == null)
			{
				throw new ArgumentNullException("registrar");
			}
			if (string.IsNullOrWhiteSpace(lifetimeScope))
			{
				return;
			}
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
			throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.UnrecognisedScope, new object[]
			{
				lifetimeScope
			}));
		}
		protected virtual Type LoadType(string typeName, Assembly defaultAssembly)
		{
			if (typeName == null)
			{
				throw new ArgumentNullException("typeName");
			}
			if (typeName.Length == 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.ArgumentMayNotBeEmpty, new object[]
				{
					"type name"
				}), "typeName");
			}
			Type type = Type.GetType(typeName);
			if (type == null && defaultAssembly != null)
			{
				type = defaultAssembly.GetType(typeName, false);
			}
			if (type == null)
			{
				throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture, ConfigurationSettingsReaderResources.TypeNotFound, new object[]
				{
					typeName
				}));
			}
			return type;
		}
	}
}
