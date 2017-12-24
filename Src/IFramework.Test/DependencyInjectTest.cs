using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using IFramework.DependencyInjection;
//using IFramework.DependencyInjection.Microsoft;
using IFramework.DependencyInjection.Autofac;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IFramework.Test
{
    public interface IA
    {
        C C { get; }
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
    public class A : IA, IDisposable
    {
        public static int ConstructedCount { get; private set; }

        public readonly IB B;
        public C C { get; set; }
        private readonly IObjectProvider _objectProvider;

        public A(IB b, C c, IObjectProvider objectProvider)
        {
            ConstructedCount++;
            B = b;
            C = c;
            _objectProvider = objectProvider;
        }

        public string Do()
        {
            return B.Id + C.Id;
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing");
        }
    }

    public class C
    {
        public int Id { get; set; }

        public C(int id) => Id = id;
    }
    public class DependencyInjectTest
    {
        public static string RootLifetimeTag = "RootLifetimeTag";
        [Fact]
        public void ScopeTest()
        {
            var builder = IoCFactory.Instance.SetProviderBuilder(new ObjectProviderBuilder());

            builder.RegisterType<IB, B>(ServiceLifetime.Singleton)
                   .RegisterType<IA, A>(ServiceLifetime.Scoped);

            var objectProvider = IoCFactory.Instance.Build();
            var b = objectProvider.GetRequiredService<IB>();
            Console.WriteLine($"b: {b.Id}");


            using (var scope = IoCFactory.Instance.ObjectProvider
                                                  .CreateScope(ob => ob.RegisterInstance(new C(1))))
            {
                scope.GetService<IB>();
                var a = scope.GetService<IA>();
                Assert.True(a != null && a.C.Id == 1);
            }
            using (var scope = IoCFactory.Instance.ObjectProvider
                                         .CreateScope(ob => ob.RegisterInstance(new C(2))))
            {
                scope.GetService<IB>();
                var a = scope.GetService<IA>();
                Assert.True(a != null && a.C.Id == 2);
            }

            b = objectProvider.GetRequiredService<IB>();
            IoCFactory.Instance.ObjectProvider.Dispose();
            Console.WriteLine($"b: {b.Id}");
            Assert.Equal(1, B.ConstructedCount);
        }
    }
}
