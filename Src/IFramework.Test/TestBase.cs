using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IFramework.Config;
using Microsoft.Extensions.Configuration;

namespace IFramework.Test
{
    public class TestBase
    {
        public TestBase()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            Configuration.Instance.UseConfiguration(builder.Build());
        }
    }
}
