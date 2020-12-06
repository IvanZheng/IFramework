using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Domain;
using IFramework.Infrastructure;

namespace Sample.Domain.Model
{
    public class AacountProfile:Entity
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; }
        public string Email{get; protected set; }

        public AacountProfile( )
        {
        }
        public AacountProfile(string name, string email)
        {
            Id = ObjectId.GenerateNewId()
                         .ToString();
            Name = name;
            Email = email;
        }
    }
}
