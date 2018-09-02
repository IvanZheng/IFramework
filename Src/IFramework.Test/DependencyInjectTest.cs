using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Core.Registration;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Unity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

//using IFramework.DependencyInjection.Microsoft;

namespace IFramework.Test
{
    public interface IA
    {
        IC C { get; }
        string Do();
    }

    public interface IB
    {
        string Id { get; set; }
    }

    public class B : IB
    {
        public B()
        {
            ConstructedCount++;
            Id = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }

        public static int ConstructedCount { get; set; }
        public string Id { get; set; }
    }

    public class B2 : IB
    {
        public B2()
        {
            Id = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }

        public string Id { get; set; }
    }

    public class A : IA, IDisposable
    {
        private readonly IObjectProvider _objectProvider;

        public readonly IB B;

        public A(IB b, IC c, IObjectProvider objectProvider)
        {
            ConstructedCount++;
            B = b;
            C = c;
            _objectProvider = objectProvider;
        }

        public static int ConstructedCount { get; private set; }
        public IC C { get; set; }

        public string Do()
        {
            return B.Id + C.Id;
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing");
        }
    }

    public interface IC
    {
        int Id { get; set; }
    }

    public class C : IC
    {
        public C(int id)
        {
            Id = id;
        }

        public int Id { get; set; }
    }

    public interface IPerson
    {
        [Transaction]
        [LogInterceptor]
        Task DoAsync(string str);

        [Transaction]
        [LogInterceptor]
        Task<(string, int)> DoAsync(string str, int i);

        [Transaction]
        [LogInterceptor]
        void Do(string str);

        [Transaction]
        [LogInterceptor]
        (string, int) Do(string str, int i);
    }

    public class Person : IPerson
    {
        public Task<(string, int)> DoAsync(string str, int i)
        {
            return Task.FromResult((str, i));
        }

        public void Do(string str)
        {
            Console.WriteLine(str);
        }

        public (string, int) Do(string str, int i)
        {
            return (str, i);
        }

        public Task DoAsync(string str)
        {
            Console.WriteLine(str);
            return Task.Delay(10);
        }
    }

    public class DependencyInjectTest
    {
        public IObjectProviderBuilder GetUnityBuilder()
        {
            var builder = new ObjectProviderBuilder();
            var services = new ServiceCollection();
            services.AddLogging();
            builder.Populate(services);
            return builder;
        }

        private IObjectProviderBuilder GetAutofacBuilder()
        {
            B.ConstructedCount = 0;
            //Configuration.Instance
            //             .UseAutofacContainer();
            //.UseMicrosoftDependencyInjection();
            var builder = new DependencyInjection.Autofac.ObjectProviderBuilder();
            var services = new ServiceCollection();
            services.AddLogging();
            builder.Populate(services);
            return builder;
        }

        private IObjectProviderBuilder GetMsBuilder()
        {
            B.ConstructedCount = 0;
            //Configuration.Instance
            //             .UseAutofacContainer();
            //.UseMicrosoftDependencyInjection();
            //return new DependencyInjection.Autofac.ObjectProviderBuilder();
            var builder = new DependencyInjection.Microsoft.ObjectProviderBuilder();
            var services = new ServiceCollection();
            services.AddLogging();
            builder.Populate(services);
            return builder;
        }


        [Fact]
        public void GetAllServicesTest()
        {
            var builder = GetUnityBuilder();
            builder.Register<IB, B>("B")
                   .Register<IB, B2>("B2");
            var objectProvider = builder.Build();
            var bSet = objectProvider.GetAllServices<IB>();
            Assert.NotNull(bSet);
            Assert.Equal(2, bSet.Count());
            objectProvider.Dispose();
        }

        [Fact]
        public void GetRequiredServiceTest()
        {
            var builder = GetUnityBuilder();

            builder.Register<IB, B>(ServiceLifetime.Singleton)
                   .Register<IB, B2>("B2");
            var objectProvider = builder.Build();

            var b = objectProvider.GetService<IB>("B");
            Assert.Null(b);

            b = objectProvider.GetService<B2>();
            Assert.Null(b);

            b = objectProvider.GetService<IB>("B2");
            Assert.NotNull(b);

            b = objectProvider.GetRequiredService(typeof(IB)) as IB;
            Assert.NotNull(b);

            Assert.Throws<ComponentNotRegisteredException>(() => objectProvider.GetRequiredService<B2>());
            objectProvider.Dispose();
        }

        [Fact]
        public async Task InterceptTest()
        {
            var builder = GetUnityBuilder();
            builder.Register<IPerson, Person>(ServiceLifetime.Scoped,
                                              new InterfaceInterceptorInjection(),
                                              new InterceptionBehaviorInjection());
            var provider = builder.Build();
            using (var scope = provider.CreateScope())
            {
                var person = scope.GetService<IPerson>();

                await person.DoAsync("ivan");

                var result2 = await person.DoAsync("ivan", 10);
                Assert.Equal("ivan", result2.Item1);
                Assert.Equal(10, result2.Item2);

                person.Do("ivan2");

                result2 = person.Do("ivan2", 20);
                Assert.Equal("ivan2", result2.Item1);
                Assert.Equal(20, result2.Item2);
            }
        }

        [Fact]
        public void MsSameServiceTest()
        {
            var builder = GetUnityBuilder();
            builder.Register<IB>(p => new B(), ServiceLifetime.Scoped)
                   .Register<B, B>(ServiceLifetime.Scoped);
            var provider = builder.Build();
            var hashCode1 = 0;
            var hashCode2 = 0;
            using (var scope = provider.CreateScope())
            {
                var b1 = scope.GetService<IB>();
                var b2 = scope.GetService<IB>();

                Assert.Equal(b1.GetHashCode(), b2.GetHashCode());
                hashCode1 = b1.GetHashCode();
            }

            using (var scope = provider.CreateScope())
            {
                hashCode2 = scope.GetService<IB>().GetHashCode();
            }

            Assert.NotEqual(hashCode1, hashCode2);
        }

        [Fact]
        public void OverrideInjectTest()
        {
            var builder = GetUnityBuilder(); //GetMsBuilder();

            builder.Register<IB, B2>(ServiceLifetime.Singleton);
            builder.Register<IB, B>(ServiceLifetime.Singleton);
            var objectProvider = builder.Build();
            using (var scope = objectProvider.CreateScope())
            {
                var b = scope.GetService<IB>();
                Assert.True(b is B);
            }
        }

        [Fact]
        public void SameServiceTest()
        {
            var builder = GetUnityBuilder();
            builder.Register<IB>(p => new B(), ServiceLifetime.Scoped)
                   .Register<B, B>(ServiceLifetime.Scoped);
            var provider = builder.Build();
            using (var scope = provider.CreateScope(ob => ob.RegisterInstance(typeof(IC), new C(1))))
            {
                var b1 = scope.GetService<IB>();
                var b2 = scope.GetService<IB>();

                Assert.Equal(b1.GetHashCode(), b2.GetHashCode());
            }
        }


        [Fact]
        public void ScopeTest()
        {
            var builder = GetUnityBuilder();

            builder.Register<IB, B>(ServiceLifetime.Singleton);
            builder.Register<A, A>();
            var objectProvider = builder.Build();
            Assert.NotNull(objectProvider.GetService<IB>());

            using (var scope = objectProvider.CreateScope(ob => ob.RegisterInstance(typeof(IC), new C(1))))
            {
                scope.GetService<IB>();
                Assert.True(scope.GetService(typeof(A)) is A a && a.C.Id == 1);
            }

            using (var scope = objectProvider.CreateScope(ob => ob.RegisterInstance(typeof(IC), new C(2))))
            {
                scope.GetService<IB>();
                var a = scope.GetService<A>();
                Assert.True(a != null && a.C.Id == 2);
            }

            var b = objectProvider.GetService<IB>();
            objectProvider.Dispose();
            Console.WriteLine($"b: {b.Id}");
            Assert.Equal(1, B.ConstructedCount);
        }
    }
}