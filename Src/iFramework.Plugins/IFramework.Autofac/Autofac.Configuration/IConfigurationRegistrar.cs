using System;
namespace Autofac.Configuration
{
	public interface IConfigurationRegistrar
	{
		void RegisterConfigurationSection(ContainerBuilder builder, SectionHandler configurationSection);
	}
}
