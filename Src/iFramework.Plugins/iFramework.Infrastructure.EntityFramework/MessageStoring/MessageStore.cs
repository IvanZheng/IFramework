using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.MessageQueue.MessageFormat;
using IFramework.Infrastructure.Logging;

namespace IFramework.EntityFramework.MessageStoring
{
    public class MessageStore : MSDbContext, IMessageStore
    {
        protected readonly ILogger _logger;

        public DbSet<Command> Commands { get; set; }
        public DbSet<Event> Events { get; set; }

        public DbSet<HandledEvent> HandledEvents { get; set; }

        public DbSet<UnSentCommand> UnSentCommands { get; set; }
        public DbSet<UnPublishedEvent> UnPublishedEvents { get; set; }

        public MessageStore(string connectionString = null)
            : base(connectionString ?? "MessageStore")
        {
            _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<HandledEvent>().HasKey(e => new {e.Id, e.SubscriptionName});

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

        protected virtual Command BuildCommand(IMessageContext commandContext)
        {
            return new Command(commandContext);
        }

        protected virtual Event BuildEvent(IMessageContext eventContext)
        {
            return new Event(eventContext);
        }

        public void SaveCommand(IMessageContext commandContext, IEnumerable<IMessageContext> eventContexts)
        {
            var command = BuildCommand(commandContext);
            Commands.Add(command);
            eventContexts.ForEach(eventContext =>
            {
                eventContext.CorrelationID = commandContext.MessageID;
                Events.Add(BuildEvent(eventContext));
                UnPublishedEvents.Add(new UnPublishedEvent(eventContext));
            });
            SaveChanges();
        }

        public void SaveFailedCommand(IMessageContext commandContext)
        {
            var command = BuildCommand(commandContext);
            command.Status = Status.Failed;
            Commands.Add(command);
            SaveChanges();
        }

        public void SaveEvent(IMessageContext eventContext, string subscriptionName, IEnumerable<IMessageContext> commandContexts)
        {
            var @event = Events.Find(eventContext.MessageID);
            if (@event == null)
            {
                @event = BuildEvent(eventContext);
                Events.Add(@event);
            }
            HandledEvents.Add(new HandledEvent(@event.ID, subscriptionName));
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

        public bool HasEventHandled(string eventId, string subscriptionName)
        {
            return HandledEvents.Count(@event => @event.Id == eventId 
                                    && @event.SubscriptionName == subscriptionName) > 0;
        }


        public void RemoveSentCommand(string commandId)
        {
            var deleteSql = string.Format("delete from UnSentCommands where ID = '{0}'", commandId);
            this.Database.ExecuteSqlCommand(deleteSql);
        }

        public void RemovePublishedEvent(string eventId)
        {
            var deleteSql = string.Format("delete from UnPublishedEvents where ID = '{0}'", eventId);
            this.Database.ExecuteSqlCommand(deleteSql);
        }


        public IEnumerable<IMessageContext> GetAllUnSentCommands()
        {
            return GetAllUnSentMessages<UnSentCommand>();
        }

        public IEnumerable<IMessageContext> GetAllUnPublishedEvents()
        {
            return GetAllUnSentMessages<UnPublishedEvent>();
        }

        IEnumerable<IMessageContext> GetAllUnSentMessages<TMessage>()
            where TMessage : UnSentMessage
        {
            var messageContexts = new List<IMessageContext>();
            this.Set<TMessage>().ToList().ForEach(message =>
            {
                try
                {
                    var rawMessage = message.MessageBody.ToJsonObject(Type.GetType(message.Type)) as IMessage;
                    if (rawMessage != null)
                    {
                        var messageContext = new MessageContext(rawMessage);
                        messageContexts.Add(messageContext);
                    }
                    else
                    {
                        this.Set<TMessage>().Remove(message);
                        _logger.ErrorFormat("get unsent message error: {0}", message.ToJson());
                    }
                }
                catch (Exception)
                {
                    this.Set<TMessage>().Remove(message);
                    _logger.ErrorFormat("get unsent message error: {0}", message.ToJson());
                }
            });
            SaveChanges();
            return messageContexts;
        }
    }
}
