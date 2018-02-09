using IFramework.Config;
using IFramework.MessageStores.Sqlserver;
using Microsoft.EntityFrameworkCore;
using Sample.Domain.Model;

namespace Sample.Persistence
{
    public class SampleModelContext : MessageStore
    {
        public SampleModelContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Product> Products { get; set; }

        public override void Dispose()
        {
            base.Dispose();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>()
                        .ToTable("Accounts")
                        .HasMany(a => a.ProductIds)
                        .WithOne()
                        .HasForeignKey(pid => pid.AccountId);

            modelBuilder.Entity<ProductId>()
                        .HasKey(p => new {p.Value, p.AccountId});

        }
    }
}