using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using Sample.Domain.Model;
using IFramework.EntityFramework;

namespace Sample.Persistence
{
    public class SampleModelContext : MSDbContext
    {
        public SampleModelContext() : base("SampleModelContext") 
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>()
             .ToTable("Accounts");

            base.OnModelCreating(modelBuilder);

        }

        public DbSet<Account> Accounts { get; set; }
    }

}
