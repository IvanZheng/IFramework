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

        public DbSet<UnSentCommand> UnSentCommands { get; set; }
        public DbSet<UnPublishedEvent> UnPublishedEvents { get; set; }

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
                    map.ToTable("Events");
                    map.MapInheritedProperties();
                });

            modelBuilder.Entity<UnSentMessage>()
                .Map<UnSentCommand>(map =>
                {
                    map.ToTable("UnSentCommands");
                    map.MapInheritedProperties();
                })
                .Map<UnPublishedEvent>(map =>
                {
                    map.ToTable("UnPublishedEvents");
                    map.MapInheritedProperties();
                });
        }

        public void SaveCommand(IMessageContext commandContext, IEnumerable<IMessageContext> eventContexts)
        {
            var command = new Command(commandContext);
            Commands.Add(command);
            eventContexts.ForEach(eventContext =>
            {
                eventContext.CorrelationID = commandContext.MessageID;
                Events.Add(new Event(eventContext));
                UnPublishedEvents.Add(new UnPublishedEvent(eventContext));
            });
            SaveChanges();
        }

        public void SaveFailedCommand(IMessageContext commandContext)
        {
            var command = new Command(commandContext)
            {
                Status = Status.Failed
            };
            Commands.Add(command);
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
                UnSentCommands.Add(new UnSentCommand(commandContext));
            });
            SaveChanges();
        }



        public bool HasCommandHandled(string commandId)
        {
            return Commands.Count(command => command.ID == commandId) > 0;
        }

        public bool HasEventHandled(string eventId)
        {
            return HandledEvents.Count(@event => @event.ID == eventId) > 0;
        }

        //public void SaveUnSentCommands(IEnumerable<IMessageContext> commandContexts)
        //{
        //    commandContexts.ForEach(commandContext => 
        //        UnSentCommands.Add(new UnSentCommand(commandContext)));
        //    SaveChanges();
        //}


        public void RemoveSentCommand(string commandID)
        {
            var deleteSql = string.Format("delete from UnSentCommands where ID = '{0}'", commandID);
            this.Database.ExecuteSqlCommand(deleteSql);
            //var unSentCommand = UnSentCommands.Find(commandID);
            //if (unSentCommand != null)
            //{
            //    UnSentCommands.Remove(unSentCommand);
            //    SaveChanges();
            //}
        }

        public void RemovePublishedEvent(string eventID)
        {
            var deleteSql = string.Format("delete from UnPublishedEvents where ID = '{0}'", eventID);
            this.Database.ExecuteSqlCommand(deleteSql);
            //var unPublishedEvent = UnPublishedEvents.Find(eventID);
            //if (unPublishedEvent != null)
            //{
            //    UnPublishedEvents.Remove(unPublishedEvent);
            //    SaveChanges();
            //}
        }


        public IEnumerable<IMessageContext> GetAllUnSentCommands()
        {
            var commandContexts = new List<IMessageContext>();
            UnSentCommands.ToList().ForEach(command =>
                commandContexts.Add((IMessageContext)command.MessageBody.ToJsonObject(Type.GetType(command.Type))));
            return commandContexts;
        }

        public IEnumerable<IMessageContext> GetAllUnPublishedEvents()
        {
            var eventContexts = new List<IMessageContext>();
            UnPublishedEvents.ToList().ForEach(@event =>
                eventContexts.Add((IMessageContext)@event.MessageBody.ToJsonObject(Type.GetType(@event.Type))));
            return eventContexts;
        }
    }
}
