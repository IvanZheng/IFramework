using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.IoC
{
    public class Parameter
    {
        public string Name { get; private set; }
        public object Value { get; private set; }

        public Parameter(string parameterName, object parameterValue)
        {
            Name = parameterName;
            Value = parameterValue;
        }
    }
}
