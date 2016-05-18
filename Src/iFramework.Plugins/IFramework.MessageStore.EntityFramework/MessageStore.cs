using IFramework.Message;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Event;
using IFramework.EntityFramework;
using Microsoft.Practices.Unity;

namespace IFramework.MessageStoring
{
    public class MessageStore : MSDbContext, IMessageStore
    {
        protected readonly ILogger _logger;

        public DbSet<Command> Commands { get; set; }
        public DbSet<Event> Events { get; set; }

        public DbSet<HandledEvent> HandledEvents { get; set; }
        public DbSet<FailHandledEvent> FailHandledEvents { get; set; }
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
            modelBuilder.Entity<HandledEvent>().HasKey(e => new { e.Id, e.SubscriptionName });

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

        protected virtual Command BuildCommand(IMessageContext commandContext, Exception ex = null)
        {
            return new Command(commandContext, ex);
        }

        protected virtual Event BuildEvent(IMessageContext eventContext)
        {
            return new Event(eventContext);
        }

        public void SaveCommand(IMessageContext commandContext, params IMessageContext[] messageContexts)
        {
            if (commandContext != null)
            {
                var command = BuildCommand(commandContext);
                Commands.Add(command);
            }
            messageContexts.ForEach(eventContext =>
            {
                eventContext.CorrelationID = commandContext.MessageID;
                Events.Add(BuildEvent(eventContext));
                UnPublishedEvents.Add(new UnPublishedEvent(eventContext));
            });
            SaveChanges();
        }

        public void SaveFailedCommand(IMessageContext commandContext, Exception ex = null, params IMessageContext[] eventContexts)
        {
            if (commandContext != null)
            {
                var command = BuildCommand(commandContext, ex);
                command.Status = MessageStatus.Failed;
                Commands.Add(command);

                if (eventContexts != null)
                {
                    eventContexts.ForEach(eventContext =>
                    {
                        eventContext.CorrelationID = commandContext.MessageID;
                        Events.Add(BuildEvent(eventContext));
                        UnPublishedEvents.Add(new UnPublishedEvent(eventContext));
                    });

                }
                SaveChanges();
            }
        }

        // if not subscribe the sampe event message by topic's mulitple subscriptions
        // we don't need EventLock to assure Events.Add(@event) having no conflict.
        //static object EventLock = new object();
        public void SaveEvent(IMessageContext eventContext, string subscriptionName,
                              IEnumerable<IMessageContext> commandContexts,
                              IEnumerable<IMessageContext> messageContexts)
        {
            //lock (EventLock)
            {
                var @event = Events.Find(eventContext.MessageID);
                if (@event == null)
                {
                    @event = BuildEvent(eventContext);
                    Events.Add(@event);
                }
                HandledEvents.Add(new HandledEvent(@event.ID, subscriptionName, DateTime.Now));
                commandContexts.ForEach(commandContext =>
                {
                    commandContext.CorrelationID = eventContext.MessageID;
                    // don't save command here like event that would be published to other bounded context
                    UnSentCommands.Add(new UnSentCommand(commandContext));
                });
                messageContexts.ForEach(messageContext =>
                {
                    messageContext.CorrelationID = eventContext.MessageID;
                    Events.Add(BuildEvent(messageContext));
                    UnPublishedEvents.Add(new UnPublishedEvent(messageContext));
                });
                SaveChanges();
            }
        }

        public void SaveFailHandledEvent(IMessageContext eventContext, string subscriptionName, Exception e, params IMessageContext[] messageContexts)
        {
            //lock (EventLock)
            {
                var @event = Events.Find(eventContext.MessageID);
                if (@event == null)
                {
                    @event = BuildEvent(eventContext);
                    Events.Add(@event);
                }
                HandledEvents.Add(new FailHandledEvent(@event.ID, subscriptionName, DateTime.Now, e));


                messageContexts.ForEach(messageContext =>
                {
                    messageContext.CorrelationID = eventContext.MessageID;
                    Events.Add(BuildEvent(messageContext));
                    UnPublishedEvents.Add(new UnPublishedEvent(messageContext));
                });
                SaveChanges();
            }
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


        public IEnumerable<IMessageContext> GetAllUnSentCommands(Func<string, object, string, string, IMessageContext> wrapMessage)
        {
            return GetAllUnSentMessages<UnSentCommand>(wrapMessage);
        }

        public IEnumerable<IMessageContext> GetAllUnPublishedEvents(Func<string, object, string, string, IMessageContext> wrapMessage)
        {
            return GetAllUnSentMessages<UnPublishedEvent>(wrapMessage);
        }

        IEnumerable<IMessageContext> GetAllUnSentMessages<TMessage>(Func<string, object, string, string, IMessageContext> wrapMessage)
            where TMessage : UnSentMessage
        {
            var messageContexts = new List<IMessageContext>();
            this.Set<TMessage>().ToList().ForEach(message =>
            {
                try
                {
                    var rawMessage = message.MessageBody.ToJsonObject(Type.GetType(message.Type));
                    if (rawMessage != null)
                    {
                        messageContexts.Add(wrapMessage(message.ID, rawMessage, message.Topic, message.CorrelationID));
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
