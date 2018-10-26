using IFramework.Domain;

namespace Sample.Domain.Model
{
    public class Address : ValueObject<Address>
    {
        public string Country { get; protected set; }

        public Address(string country)
        {
            Country = country;
        }
    }
}