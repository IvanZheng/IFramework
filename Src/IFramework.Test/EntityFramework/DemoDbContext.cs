using IFramework.Config;
using IFramework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IFramework.Test.EntityFramework
{
    public class DemoDbContext : MsDbContext
    {
        internal DemoDbContext()
            : this(new DbContextOptionsBuilder()//.UseInMemoryDatabase(nameof(DemoDbContext))
                                                .UseSqlServer(Configuration.GetConnectionString("DemoDb"))
                                                .Options)
        {
        }

        public DemoDbContext(DbContextOptions options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<User> Users { get; set; }
    }
}