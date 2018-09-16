using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.DependencyInjection.Microsoft;
using IFramework.DependencyInjection.Unity;
using IFramework.Domain;
using IFramework.EntityFrameworkCore;
using IFramework.Infrastructure;
using IFramework.JsonNet;
using IFramework.Log4Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ObjectProvider = IFramework.DependencyInjection.Unity.ObjectProvider;

namespace IFramework.Test.EntityFramework
{

    public class DemoDbContextFactory : IDesignTimeDbContextFactory<DemoDbContext>
    {
        public static string ConnectionStringName = "DemoDbContext.MySql";
        //public static string ConnectionStringName = "DemoDbContext";
        public DemoDbContext CreateDbContext(string[] args)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            var configuratoin = builder.Build();
            var optionsBuilder = new DbContextOptionsBuilder<DemoDbContext>();
            optionsBuilder.UseMySql(configuratoin.GetConnectionString(ConnectionStringName));
            return new DemoDbContext(optionsBuilder.Options);
        }
    }


    public class EntityFrameworkTests
    {
        public EntityFrameworkTests()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            Configuration.Instance
                         //.UseMicrosoftDependencyInjection()
                         //.UseUnityContainer()
                         .UseAutofacContainer()
                         .UseConfiguration(builder.Build())
                         .UseCommonComponents()
                         .UseJsonNet()
                         .UseLog4Net()
                         .UseDbContextPool<DemoDbContext>(options =>
                         {
                             options.EnableSensitiveDataLogging();
                             //options.UseMySql(Configuration.Instance.GetConnectionString(DemoDbContextFactory.ConnectionStringName));
                             options.UseInMemoryDatabase(nameof(DemoDbContext));
                             //options.UseSqlServer(Configuration.Instance.GetConnectionString(DemoDbContextFactory.ConnectionStringName));
                         });

            ObjectProviderFactory.Instance.Build();
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            {
                var dbContext = serviceScope.GetService<DemoDbContext>();
                dbContext.Database.Migrate();
            }
        }

        public class DbTest : IDisposable
        {
            public int Count { get; }

            public DbTest(int count)
            {
                Count = count;
            }
            public void Dispose()
            {
               Console.Write("dispose");
            }
        }

        [Fact]
        public void DbContextPoolTest()
        {
            int hashCode1, hashCode2;
            var services = new ServiceCollection();
            services.AddScoped<DbTest>();
            using (var scope = ObjectProviderFactory.CreateScope(provider => provider.RegisterInstance(new DbTest(3))))
            {
                var dbContext = scope.GetService<DemoDbContext>();
                hashCode1 = dbContext.GetHashCode();
                dbContext.Database.AutoTransactionsEnabled = false;
                var dbTest = scope.GetService<DbTest>();
                Assert.Equal(3, dbTest.Count);
            }

            using (var scope = ObjectProviderFactory.CreateScope(provider => provider.RegisterInstance(new DbTest(1))))
            {
                var dbContext = scope.GetService<DemoDbContext>();
                hashCode2 = dbContext.GetHashCode();
                Assert.True(dbContext.Database.AutoTransactionsEnabled);
                var dbTest = scope.GetService<DbTest>();
                Assert.Equal(1, dbTest.Count);
            }

            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public async Task AddPresonTest()
        {
            try
            {
                using (var serviceScope = ObjectProviderFactory.CreateScope())
                {
                    var dbContext = serviceScope.GetService<DemoDbContext>();
                    var person = new Person("ivan");
                    dbContext.Persons.Add(person);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
       
        }

        


        [Fact]
        public async Task ConcurrentUdpateTest()
        {
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            {
                var concurrencyProcessor = serviceScope.GetService<IConcurrencyProcessor>();
                var dbContext = serviceScope.GetService<DemoDbContext>();
                using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                                   new TransactionOptions
                                                                   {
                                                                       IsolationLevel = IsolationLevel.ReadCommitted
                                                                   },
                                                                   TransactionScopeAsyncFlowOption.Enabled))
                {
                    await concurrencyProcessor.ProcessAsync(async () =>
                    {
                        var account = await dbContext.Users.FirstOrDefaultAsync();
                        account.ModifyName($"ivan{DateTime.Now}");
                        await dbContext.SaveChangesAsync();
                    });
                    transactionScope.Complete();
                }
            }
        }

        [Fact]
        public async Task ConcurrentTest()
        {
            var logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());

            try
            {
                await AddUserTest();
                var tasks = new List<Task>();
                for (int i = 0; i < 2000; i++)
                {
                    tasks.Add(GetUsersTest());
                }

                await Task.WhenAll(tasks);

                logger.LogDebug($"incremented : {DemoDbContext.Total }");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"incremented : {DemoDbContext.Total }");
                throw;
            }
        }

        [Fact]
        public async Task AddUserTest()
        {

            using (var serviceScope = ObjectProviderFactory.CreateScope())
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                                                    new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                                                    TransactionScopeAsyncFlowOption.Enabled))
            {
                var serviceProvider = serviceScope.GetService<IServiceProvider>();
                if (serviceProvider == null)
                {
                    Assert.NotNull(serviceProvider);
                }

                try
                {
                    var dbContext = serviceProvider.GetService<DemoDbContext>();
                    if (dbContext == null)
                    {
                        var logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());
                        logger.LogError((serviceScope as ObjectProvider)?.UnityContainer.Registrations.ToJson());
                        Assert.NotNull(dbContext);
                    }
                    
                    var user = new User("ivan", "male");
                    user.AddCard("ICBC");
                    user.AddCard("CCB");
                    user.AddCard("ABC");

                    dbContext.Users.Add(user);
                    await dbContext.SaveChangesAsync();
                    scope.Complete();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }


               
            }
        }

        [Fact]
        public async Task GetUsersTest()
        {
            using (var scope = ObjectProviderFactory.CreateScope())
            {
                var serviceProvider = scope.GetService<IServiceProvider>();
                if (serviceProvider == null)
                {
                    var logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());
                    logger.LogError((scope as ObjectProvider)?.UnityContainer.Registrations.ToJson());
                    Assert.NotNull(serviceProvider);
                }
                var dbContext = scope.GetService<DemoDbContext>();
                var users = await dbContext.Users
                                           //.Include(u => u.Cards)
                                           .FindAll(u => !string.IsNullOrWhiteSpace(u.Name))
                                           .Take(10)
                                           .ToArrayAsync();
                foreach (var u in users)
                {
                    await u.LoadCollectionAsync(u1 => u1.Cards);
                    Assert.Equal(u.GetDbContext<DemoDbContext>().GetHashCode(), dbContext.GetHashCode());
                }
            }
        }
    }
}