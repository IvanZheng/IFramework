using IFramework.Config;
using IFramework.MessageStores.Relational;
using Microsoft.EntityFrameworkCore;
using Sample.Domain.Model;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using IFramework.DependencyInjection;
using IFramework.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Sample.DTO.Exceptions;

namespace Sample.Persistence
{
    public class SampleModelContext : MessageStore//IFramework.MessageStores.MongoDb.MessageStore
    {
        private const string UniqueConstaintErrorMessage = "在'{0}'中不能有重复的'{1}', 重复的值为'{2}'.";
        private readonly ILogger _logger = ObjectProviderFactory.GetService<ILogger<SampleModelContext>>();
        /// <summary>
        ///     Cannot insert duplicate key row in object 'dbo.AssetBrokers' with unique index 'IX_AssetBrokers_Code'. The
        ///     duplicate key value is (111).
        /// </summary>
        private static readonly Regex UniqueConstraintRegex =
            new Regex(@"(['])(?:(?!\1).)*?\1|\(([^)]*)\)", RegexOptions.Compiled);

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
            modelBuilder.Ignore<People>();

            modelBuilder.Entity<Account>()
                        .HasIndex(a => a.Email)
                        .IsUnique();
            modelBuilder.Entity<Account>()
                        .HasKey(a => a.Id);

            modelBuilder.Entity<Account>(b =>
            {
                b.HasMany(a => a.Profiles)
                 .WithOne()
                 .HasForeignKey("AccountId");
            });


            modelBuilder.Entity<Account>()
                        //.ToTable("Accounts")
                        .HasMany(a => a.ProductIds)
                        .WithOne()
                        .HasForeignKey(pid => pid.AccountId);

            modelBuilder.Entity<ProductId>()
                        .HasKey(p => new { p.Value, p.AccountId });

        }


        protected override void OnException(Exception ex)
        {
            base.OnException(ex);
            if (ex is DbUpdateException dbUpdateException && ex.GetBaseException() is SqlException sqlException)
            {
                Exception exception = null;
                switch (sqlException.Number)
                {
                    case 2627: // Unique constraint error
                        exception = new DomainException(DTO.ErrorCode.UniqueConstraint, new object[] { sqlException.Message }, ex);
                        break;
                    case 547: // Constraint check violation
                        exception = new DomainException(DTO.ErrorCode.ConstraintCheckViolation, new object[] { sqlException.Message }, ex);
                        break;
                    case 2601: // Duplicated key row error
                        // Constraint violation exception
                        // A custom exception of yours for concurrency issues
                        exception = UniqueErrorFormatter(sqlException, dbUpdateException.Entries);
                        break;
                }

                if (exception != null)
                {
                    _logger.LogDebug(exception);
                    throw exception;
                }
            }
        }

        public UniqueConstraintException UniqueErrorFormatter(SqlException ex, IReadOnlyList<EntityEntry> entitiesNotSaved)
        {
            try
            {
                var message = ex.Errors[0].Message;
                var matches = UniqueConstraintRegex.Matches(message);

                if (matches.Count != 3)
                {
                    return new UniqueConstraintException(ex);
                }

                var entityDisplayName = entitiesNotSaved.Count == 1
                                            ? entitiesNotSaved.FirstOrDefault()?.Entity.GetType().Name
                                            : matches[0].Value.Replace("\'", string.Empty);

                var indexName = matches[1].Value.Replace("\'", string.Empty);
                var duplicatedValue = matches[2].Value;
                if (duplicatedValue.Length > 2)
                {
                    duplicatedValue = duplicatedValue.Substring(1, duplicatedValue.Length - 2);
                }

                var returnError = string.Format(UniqueConstaintErrorMessage,
                                                entityDisplayName?.Split('.').LastOrDefault(),
                                                indexName?.Split('_').LastOrDefault(),
                                                duplicatedValue);

                return new UniqueConstraintException(ex, returnError, entityDisplayName, indexName, duplicatedValue);
            }
            catch (Exception e)
            {
                return new UniqueConstraintException(ex);
            }
        }

    }
}