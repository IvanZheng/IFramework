using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Infrastructure.Logging
{
    public class AopLeavingLog: JsonLogBase
    {
        public string Action { get; set; } = Leaving;
        public double CostTime { get; set; } 
        public object Result { get; set; }
    }
}
