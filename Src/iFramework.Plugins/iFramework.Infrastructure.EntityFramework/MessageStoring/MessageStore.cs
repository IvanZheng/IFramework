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

        public virtual Command BuildCommand(IMessageContext commandContext, string domainEventID = null)
        {
            return new Command(commandContext, domainEventID);
        }


        public virtual DomainEvent BuildDomainEvent(IMessageContext domainEventContext, string commanadID)
        {
            return new DomainEvent(domainEventContext, commanadID);
        }

        public void Save(IMessageContext commandContext, string domainEventID)
        {
            try
            {
                var command = Commands.Find(commandContext.MessageID);
                if (command == null)
                {
                    command = BuildCommand(commandContext, domainEventID);
                    Commands.Add(command);
                    SaveChanges();
                }
            }
            catch (Exception)
            {

            }
        }

        public void Save(IMessageContext commandContext, IEnumerable<IMessageContext> domainEventContexts)
        {
            try
            {
                var command = Commands.Find(commandContext.MessageID);
                if (command == null)
                {
                    command = BuildCommand(commandContext);
                    Commands.Add(command);
                    SaveChanges();
                }
            }
            catch (Exception)
            {
                // command may be readd!
            }

            domainEventContexts.ForEach(domainEventContext =>
            {
                var domainEvent = BuildDomainEvent(domainEventContext, commandContext.MessageID);
                DomainEvents.Add(domainEvent);
            });
            SaveChanges();
        }
    }
}
