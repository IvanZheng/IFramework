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
    public class SampleModelContextCreateDatabaseIfNotExists : DropCreateDatabaseIfModelChanges<SampleModelContext>
    {
        protected override void Seed(SampleModelContext context)
        {
            base.Seed(context);
        }
    }

    public class SampleModelContext : MessageStore
    {
        static SampleModelContext()
        {
            Database.SetInitializer(new SampleModelContextCreateDatabaseIfNotExists());
        }

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
                        .ToTable("Accounts")
                        .HasMany<ProductId>(a => a.ProductIds)
                        .WithRequired()
                        .HasForeignKey(pid => pid.AccountId);

            modelBuilder.Entity<ProductId>()
                        .HasKey(p => new { p.Value, p.AccountId });

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Product> Products { get; set; }
    }

}
