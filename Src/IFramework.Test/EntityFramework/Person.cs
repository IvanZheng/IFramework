using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using IFramework.Domain;

namespace IFramework.Test.EntityFramework
{
    public enum PersonStatus
    {
        Normal,
        Disabled
    }
    public class Person: BaseEntity
    {
        public string Name { get; protected set; }
        public PersonStatus Status { get; protected set; }
        protected Person()
        {

        }

        public Person(string name)
        {
            Name = name;
            Status = PersonStatus.Disabled;
        }
        public Person(long id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
