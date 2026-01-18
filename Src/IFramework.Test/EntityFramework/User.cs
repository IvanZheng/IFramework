using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using IFramework.Domain;
using IFramework.Infrastructure;

namespace IFramework.Test.EntityFramework
{
    public record Address : ValueObject<Address>
    {
        public Address(){}
        public Address(string country, string city, string street)
        {
            Country = country;
            City = city;
            Street = street;
        }
        [Required]
        public string Country { get; init; }
        public string City { get; init;}
        public string Street { get; init;}
        public override bool IsNull()
        {
            return string.IsNullOrWhiteSpace(Country);
        }
    }
    public record UserProfile:ValueObject<UserProfile>
    {
        public UserProfile(Address address, string hobby)
        {
            Address = address;
            Hobby = hobby;
        }

        public UserProfile()
        {
        }
        [Required]
        public Address Address { get; init; } = Address.Empty;
        public string Hobby { get; }
        public override bool IsNull()
        {
            return Address?.IsNull() ?? true;
        }
    }
    public class User: TimestampedAggregateRoot
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public string Gender { get; protected set; }
        [Required]
        public Address Address { get; protected set; }
        [Required]
        public UserProfile UserProfile {get; protected set; }
        public virtual ICollection<Card> Cards { get; set; } = new HashSet<Card>();
        [MaxLength(500)] 
        public List<string> Pictures { get; protected set; } = new List<string>();
        protected User()
        {
           
        }
        public User(string name, string gender, UserProfile profile = null)
        {
            Id = ObjectId.GenerateNewId()
                         .ToString();
            Name = name;
            Gender = gender;
            UserProfile = profile ?? UserProfile.Empty;
            Address = Address.Empty;
        }

        public void ModifyProfile(UserProfile profile)
        {
            UserProfile = profile;
        }

        public void ModifyName(string name)
        {
            Name = name;
        }

        public void AddCard(string cardName)
        {
            Cards.Add(new Card(Id, cardName));
        }

       

        public void RemoveCard(Card card)
        {
            RemoveCollectionEntities(Cards, card);
            //Cards.Remove(card);
        }

        public void RemoveCards()
        {
            Cards.Clear();
        }

        public void UpdateCard(string cardName)
        {
            Cards.FirstOrDefault()?.UpdateName(cardName);
        }

        public void ModifyProfileAddress(string address)
        {
            var newAddress = UserProfile.Address.CloneWith(new
            {
                Street = address
            });
            UserProfile = UserProfile.CloneWith(new {Address = newAddress});
        }
    }
}
