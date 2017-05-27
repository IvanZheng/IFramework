using System;

namespace IFramework.Domain
{
    public abstract class Identity
    {
        public new abstract string ToString();
    }

    public abstract class AggregateRoot<TIdentity> : AggregateRoot
        where TIdentity : Identity, new()
    {
        public TIdentity ID { get; protected set; }

        public string Identity
        {
            get => ID.ToString();
            private set => ID = Activator.CreateInstance(typeof(TIdentity), value) as TIdentity;
        }
    }
}