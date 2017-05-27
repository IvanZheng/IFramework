using System.Collections.Generic;
using Autofac.Configuration.Util;
using Autofac.Core;

namespace Autofac.Configuration.Elements
{
    public class ParameterElementCollection : NamedConfigurationElementCollection<ParameterElement>
    {
        public ParameterElementCollection() : base("parameter", "name")
        {
        }

        public IEnumerable<Parameter> ToParameters()
        {
            foreach (var current in this)
            {
                var localParameter = current;
                yield return new ResolvedParameter((pi, c) => pi.Name == localParameter.Name,
                    (pi, c) => TypeManipulation.ChangeToCompatibleType(localParameter.CoerceValue(), pi.ParameterType,
                        pi));
            }
        }
    }
}