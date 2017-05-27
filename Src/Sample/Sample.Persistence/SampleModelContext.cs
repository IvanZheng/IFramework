using System.Data.Entity;
using IFramework.MessageStoring;
using Sample.Domain.Model;

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

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Product> Products { get; set; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>()
                .ToTable("Accounts")
                .HasMany(a => a.ProductIds)
                .WithRequired()
                .HasForeignKey(pid => pid.AccountId);

            modelBuilder.Entity<ProductId>()
                .HasKey(p => new {p.Value, p.AccountId});

            base.OnModelCreating(modelBuilder);
        }
    }
}