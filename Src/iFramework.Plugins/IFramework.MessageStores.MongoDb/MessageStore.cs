using IFramework.Domain;
using IFramework.Message.Impl;
using IFramework.MessageStores.Abstracts;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace IFramework.MessageStores.MongoDb
{
    public abstract class MessageStore : Abstracts.MessageStore
    {
        protected MessageStore(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<SagaInfo>();
            modelBuilder.Ignore<AggregateRoot>();
            modelBuilder.Ignore<Entity>();
            modelBuilder.Ignore<TimestampedAggregateRoot>();
            modelBuilder.Ignore<VersionedAggregateRoot>();
            modelBuilder.Ignore<ValueObject<SagaInfo>>();
            modelBuilder.Ignore<UnSentMessage>();
            modelBuilder.Ignore<Abstracts.Message>();

            modelBuilder.Entity<UnPublishedEvent>()
                        .HasKey(e => e.Id);
            modelBuilder.Entity<UnSentCommand>()
                        .HasKey(c => c.Id);
            modelBuilder.Entity<Abstracts.Command>()
                        .HasKey(c => c.Id);
            modelBuilder.Entity<Abstracts.Event>()
                        .HasKey(e => e.Id);
            modelBuilder.Entity<HandledEvent>()
                        .HasKey(e => e.Id);
        }
    }
}