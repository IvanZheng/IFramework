using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;

namespace IFramework.EntityFramework.MessageStoring
{
    public class MessageStore : DbContext, IMessageStore
    {
        public DbSet<Command> Commands { get; set; }
        public DbSet<DomainEvent> DomainEvents { get; set; }

        public DbSet<HandledEvent> HandledEvents { get; set; }

        public DbSet<Command> UnSentCommands { get; set; }
        public DbSet<DomainEvent> UnPublishedEvents { get; set; }

        public MessageStore()
            : base("MessageStore")
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Message>()
                .Map<Command>(map =>
                {
                    map.ToTable("Commands");
                    map.MapInheritedProperties();
                })
                .Map<DomainEvent>(map =>
                {
                    map.ToTable("DomainEvents");
                    map.MapInheritedProperties();
                });
        }

        public void SaveCommand(IMessageContext commandContext, IEnumerable<IMessageContext> domainEventContexts)
        {
            var command = new Command(commandContext);
            Commands.Add(command);
            domainEventContexts.ForEach(domainEventContext =>
            {
                domainEventContext.CorrelationID = commandContext.MessageID;
                DomainEvents.Add(new DomainEvent(domainEventContext));
                UnPublishedEvents.Add(new DomainEvent(domainEventContext));
            });
            SaveChanges();
        }

        public void SaveDomainEvent(IMessageContext domainEventContext, IEnumerable<IMessageContext> commandContexts)
        {
            var domainEvent = DomainEvents.Find(domainEventContext.MessageID);
            if (domainEvent == null)
            {
                domainEvent = new DomainEvent(domainEventContext);
                DomainEvents.Add(domainEvent);
            }
            HandledEvents.Add(new HandledEvent(domainEvent.ID));
            commandContexts.ForEach(commandContext =>
            {
                commandContext.CorrelationID = domainEventContext.MessageID;
                UnSentCommands.Add(new Command(commandContext));
            });
            SaveChanges();
        }


    }
}
