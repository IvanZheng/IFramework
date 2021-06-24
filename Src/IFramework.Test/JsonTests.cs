using System;
using System.Collections.Generic;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.Domain;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.JsonNet;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace IFramework.Test
{
    public class AValueObject<T> : ValueObject<T> 
        where T : ValueObject
    {
        public AValueObject()
        {
            CreatedTime = DateTime.Now;
        }

        public new static T Empty => Activator.CreateInstance<T>();
        public DateTime CreatedTime { get; set; }
    }

    public class AClass : AValueObject<AClass>
    {
        public AClass(){}
        public AClass(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; protected set; }
        public string Name { get; protected set;}
    }

    public class AException : Exception
    {
        public AException(string message)
            : base(message) { }
    }

    public class JsonTests
    {
        public JsonTests(ITestOutputHelper output)
        {
            var services = new ServiceCollection();
            services.AddAutofacContainer()
                    .AddJsonNet();

            ObjectProviderFactory.Instance
                                 .Build(services);
            _output = output;
        }

        private readonly ITestOutputHelper _output;

        [Fact]
        public void CloneTest()
        {
            var a = new AClass("ddd", "name");
            var cloneObject = a.Clone();
            Assert.True(a == cloneObject);
            
            cloneObject = a.Clone(new {Name = "ivan"});
            Assert.True("ivan" == cloneObject.Name);

            var list = new List<AClass>{a};
            var listClone = list.Clone();
            Assert.True(listClone[0] == a);
        }

        [Fact]
        public void SerializeReadonlyObject()
        {
            //var ex = new Exception("test");
            //var json = ex.ToJson();
            //var ex2 = json.ToObject<Exception>();
            //Assert.Equal(ex.Message, ex2.Message);
            var a = new AClass("ddd", "name");
            var aJson = a.ToJson();
            var b = aJson.ToJsonObject<AClass>();
            Assert.NotNull(aJson);
            Assert.NotNull(b.Name);
            Assert.Equal(a.CreatedTime, b.CreatedTime);


            var de = new DomainException(1, "test");
            var json2 = de.ToJson();
            var de2 = json2.ToJsonObject<DomainException>();
            Assert.Equal(de.Message, de2.Message);
            Assert.Equal(de.ErrorCode, de2.ErrorCode);

            var e = new AException("test");
            var json = e.ToJson();
            var e2 = json.ToJsonObject<AException>();
            Assert.Equal(e.Message, e2.Message);


            de = new DomainException("2", "test");
            json2 = de.ToJson();
            de2 = json2.ToJsonObject<DomainException>();
            Assert.Equal(de.Message, de2.Message);
            Assert.Equal(de.ErrorCode, de2.ErrorCode);

            de = new DomainException(null, "test");
            json2 = de.ToJson();
            de2 = json2.ToJsonObject<DomainException>();
            Assert.Equal(de.Message, de2.Message);
            Assert.Equal(de.ErrorCode, de2.ErrorCode);
        }
    }
}