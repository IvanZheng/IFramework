using System;
using System.Collections.Generic;
using System.Text;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using Newtonsoft.Json;
using JsonNetCore = IFramework.JsonNetCore;
using Xunit;
using Xunit.Abstractions;

namespace IFramework.Test
{
    public class AEvent
    {
        public AEvent(string message)
        {
            Message = message;
        }
        public string Message { get; }
    }
    public class JsonTests: TestBase
    {
        private readonly ITestOutputHelper _output;
        public JsonTests(ITestOutputHelper output)
        {
            _output = output;
            Configuration.Instance
                         .UseAutofacContainer()
                         .UseCommonComponents();
            JsonNetCore.FrameworkConfigurationExtension.UseJsonNet(Configuration.Instance);

            IoCFactory.Instance
                      .Build();
        }
        [Fact]
        public void SerializeReadonlyObject()
        {
            var e = new AEvent("test");
            var json = e.ToJson();
            var e2 = json.ToJsonObject<AEvent>();
            Assert.Equal(e.Message, e2.Message);


            var de = new DomainException(1, "test");
            var json2 = de.ToJson();
            var de2 = json2.ToJsonObject<DomainException>();
            Assert.Equal(de.Message, de2.Message);
        }
    }
}
