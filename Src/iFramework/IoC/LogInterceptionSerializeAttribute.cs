using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.IoC
{
    public class LogInterceptionSerializeAttribute: Attribute
    {
        public bool SerializeArguments { get; set; }
        public bool SerializeReturnValue { get; set; }
    }
}
