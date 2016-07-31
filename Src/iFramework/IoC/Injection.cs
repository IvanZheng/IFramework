using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.IoC
{
    public interface Injection
    {

    }

    public class ConstructInjection : Injection
    {
        public ParameterInjection[] Parameters { get; set; }

        public ConstructInjection(params ParameterInjection[] parameters)
        {
            Parameters = parameters;
        }
    }

    public class ParameterInjection : Injection
    {
        public string ParameterName { get; private set; }
        public object ParameterValue { get; private set; }

        public ParameterInjection(string propertyName)
        {
            ParameterName = propertyName;
        }

        public ParameterInjection(string propertyName, object propertyValue)
            : this(propertyName)
        {
            ParameterValue = propertyValue;
        }
    }
}
