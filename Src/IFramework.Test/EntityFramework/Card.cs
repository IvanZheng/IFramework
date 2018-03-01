using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Infrastructure;

namespace IFramework.Test.EntityFramework
{
    public class Card
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; }

        protected Card()
        {
            
        }

        public Card(string name)
        {
            Id = ObjectId.GenerateNewId().ToString();
            Name = name;
        }
    }
}
