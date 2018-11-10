using IFramework.MessageStores.Abstracts;
using Microsoft.EntityFrameworkCore;

namespace IFramework.MessageStores.Relational
{
    public abstract class MessageStore : Abstracts.MessageStore
    {
        protected MessageStore(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HandledEvent>()
                        .ToTable("msgs_HandledEvents");

            modelBuilder.Entity<Abstracts.Command>()
                        .ToTable("msgs_Commands");

            modelBuilder.Entity<Abstracts.Event>()
                        .ToTable("msgs_Events");

            modelBuilder.Entity<UnSentCommand>()
                        .ToTable("msgs_UnSentCommands");

            modelBuilder.Entity<UnPublishedEvent>()
                        .ToTable("msgs_UnPublishedEvents");
        }
    }
}