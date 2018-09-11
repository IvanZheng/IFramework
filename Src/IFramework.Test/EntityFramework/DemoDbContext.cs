using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace IFramework.Test.EntityFramework
{
    public class DemoDbContext : MsDbContext
    {
        public static int Total;
        public DemoDbContext(DbContextOptions options)
            : base(options)
        {
            Interlocked.Add(ref Total, 1);
            //Database.EnsureCreated();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
        protected const string NextSequenceId = "NEXT VALUE FOR ids";

        public DbSet<User> Users { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Person> Persons { get;set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //modelBuilder.HasSequence<long>("ids")
            //            .StartsAt(1000)
            //            .IncrementsBy(1);
            modelBuilder.Entity<Person>()
                        .Property(e => e.Id)
                        .HasDefaultValue();
        }
    }
}