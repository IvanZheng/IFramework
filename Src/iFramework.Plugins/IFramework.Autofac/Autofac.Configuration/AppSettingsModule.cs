using Autofac.Configuration.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Reflection;
namespace Autofac.Configuration
{
	public class AppSettingsModule : Module
	{
		private readonly IEnumerable<Module> _modules;
		public AppSettingsModule(IEnumerable<Module> modules)
		{
			this._modules = modules;
		}
		protected override void Load(ContainerBuilder builder)
		{
			NameValueCollection appSettings = ConfigurationManager.AppSettings;
			string[] allKeys = appSettings.AllKeys;
			string[] array = allKeys;
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				if (text.Count((char c) => c == '.') == 1)
				{
					string[] array2 = text.Split(new char[]
					{
						'.'
					});
					string moduleName = array2[0];
					string name = array2[1];
					string value = appSettings[text];
					Module module = this._modules.FirstOrDefault((Module m) => m.GetType().Name == moduleName + "Module");
					if (module != null)
					{
						PropertyInfo property = module.GetType().GetProperty(name);
						object value2 = TypeManipulation.ChangeToCompatibleType(value, property.PropertyType, property);
						property.SetValue(module, value2, null);
					}
				}
			}
			foreach (Module current in this._modules)
			{
				ModuleRegistrationExtensions.RegisterModule(builder, current);
			}
		}
	}
}
