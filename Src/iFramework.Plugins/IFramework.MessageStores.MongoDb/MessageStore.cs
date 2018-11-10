using IFramework.Domain;
using IFramework.Message.Impl;
using IFramework.MessageStores.Abstracts;
using Microsoft.EntityFrameworkCore;

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

           
        }
    }
}