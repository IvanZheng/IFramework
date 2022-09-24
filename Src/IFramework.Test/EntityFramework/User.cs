using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

        public string Country { get; set; }
        public string City { get; set; }
        public string Street { get;  set; }
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

        private Address _address;
        public virtual Address Address { get => _address; protected set => _address = value.Clone(); }
        public string Hobby { get; protected set; }
    }
    public class User: TimestampedAggregateRoot
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public string Gender { get; protected set; }
        public Address Address { get; set; }  
        private UserProfile _userProfile;
        [Required]
        public virtual UserProfile UserProfile { get => _userProfile; protected set => _userProfile = value.Clone(); }
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
            UserProfile.Address.City = address;
        }
    }
}
