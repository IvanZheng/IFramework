using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Logging
{
    public class AopEnteringLog: JsonLogBase
    {
        public string Action { get; set; } = Entering;
        public Dictionary<string, object> Parameters { get; set; }
    }
}
