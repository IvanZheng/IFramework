using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Domain;
using IFramework.EntityFrameworkCore;
using IFramework.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Pomelo.EntityFrameworkCore.MySql.Internal;
using Pomelo.EntityFrameworkCore.MySql.Query.Expressions.Internal;
using Pomelo.EntityFrameworkCore.MySql.Storage.Internal;

namespace IFramework.Test.EntityFramework
{
    public class DemoDbContext : MsDbContext
    {
        public static int Total;
        private long _tenantId;

        public DemoDbContext(DbContextOptions options)
            : base(options)
        {
            Interlocked.Add(ref Total, 1);
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public void InitializeTenant()
        {
            _tenantId = this.GetService<IObjectProvider>().GetContextData<long>("TenantId");
        }

        protected const string NextSequenceId = "NEXT VALUE FOR ids";

        public DbSet<User> Users { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Person> Persons { get;set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //modelBuilder.Ignore<Entity>();
            //modelBuilder.Ignore<AggregateRoot>();
            //modelBuilder.Ignore<TimestampedAggregateRoot>();
            //modelBuilder.Ignore<BaseEntity>();
            //modelBuilder.HasSequence<long>("ids")
            //            .StartsAt(1000)
            //            .IncrementsBy(1);

            var userEntity = modelBuilder.Entity<User>();
            userEntity.OwnsOne(u => u.UserProfile, b =>
            {
                b.OwnsOne(p => p.Address);
                b.Property(p => p.Hobby)
                 .IsConcurrencyToken();
            });
            //userEntity.HasMany(u => u.Cards)
            //            .WithOne()
            //            .HasForeignKey(c => c.UserId);
            //modelBuilder.Ignore<Card>();

            modelBuilder.Entity<User>()
                        .HasIndex(u => u.Name)
                        .IsUnique();

            modelBuilder.Owned<Address>();
            //modelBuilder.Owned<UserProfile>();
            modelBuilder.Entity<Person>()
                        .Property(e => e.Id);

            modelBuilder.Entity<User>()
                        .Property(u => u.Address)
                        .HasJsonConversion();

            modelBuilder.Entity<User>()
                        .Property(u => u.Pictures)
                        .HasJsonConversion();

            modelBuilder.AddSqlFunctions();
        }



        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries().ToArray();

            entries.Where(e => e.Entity is IAggregateRoot).ForEach(e =>
            {
                if (IsEntryModified(e))
                {
                    Console.WriteLine(e.State);
                }
            });
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}