using System.IO;
using System.Threading.Tasks;
using System.Transactions;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.Infrastructure;
using IFramework.Log4Net;
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
                         .UseConfiguration(builder.Build());
        }

        [Fact]
        public async Task AddUserTest()
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                                                    new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                                                    TransactionScopeAsyncFlowOption.Enabled))
            {
                using (var dbContext = new DemoDbContext())
                {
                    var user = new User("ivan", "male");
                    user.AddCard("ICBC");
                    user.AddCard("CCB");
                    user.AddCard("ABC");

                    dbContext.Users.Add(user);
                    await dbContext.SaveChangesAsync();
                }

                scope.Complete();
            }
        }

        [Fact]
        public async Task GetUsersTest()
        {
            using (var dbContext = new DemoDbContext())
            {
                var users = await dbContext.Users
                                           .Include(u => u.Cards)
                                           .FindAll(u => !string.IsNullOrWhiteSpace(u.Name))
                                           .ToArrayAsync();
                users.ForEach(u =>
                {
                    Assert.NotNull(u.GetDbContext<DemoDbContext>());
                });
            }
        }
    }
}