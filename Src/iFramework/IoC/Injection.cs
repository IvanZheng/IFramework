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
        public object[] Objects { get; set; }

        public ConstructInjection(params object[] objects)
        {
            Objects = objects;
        }
    }

    public class PropertyInjection : Injection
    {
        public string PropertyName { get; private set; }
        public object PropertyValue { get; private set; }

        public PropertyInjection(string propertyName)
        {
            PropertyName = propertyName;
        }

        public PropertyInjection(string propertyName, object propertyValue)
            : this(propertyName)
        {
            PropertyValue = propertyValue;
        }
    }
}
