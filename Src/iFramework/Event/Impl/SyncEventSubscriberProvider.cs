using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Event.Impl;

namespace IFramework.Event.Impl
{
    public class SyncEventSubscriberProvider: EventSubscriberProvider
    {
        public SyncEventSubscriberProvider()
        {
            
        }
        public SyncEventSubscriberProvider(params string[] assemblies)
            :base(assemblies)
        {
            
        }

    }
}
