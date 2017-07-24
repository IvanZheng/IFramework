using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.IoC
{
    public class LogInterceptionAttribute: Attribute
    {
        public string LoggerName { get; set; }
        public bool SerializeArguments { get; set; }
        public bool SerializeReturnValue { get; set; }
    }
}
