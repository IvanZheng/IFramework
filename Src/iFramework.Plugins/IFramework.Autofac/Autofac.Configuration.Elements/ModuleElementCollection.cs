using System;
namespace Autofac.Configuration.Elements
{
	public class ModuleElementCollection : ConfigurationElementCollection<ModuleElement>
	{
		public ModuleElementCollection() : base("module")
		{
		}
	}
}
