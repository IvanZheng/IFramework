using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.Domain;
using IFramework.EntityFrameworkCore;
using IFramework.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
                         .UseAutofacContainer()
                         .UseConfiguration(builder.Build())
                         .UseCommonComponents()
                         .UseDbContextPool<DemoDbContext>(options =>
                         {
                             options.EnableSensitiveDataLogging();
                             options.UseSqlServer(Configuration.Instance.GetConnectionString(nameof(DemoDbContext)));
                         });

            ObjectProviderFactory.Instance.Build();
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
            var tasks = new List<Task>();
            for (int i = 0; i < 200; i++)
            {
                tasks.Add(GetUsersTest());
            }

            await Task.WhenAll(tasks);
            Console.WriteLine($"incremented : {DemoDbContext.Total }");

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
                    Assert.NotNull(u.GetDbContext<DemoDbContext>());
                }
            }
        }
    }
}