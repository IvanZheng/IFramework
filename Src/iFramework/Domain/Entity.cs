using Newtonsoft.Json;

namespace IFramework.Domain
{
    public class Entity : IEntity
    {
        [JsonIgnore]
        public virtual object DomainContext { get; private set; }

        internal void SetDomainContext(object domainContext)
        {
            DomainContext = domainContext;
        }
    }
}