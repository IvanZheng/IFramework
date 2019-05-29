using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.MessageStores.Abstracts;
using Microsoft.EntityFrameworkCore;

namespace IFramework.MessageStores.Relational
{
    public abstract class MessageStore : Abstracts.MessageStore
    {
        protected MessageStore(DbContextOptions options) : base(options) { }
        public DbSet<HandledEvent> HandledEvents { get; set; }
        public DbSet<FailHandledEvent> FailHandledEvents { get; set; }

        public override Task HandleEventAsync(IMessageContext eventContext,
                                              string subscriptionName,
                                              IEnumerable<IMessageContext> commandContexts,
                                              IEnumerable<IMessageContext> messageContexts)
        {
            HandledEvents.Add(new HandledEvent(eventContext.MessageId, subscriptionName, eventContext.MessageOffset, DateTime.Now));
            commandContexts.ForEach(commandContext =>
            {
                commandContext.CorrelationId = eventContext.MessageId;
                // don't save command here like event that would be published to other bounded context
                UnSentCommands.Add(new UnSentCommand(commandContext));
            });
            messageContexts.ForEach(messageContext =>
            {
                messageContext.CorrelationId = eventContext.MessageId;
                Events.Add(BuildEvent(messageContext));
                UnPublishedEvents.Add(new UnPublishedEvent(messageContext));
            });
            return SaveChangesAsync();
        }

        public override Task<bool> HasEventHandledAsync(string eventId, string subscriptionName)
        {
            return HandledEvents.AnyAsync(@event => @event.Id == eventId && @event.SubscriptionName == subscriptionName);
        }

        public override Task SaveFailHandledEventAsync(IMessageContext eventContext,
                                                       string subscriptionName,
                                                       Exception e,
                                                       params IMessageContext[] messageContexts)
        {
            HandledEvents.Add(new FailHandledEvent(eventContext.MessageId, subscriptionName, eventContext.MessageOffset, DateTime.Now, e));

            messageContexts.ForEach(messageContext =>
            {
                messageContext.CorrelationId = eventContext.MessageId;
                Events.Add(BuildEvent(messageContext));
                UnPublishedEvents.Add(new UnPublishedEvent(messageContext));
            });
            return SaveChangesAsync();
        }

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