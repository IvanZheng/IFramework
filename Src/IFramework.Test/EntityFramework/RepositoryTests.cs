using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Castle.Core.Resource;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.EntityFrameworkCore;
using IFramework.EntityFrameworkCore.Repositories;
using IFramework.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace IFramework.Test.EntityFramework
{
    public class RepositoryBase<TEntity> : Repository<TEntity> where TEntity: class
    {
        public RepositoryBase(DemoDbContext dbContext, IUnitOfWork unitOfWork) : base(dbContext, unitOfWork) { }

    }
    public class RepositoryTests
    {
        private readonly ITestOutputHelper _output;

        static RepositoryTests()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            Configuration.Instance
                         .UseConfiguration(builder.Build())
                         .UseAutofacContainer(new ContainerBuilder())
                         .UseCommonComponents()
                         .UseEntityFrameworkComponents(typeof(RepositoryBase<>), new []{typeof(DemoDbContext)});

            ObjectProviderFactory.Instance
                                 .RegisterComponents(RegisterComponents, ServiceLifetime.Scoped)
                                 .Build();
        }
        public RepositoryTests(ITestOutputHelper output)
        {
            _output = output;

        }

        private static void RegisterComponents(IObjectProviderBuilder builder, ServiceLifetime lifeTime = ServiceLifetime.Scoped)
        {
            var services = new ServiceCollection();
            services.AddDbContextPool<DemoDbContext>(options => options.UseSqlServer(Configuration.Instance
                                                                                                  .GetConnectionString("DemoDb")));
            builder.Register<IDemoRepository, DemoRepository>(lifeTime);
            builder.Populate(services);
        }

        [Fact]
        public async Task DbContextPoolScopeTest()
        {
            var tasks = new object[10].Select(o => Task.Run(() =>
            {
                using (var scope = ObjectProviderFactory.CreateScope())
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
                using (var scope = ObjectProviderFactory.CreateScope())
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
            using (var scope = ObjectProviderFactory.CreateScope())
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
            using (var scope = ObjectProviderFactory.CreateScope())
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

            using (var scope = ObjectProviderFactory.CreateScope())
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

            using (var scope = ObjectProviderFactory.CreateScope())
            {
                var repository = scope.GetRequiredService<IDemoRepository>();
                user = await repository.GetByKeyAsync<User>(user.Id)
                                       .ConfigureAwait(false);
                Assert.Null(user);
            }
        }
    }
}