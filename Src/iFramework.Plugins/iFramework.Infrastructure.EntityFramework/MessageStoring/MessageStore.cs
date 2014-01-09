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
            try
            {
                var command = new Command(commandContext, domainEventID);
                Commands.Add(command);
                SaveChanges();
            }
            catch(Exception)
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
                    command = new Command(commandContext);
                    Commands.Add(command);
                }
            }
            catch(Exception)
            {
                // command may be readd!
            }
            
            domainEventContexts.ForEach(domainEventContext => {
                var domainEvent = new DomainEvent(domainEventContext, commandContext.MessageID);
                DomainEvents.Add(domainEvent);
            });
            SaveChanges();
        }
    }
}
