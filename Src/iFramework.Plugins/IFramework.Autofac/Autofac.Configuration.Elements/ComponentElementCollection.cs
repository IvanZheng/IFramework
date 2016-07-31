using System;
namespace Autofac.Configuration.Elements
{
	public class ComponentElementCollection : ConfigurationElementCollection<ComponentElement>
	{
		public ComponentElementCollection() : base("component")
		{
		}
	}
}
