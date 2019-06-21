using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Domain;
using IFramework.Infrastructure;

namespace IFramework.Test.EntityFramework
{
    public class Address:ValueObject<Address>
    {
        public Address(){}
        public Address(string country, string city, string street)
        {
            Country = country;
            City = city;
            Street = street;
        }

        public string Country { get; protected set; }
        public string City { get;protected set; }
        public string Street { get; protected set; }
    }
    public class UserProfile:ValueObject<UserProfile>
    {
        public UserProfile(Address address, string hobby)
        {
            Address = address;
            Hobby = hobby;
        }

        public UserProfile()
        {
        }

        public virtual Address Address { get; protected set; }
        public string Hobby { get; protected set; }
    }
    public class User: AggregateRoot
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public string Gender { get; protected set; }

        public virtual UserProfile UserProfile { get; protected set; }
        public virtual ICollection<Card> Cards { get; set; } = new HashSet<Card>();

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
    }
}
