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
using IFramework.IoC;
using IFramework.Message.Impl;
using System.Data.Entity.Infrastructure.Annotations;
using System.ComponentModel.DataAnnotations.Schema;
using IFramework.Exceptions;

namespace IFramework.MessageStoring
{
    public abstract class MessageStore : MSDbContext, IMessageStore
    {
        protected readonly ILogger _logger;

        public DbSet<Command> Commands { get; set; }
        public DbSet<Event> Events { get; set; }

        public DbSet<HandledEvent> HandledEvents { get; set; }
        public DbSet<FailHandledEvent> FailHandledEvents { get; set; }
        public DbSet<UnSentCommand> UnSentCommands { get; set; }
        public DbSet<UnPublishedEvent> UnPublishedEvents { get; set; }

        static object EventLock = new object();

        public MessageStore(string connectionString = null)
            : base(connectionString ?? "MessageStore")
        {
            if (IoCFactory.IsInit())
            {
                _logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType());
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<HandledEvent>().HasKey(e => new { e.Id, e.SubscriptionName });

            //modelBuilder.Entity<Message>()
            //    .Map<Command>(map =>
            //    {
            //        map.ToTable("Commands");
            //        map.MapInheritedProperties();
            //    })
            //    .Map<Event>(map =>
            //    {
            //        map.ToTable("Events");
            //        map.MapInheritedProperties();
            //    });


            modelBuilder.Entity<HandledEvent>()
                        .ToTable("msgs_HandledEvents");

            modelBuilder.Entity<Command>()
                        .Ignore(c => c.Reply)
                        .ToTable("msgs_Commands")
                        .Property(c => c.CorrelationID)
                        .HasMaxLength(200)
                        .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("CorrelationIdIndex")));
            modelBuilder.Entity<Command>()
                        .Property(c => c.Name)
                        .HasMaxLength(200)
                        .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("NameIndex")));
            modelBuilder.Entity<Command>()
                       .Property(c => c.Topic)
                       .HasMaxLength(200)
                       .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("TopicIndex")));


            modelBuilder.Entity<Event>()
                        .ToTable("msgs_Events")
                        .Property(e => e.CorrelationID)
                        .HasMaxLength(200)
                        .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("CorrelationIdIndex")));
            modelBuilder.Entity<Event>()
                        .Property(e => e.Name)
                        .HasMaxLength(200)
                        .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("NameIndex")));
            modelBuilder.Entity<Event>()
                        .Property(e => e.AggregateRootID)
                        .HasMaxLength(200)
                        .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("AGRootIdIndex")));
            modelBuilder.Entity<Event>()
                        .Property(e => e.Topic)
                        .HasMaxLength(200)
                        .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("TopicIndex")));

            modelBuilder.Entity<UnSentMessage>()
                .Map<UnSentCommand>(map =>
                {
                    map.ToTable("msgs_UnSentCommands");
                    map.MapInheritedProperties();
                })
                .Map<UnPublishedEvent>(map =>
                {
                    map.ToTable("msgs_UnPublishedEvents");
                    map.MapInheritedProperties();
                });
        }

        protected virtual Command BuildCommand(IMessageContext commandContext, object result)
        {
            return new Command(commandContext, result);
        }

        protected virtual Event BuildEvent(IMessageContext eventContext)
        {
            return new Event(eventContext);
        }

        public void SaveCommand(IMessageContext commandContext, object result = null, params IMessageContext[] messageContexts)
        {
            if (commandContext != null)
            {
                var command = BuildCommand(commandContext, result);
                Commands.Add(command);
            }
            messageContexts.ForEach(eventContext =>
            {
                eventContext.CorrelationID = commandContext?.MessageID;
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
                command.Status = ex is DomainException ? MessageStatus.Failed : MessageStatus.UnknownFailed;
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


        public void SaveEvent(IMessageContext eventContext)
        {
            lock (EventLock)
            {
                try
                {
                    var @event = Events.Find(eventContext.MessageID);
                    if (@event == null)
                    {
                        @event = BuildEvent(eventContext);
                        Events.Add(@event);
                        SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    // only multiple subscribers consuming the same topic message will cause the Event Saving conflict.
                    _logger?.Error($"SaveEvent failed. Event: {eventContext.Message.ToJson()}", ex);
                }
            }
        }

        // if not subscribe the same event message by topic's mulitple subscriptions
        // we don't need EventLock to assure Events.Add(@event) having no conflict.
        public void HandleEvent(IMessageContext eventContext, string subscriptionName,
                              IEnumerable<IMessageContext> commandContexts,
                              IEnumerable<IMessageContext> messageContexts)
        {
            HandledEvents.Add(new HandledEvent(eventContext.MessageID, subscriptionName, DateTime.Now));
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

        public CommandHandledInfo GetCommandHandledInfo(string commandId)
        {
            CommandHandledInfo commandHandledInfo = null;
            var command = Commands.FirstOrDefault(c => c.ID == commandId);
            if (command != null)
            {
                commandHandledInfo = new CommandHandledInfo
                {
                    Result = command.Reply,
                    Id = command.ID
                };
            }
            return commandHandledInfo;
        }

        public bool HasEventHandled(string eventId, string subscriptionName)
        {
            return HandledEvents.Count(@event => @event.Id == eventId
                                    && @event.SubscriptionName == subscriptionName) > 0;
        }


        public void RemoveSentCommand(string commandId)
        {
            var deleteSql = string.Format("delete from msgs_UnSentCommands where ID = '{0}'", commandId);
            this.Database.ExecuteSqlCommand(deleteSql);
        }

        public void RemovePublishedEvent(string eventId)
        {
            var deleteSql = string.Format("delete from msgs_UnPublishedEvents where ID = '{0}'", eventId);
            this.Database.ExecuteSqlCommand(deleteSql);
        }


        public IEnumerable<IMessageContext> GetAllUnSentCommands(Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage)
        {
            return GetAllUnSentMessages<UnSentCommand>(wrapMessage);
        }

        public IEnumerable<IMessageContext> GetAllUnPublishedEvents(Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage)
        {
            return GetAllUnSentMessages<UnPublishedEvent>(wrapMessage);
        }

        IEnumerable<IMessageContext> GetAllUnSentMessages<TMessage>(Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage)
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
                        messageContexts.Add(wrapMessage(message.ID, rawMessage, message.Topic, message.CorrelationID, message.ReplyToEndPoint, message.SagaInfo, message.Producer));
                    }
                    else
                    {
                        this.Set<TMessage>().Remove(message);
                        _logger?.ErrorFormat("get unsent message error: {0}", message.ToJson());
                    }
                }
                catch (Exception)
                {
                    this.Set<TMessage>().Remove(message);
                    _logger?.ErrorFormat("get unsent message error: {0}", message.ToJson());
                }
            });
            SaveChanges();
            return messageContexts;
        }
    }
}
