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
using IFramework.Log4Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IFramework.Test.EntityFramework
{
    public class EntityFrameworkTests
    {
        public EntityFrameworkTests()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            Configuration.Instance
                         //.UseMicrosoftDependencyInjection()
                         .UseUnityContainer()
                         //.UseAutofacContainer()
                         .UseConfiguration(builder.Build())
                         .UseCommonComponents()
                         .UseLog4Net()
                         .UseDbContextPool<DemoDbContext>(options =>
                         {
                             options.EnableSensitiveDataLogging();
                             options.UseInMemoryDatabase(nameof(DemoDbContext));
                             //options.UseSqlServer(Configuration.Instance.GetConnectionString(nameof(DemoDbContext)));
                         },2000);

            ObjectProviderFactory.Instance.Build();
        }

        [Fact]
        public void DbContextPoolTest()
        {
            int hashCode1, hashCode2;
            using (var scope = ObjectProviderFactory.CreateScope())
            {
                var dbContext = scope.GetService<DemoDbContext>();
                hashCode1 = dbContext.GetHashCode();
                dbContext.Database.AutoTransactionsEnabled = false;
            }

            using (var scope = ObjectProviderFactory.CreateScope())
            {
                var dbContext = scope.GetService<DemoDbContext>();
                hashCode2 = dbContext.GetHashCode();
                //Assert.True(dbContext.Database.AutoTransactionsEnabled);
            }

            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public async Task AddUserTest()
        {
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                                                    new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                                                    TransactionScopeAsyncFlowOption.Enabled))
            {
                var dbContext = serviceScope.GetService<DemoDbContext>();


                var user = new User("ivan", "male");
                user.AddCard("ICBC");
                user.AddCard("CCB");
                user.AddCard("ABC");

                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();
                scope.Complete();
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
            await AddUserTest();
            var tasks = new List<Task>();
            for (int i = 0; i < 2000; i++)
            {
                tasks.Add(GetUsersTest());
            }

            await Task.WhenAll(tasks);

            var logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());
            logger.LogDebug($"incremented : {DemoDbContext.Total }");
        }

        [Fact]
        public async Task GetUsersTest()
        {
            using (var scope = ObjectProviderFactory.CreateScope())
            {
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