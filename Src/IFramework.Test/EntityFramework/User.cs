using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Domain;
using IFramework.Infrastructure;
using MongoDB.Bson.Serialization.Attributes;

namespace IFramework.Test.EntityFramework
{
    public class User: TimestampedAggregateRoot
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public string Gender { get; protected set; }

        [BsonElement]
        public virtual ICollection<Card> Cards { get; set; } = new HashSet<Card>();

        protected User()
        {
            
        }
        public User(string name, string gender)
        {
            Id = ObjectId.GenerateNewId()
                         .ToString();
            Name = name;
            Gender = gender;

        }

        public void ModifyName(string name)
        {
            Name = name;
        }

        public void AddCard(string cardName)
        {
            Cards.Add(new Card(Id, cardName));
        }
    }
}
