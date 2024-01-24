using IFramework.Domain;

namespace Sample.Domain.Model
{
    public record Address : ValueObject<Address>
    {
        public string Country { get; protected set; }

        public Address(string country)
        {
            Country = country;
        }

        public override bool IsNull()
        {
            return string.IsNullOrWhiteSpace(Country);
        }
    }
}