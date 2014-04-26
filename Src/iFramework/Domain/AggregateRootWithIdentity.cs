using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using Microsoft.Practices.Unity;
using System.Reflection;
using IFramework.Event;
using IFramework.Event.Impl;

namespace IFramework.Domain
{
    public abstract class Identity
    {
        public abstract new string ToString();
    }

    public abstract class AggregateRoot<TIdentity> : AggregateRoot
        where TIdentity : Identity, new()
    {
        public TIdentity ID { get; protected set; }
        public string Identity
        {
            get
            {
                return ID.ToString();
            }
            private set
            {
                ID = Activator.CreateInstance(typeof(TIdentity), value) as TIdentity;
            }
        }
    }
}
