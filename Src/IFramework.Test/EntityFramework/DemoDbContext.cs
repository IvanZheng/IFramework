using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace IFramework.Test.EntityFramework
{
    public class DemoDbContext : MsDbContext
    {
        internal DemoDbContext()
            : this(new DbContextOptionsBuilder()//.UseInMemoryDatabase(nameof(DemoDbContext))
                                                .UseSqlServer(Configuration.Instance
                                                                           .GetConnectionString("DemoDb"))
                                                .Options)
        {
        }
        

        public DemoDbContext(DbContextOptions options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Card> Cards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}