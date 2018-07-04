using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using Newtonsoft.Json;
using IFramework.JsonNet;
using Xunit;
using Xunit.Abstractions;

namespace IFramework.Test
{
    public struct AClass
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public AClass(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
    public class AException:Exception
    {
        public AException(string message)
            :base(message)
        {
        }
    }
    public class JsonTests
    {
        private readonly ITestOutputHelper _output;

        public JsonTests(ITestOutputHelper output)
        {
            _output = output;
            
           
        }
        [Fact]
        public void SerializeReadonlyObject()
        {
            Configuration.Instance
                      .UseAutofacContainer()
                      .UseJsonNet();

            ObjectProviderFactory.Instance
                                 .Build();
            //var ex = new Exception("test");
            //var json = ex.ToJson();
            //var ex2 = json.ToObject<Exception>();
            //Assert.Equal(ex.Message, ex2.Message);
            var a = new AClass("ddd", "name");
            var aJson = a.ToJson();
            a = aJson.ToJsonObject<AClass>();
            Assert.NotNull(aJson);
            Assert.NotNull(a.Name);

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
