using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Domain;
using IFramework.Infrastructure;

namespace IFramework.Test.EntityFramework
{
    public class User: TimestampedAggregateRoot
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public string Gender { get; protected set; }
        public DateTime CreatedTime { get; protected set; }
        public DateTime ModifiedTime { get; protected set; }
        public virtual ICollection<Card> Cards { get; } = new HashSet<Card>();

        protected User()
        {
            
        }
        public User(string name, string gender)
        {
            Id = ObjectId.GenerateNewId()
                         .ToString();
            Name = name;
            Gender = gender;
            CreatedTime = ModifiedTime = DateTime.Now;

        }

        public void ModifyName(string name)
        {
            Name = name;
            ModifiedTime = DateTime.Now;
        }

        public void AddCard(string cardName)
        {
            Cards.Add(new Card(cardName));
            ModifiedTime = DateTime.Now;
        }
    }
}
