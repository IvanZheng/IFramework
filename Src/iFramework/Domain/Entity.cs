using Newtonsoft.Json;

namespace IFramework.Domain
{
    public class Entity : IEntity
    {
        [JsonIgnore]
        public object DomainContext { get; set; }
    }
}