using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;

namespace IFramework.EntityFramework
{
    public class MessageStore : DbContext, IMessageStore
    {

        public DbSet<Command> Commands { get; set; }
        public DbSet<DomainEvent> DomainEvents { get; set; }

        public MessageStore()
            : base("MessageStore")
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Message>()
                .Map<Command>(map => {
                    map.ToTable("Commands");
                    map.MapInheritedProperties();
                })
                .Map<DomainEvent>(map => {
                    map.ToTable("DomainEvents");
                    map.MapInheritedProperties();
                });
        }


        public void Save(IMessageContext commandContext, string domainEventID)
        {
            var command = new Command(commandContext, domainEventID);
            Commands.Add(command);
            SaveChanges();
        }

        public void Save(IMessageContext commandContext, IEnumerable<IMessageContext> domainEventContexts)
        {
            var command = Commands.Find(commandContext.MessageID);
            if (command == null)
            {
                command = new Command(commandContext);
                Commands.Add(command);
            }
            domainEventContexts.ForEach(domainEventContext => {
                var domainEvent = new DomainEvent(domainEventContext, command.ID);
                DomainEvents.Add(domainEvent);
            });
            SaveChanges();
        }
    }
}
