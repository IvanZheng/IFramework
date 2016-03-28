using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using Sample.Domain.Model;
using IFramework.EntityFramework;
using IFramework.MessageStoring;

namespace Sample.Persistence
{
    public class SampleModelContext : MessageStore
    {
        public SampleModelContext() : base("SampleModelContext") 
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>()
             .ToTable("Accounts");

            base.OnModelCreating(modelBuilder);

        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Product> Products { get; set; }
    }

}
