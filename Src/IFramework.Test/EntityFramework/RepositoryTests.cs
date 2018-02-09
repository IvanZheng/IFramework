using System;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Resource;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.EntityFrameworkCore;
using IFramework.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace IFramework.Test.EntityFramework
{
    public class RepositoryTests : TestBase
    {
        private ITestOutputHelper _output;
        public RepositoryTests(ITestOutputHelper output)
        {
            _output = output;
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
            var services = new ServiceCollection();
            services.AddDbContextPool<DemoDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DemoDb")));
            builder.Register<IDemoRepository, DemoRepository>(lifeTime);
            builder.Populate(services);
        }

        [Fact]
        public async Task DbContextPoolScopeTest()
        {
            var tasks = new object[10].Select(o => Task.Run(() =>
            {
                using (var scope = IoCFactory.CreateScope())
                {
                    var dbCtx = scope.GetService<DemoDbContext>();
                    var hashCode = dbCtx.GetHashCode();

                    dbCtx = scope.GetService<DemoDbContext>();
                    Assert.Equal(hashCode, dbCtx.GetHashCode());

                    dbCtx = scope.GetService<DemoDbContext>();
                    Assert.Equal(hashCode, dbCtx.GetHashCode());

                    _output.WriteLine($"dbctx hashcode  {hashCode}");
                }
            })).ToArray();
            await Task.WhenAll(tasks);

            tasks = new object[10].Select(o => Task.Run(() =>
            {
                using (var scope = IoCFactory.CreateScope())
                {
                    var dbCtx = scope.GetService<DemoDbContext>();
                    var hashCode = dbCtx.GetHashCode();

                    dbCtx = scope.GetService<DemoDbContext>();
                    Assert.Equal(hashCode, dbCtx.GetHashCode());

                    dbCtx = scope.GetService<DemoDbContext>();
                    Assert.Equal(hashCode, dbCtx.GetHashCode());

                    _output.WriteLine($"dbctx hashcode  {hashCode}");
                }
            })).ToArray();
            await Task.WhenAll(tasks);
        }


        [Fact]
        public async Task CrudTest()
        {
            User user = null;
            using (var scope = IoCFactory.CreateScope())
            {
                var dbCtx = scope.GetRequiredService<DemoDbContext>();

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
                var dbCtx = scope.GetRequiredService<DemoDbContext>();

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