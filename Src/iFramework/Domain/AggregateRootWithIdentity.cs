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
        public TIdentity Id { get; protected set; }

        public string Identity
        {
            get => Id.ToString();
            private set => Id = Activator.CreateInstance(typeof(TIdentity), value) as TIdentity;
        }
    }
}