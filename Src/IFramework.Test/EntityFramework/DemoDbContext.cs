using IFramework.Config;
using IFramework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IFramework.Test.EntityFramework
{
    public class DemoDbContext : MsDbContext
    {
        public DemoDbContext()
            : base(new DbContextOptionsBuilder<DemoDbContext>().UseSqlServer(Configuration.GetConnectionString("DemoDb"))
                                                               .Options)
        {
            Database.EnsureCreated();
        }

        public DbSet<User> Users { get; set; }
    }
}