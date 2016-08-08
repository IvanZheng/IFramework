using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.Message.Impl
{
    public class HandlerTypeInfo
    {
        public Type Type { get; set; }
        public bool IsAsync { get; set; }

        public HandlerTypeInfo(Type type, bool isAsync)
        {
            Type = type;
            IsAsync = isAsync;
        }
    }
}
