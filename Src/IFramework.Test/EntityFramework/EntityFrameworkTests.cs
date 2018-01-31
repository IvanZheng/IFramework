using System.IO;
using System.Threading.Tasks;
using IFramework.Config;
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
            Configuration.Instance.UseConfiguration(builder.Build());
        }


        [Fact]
        public async Task AddUserTest()
        {
            using (var dbContext = new DemoDbContext())
            {
                var user = new User("ivan", "male");
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task GetUsersTest()
        {
            using (var dbContext = new DemoDbContext())
            {
                var users = await dbContext.Users
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