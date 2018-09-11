using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using IFramework.Domain;

namespace IFramework.Test.EntityFramework
{
    public class Person: BaseEntity
    {
        public string Name { get; set; }

        protected Person()
        {

        }

        public Person(string name)
        {
            Name = name;
        }
        public Person(long id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
