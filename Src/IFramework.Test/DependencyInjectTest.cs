using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Microsoft;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IFramework.Test
{
    public interface IA
    {
        string Do();
    }

    public interface IB
    {
        string Id { get; set; }
    }
    public class B : IB
    {
        public static int ConstructedCount { get; private set; }
        public string Id { get; set; }

        public B()
        {
            ConstructedCount++;
            Id = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }

    }
    public class A : IA
    {
        public static int ConstructedCount { get; private set; }

        private readonly IB _b;
        private readonly IServiceProvider _serviceProvider;

        public A(IB b, IServiceProvider serviceProvider)
        {
            ConstructedCount++;
            _b = b;
            _serviceProvider = serviceProvider;
        }

        public string Do()
        {
            return _b.Id;
        }
    }
    public class DependencyInjectTest
    {
        public static string RootLifetimeTag = "RootLifetimeTag";
        [Fact]
        public void ScopeTest()
        {
            var builder = IoCFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder());

            builder.RegisterType<IB, B>(ServiceLifetime.Singleton);
            //.RegisterType<IA, A>(ServiceLifetime.Scoped);

            var objectProvider = IoCFactory.Instance.Build();
            var b = objectProvider.GetRequiredService<IB>();
            Console.WriteLine($"b: {b.Id}");


            using (var scope = IoCFactory.Instance.ObjectProvider.CreateScope())
            {
                scope.GetService<IB>();
                var scopedServiceProvider = scope.GetService<IServiceProvider>();
                var a = scope.GetService<IA>();
                Assert.True(a != null);
            }
            b = objectProvider.GetRequiredService<IB>();
            Console.WriteLine($"b: {b.Id}");
            Assert.Equal(1, B.ConstructedCount);
        }
    }
}
