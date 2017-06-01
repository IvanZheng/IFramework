using System.Collections.Generic;
using System.Reflection;
using Autofac.Configuration.Util;
using Autofac.Core;

namespace Autofac.Configuration.Elements
{
    public class PropertyElementCollection : NamedConfigurationElementCollection<PropertyElement>
    {
        public PropertyElementCollection() : base("property", "name") { }

        public IEnumerable<Parameter> ToParameters()
        {
            foreach (var current in this)
            {
                var localParameter = current;
                yield return new ResolvedParameter(delegate(ParameterInfo pi, IComponentContext c)
                {
                    PropertyInfo propertyInfo;
                    return pi.TryGetDeclaringProperty(out propertyInfo) && propertyInfo.Name == localParameter.Name;
                }, delegate(ParameterInfo pi, IComponentContext c)
                {
                    PropertyInfo memberInfo = null;
                    pi.TryGetDeclaringProperty(out memberInfo);
                    return TypeManipulation.ChangeToCompatibleType(localParameter.CoerceValue(), pi.ParameterType,
                                                                   memberInfo);
                });
            }
        }
    }
}