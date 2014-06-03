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
        public DbSet<Event> Events { get; set; }

        public DbSet<HandledEvent> HandledEvents { get; set; }

        public DbSet<Command> UnSentCommands { get; set; }
        public DbSet<Event> UnPublishedEvents { get; set; }

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
                .Map<Event>(map =>
                {
                    map.ToTable("DomainEvents");
                    map.MapInheritedProperties();
                });
        }

        public void SaveCommand(IMessageContext commandContext, IEnumerable<IMessageContext> eventContexts)
        {
            var command = new Command(commandContext);
            Commands.Add(command);
            eventContexts.ForEach(domainEventContext =>
            {
                domainEventContext.CorrelationID = commandContext.MessageID;
                Events.Add(new Event(domainEventContext));
                UnPublishedEvents.Add(new Event(domainEventContext));
            });
            SaveChanges();
        }

        public void SaveEvent(IMessageContext eventContext, IEnumerable<IMessageContext> commandContexts)
        {
            var @event = Events.Find(eventContext.MessageID);
            if (@event == null)
            {
                @event = new Event(eventContext);
                Events.Add(@event);
            }
            HandledEvents.Add(new HandledEvent(@event.ID));
            commandContexts.ForEach(commandContext =>
            {
                commandContext.CorrelationID = eventContext.MessageID;
                UnSentCommands.Add(new Command(commandContext));
            });
            SaveChanges();
        }



        public bool HasCommandHandled(string commandID)
        {
            return Commands.Count(command => command.ID == commandID) > 0;
        }

        public bool HasEventHandled(string eventID)
        {
            return HandledEvents.Count(@event => @event.ID == eventID) > 0;
        }
    }
}
