using System;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.EntityFrameworkCore;
using IFramework.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IFramework.Test.EntityFramework
{
    public class RepositoryTests : TestBase
    {
        public RepositoryTests()
        {
            Configuration.Instance
                         .UseAutofacContainer()
                         .RegisterCommonComponents()
                         .RegisterEntityFrameworkComponents();

            IoCFactory.Instance
                      .RegisterComponents(RegisterComponents, ServiceLifetime.Scoped)
                      .Build();
        }

        private void RegisterComponents(IObjectProviderBuilder builder, ServiceLifetime lifeTime = ServiceLifetime.Scoped)
        {
            builder.RegisterType<DemoDbContext, DemoDbContext>(lifeTime);
            builder.RegisterType<IDemoRepository, DemoRepository>(lifeTime);
        }

        [Fact]
        public async Task CrudTest()
        {
            User user = null;
            using (var scope = IoCFactory.CreateScope())
            {
                var unitOfWork = scope.GetRequiredService<IAppUnitOfWork>();
                var repository = scope.GetRequiredService<IDemoRepository>();
                user = new User($"ivan-{DateTime.Now.Ticks}", "male");
                repository.Add(user);
                await unitOfWork.CommitAsync()
                                .ConfigureAwait(false);
            }
            var newName = $"new name {DateTime.Now.Ticks}";
            using (var scope = IoCFactory.CreateScope())
            {
                var repository = scope.GetRequiredService<IDemoRepository>();
                var unitOfWork = scope.GetRequiredService<IAppUnitOfWork>();
                user = await repository.GetByKeyAsync<User>(user.Id)
                                            .ConfigureAwait(false);
                Assert.NotNull(user);
                user.ModifyName(newName);
                await unitOfWork.CommitAsync()
                                .ConfigureAwait(false);
            }

            using (var scope = IoCFactory.CreateScope())
            {
                var repository = scope.GetRequiredService<IDemoRepository>();
                var unitOfWork = scope.GetRequiredService<IAppUnitOfWork>();
                user = await repository.GetByKeyAsync<User>(user.Id)
                                       .ConfigureAwait(false);
                Assert.True(user.Name == newName);
                repository.Remove(user);
                await unitOfWork.CommitAsync()
                                .ConfigureAwait(false);
            }

            using (var scope = IoCFactory.CreateScope())
            {
                var repository = scope.GetRequiredService<IDemoRepository>();
                user = await repository.GetByKeyAsync<User>(user.Id)
                                       .ConfigureAwait(false);
                Assert.Null(user);
            }
        }


    }
}