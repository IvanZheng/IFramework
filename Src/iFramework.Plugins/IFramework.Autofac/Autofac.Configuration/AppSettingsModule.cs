using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Autofac.Configuration.Util;

namespace Autofac.Configuration
{
    public class AppSettingsModule : Module
    {
        private readonly IEnumerable<Module> _modules;

        public AppSettingsModule(IEnumerable<Module> modules)
        {
            _modules = modules;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var appSettings = ConfigurationManager.AppSettings;
            var allKeys = appSettings.AllKeys;
            var array = allKeys;
            for (var i = 0; i < array.Length; i++)
            {
                var text = array[i];
                if (text.Count(c => c == '.') == 1)
                {
                    var array2 = text.Split('.');
                    var moduleName = array2[0];
                    var name = array2[1];
                    var value = appSettings[text];
                    var module = _modules.FirstOrDefault(m => m.GetType().Name == moduleName + "Module");
                    if (module != null)
                    {
                        var property = module.GetType().GetProperty(name);
                        var value2 = TypeManipulation.ChangeToCompatibleType(value, property.PropertyType, property);
                        property.SetValue(module, value2, null);
                    }
                }
            }
            foreach (var current in _modules)
            {
                builder.RegisterModule(current);
            }
        }
    }
}