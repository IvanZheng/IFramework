using System;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Microsoft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace IFramework.Test
{
    public class DependencyInjectTest
    {
        [Fact]
        public void Test1()
        {
            var builder = IoCFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder());

            var provider = builder.Build(new ServiceCollection());

            using (var scope = provider.CreateScope())
            {
                
            }
        }
    }
}
