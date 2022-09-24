using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using IFramework.Infrastructure;

namespace IFramework.Test.EntityFramework
{
    public class Card
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public string UserId { get; protected set; }
        protected Card()
        {
            
        }

        public Card(string userId, string name)
        {
            Id = ObjectId.GenerateNewId().ToString();
            UserId = userId;
            Name = name;
        }

        public void UpdateName(string cardName)
        {
            Name = cardName;
        }
    }
}
