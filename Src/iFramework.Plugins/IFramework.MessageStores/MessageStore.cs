using System;
using System.Collections.Generic;
using System.Linq;
using IFramework.DependencyInjection;
using IFramework.EntityFrameworkCore;
using IFramework.Exceptions;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IFramework.MessageStores.Sqlserver
{
    public abstract class MessageStore : MsDbContext, IMessageStore
    {
        protected readonly ILogger Logger;

        protected MessageStore(DbContextOptions options)
            : base(options)
        {
            Logger = IoCFactory.GetService<ILoggerFactory>().CreateLogger(GetType());
        }

        public DbSet<Command> Commands { get; set; }

        public DbSet<HandledEvent> HandledEvents { get; set; }
        public DbSet<FailHandledEvent> FailHandledEvents { get; set; }
        public DbSet<UnSentCommand> UnSentCommands { get; set; }
        public DbSet<UnPublishedEvent> UnPublishedEvents { get; set; }

        public void SaveCommand(IMessageContext commandContext,
                                object result = null,
                                params IMessageContext[] messageContexts)
        {
            if (commandContext != null)
            {
                var command = BuildCommand(commandContext, result);
                Commands.Add(command);
            }
            messageContexts?.ForEach(eventContext =>
            {
                eventContext.CorrelationId = commandContext?.MessageId;
                //Events.Add(BuildEvent(eventContext));
                UnPublishedEvents.Add(new UnPublishedEvent(eventContext));
            });
            SaveChanges();
        }

        public void SaveFailedCommand(IMessageContext commandContext,
                                      Exception ex = null,
                                      params IMessageContext[] eventContexts)
        {
            if (commandContext != null)
            {
                var command = BuildCommand(commandContext, ex);
                command.Status = MessageStatus.Failed;
                Commands.Add(command);
            }
            eventContexts?.ForEach(eventContext =>
            {
                eventContext.CorrelationId = commandContext?.MessageId;
                //Events.Add(BuildEvent(eventContext));
                UnPublishedEvents.Add(new UnPublishedEvent(eventContext));
            });
            SaveChanges();
        }


        //internal Event InternalSaveEvent(IMessageContext eventContext)
        //{
        //    // lock (EventLock)
        //    {
        //        var retryTimes = 5;
        //        while (true)
        //        {
        //            try
        //            {
        //                var @event = Events.Find(eventContext.MessageId);
        //                if (@event == null)
        //                {
        //                    @event = BuildEvent(eventContext);
        //                    Events.Add(@event);
        //                    SaveChanges();
        //                }
        //                return @event;
        //            }
        //            catch (Exception)
        //            {
        //                if (--retryTimes > 0)
        //                {
        //                    Task.Delay(50).Wait();
        //                }
        //                else
        //                {
        //                    throw;
        //                }
        //            }
        //        }

        //    }
        //}


        //public void SaveEvent(IMessageContext eventContext)
        //{
        //    InternalSaveEvent(eventContext);
        //}

        // if not subscribe the same event message by topic's mulitple subscriptions
        // we don't need EventLock to assure Events.Add(@event) having no conflict.
        public void HandleEvent(IMessageContext eventContext,
                                string subscriptionName,
                                IEnumerable<IMessageContext> commandContexts,
                                IEnumerable<IMessageContext> messageContexts)
        {
            HandledEvents.Add(new HandledEvent(eventContext.MessageId, subscriptionName, DateTime.Now));
            commandContexts.ForEach(commandContext =>
            {
                commandContext.CorrelationId = eventContext.MessageId;
                // don't save command here like event that would be published to other bounded context
                UnSentCommands.Add(new UnSentCommand(commandContext));
            });
            messageContexts.ForEach(messageContext =>
            {
                messageContext.CorrelationId = eventContext.MessageId;
                //Events.Add(BuildEvent(messageContext));
                UnPublishedEvents.Add(new UnPublishedEvent(messageContext));
            });
            SaveChanges();
        }

        public void SaveFailHandledEvent(IMessageContext eventContext,
                                         string subscriptionName,
                                         Exception e,
                                         params IMessageContext[] messageContexts)
        {
            HandledEvents.Add(new FailHandledEvent(eventContext.MessageId, subscriptionName, DateTime.Now, e));

            messageContexts.ForEach(messageContext =>
            {
                messageContext.CorrelationId = eventContext.MessageId;
                UnPublishedEvents.Add(new UnPublishedEvent(messageContext));
            });
            SaveChanges();
        }

        public CommandHandledInfo GetCommandHandledInfo(string commandId)
        {
            CommandHandledInfo commandHandledInfo = null;
            var command = Commands.FirstOrDefault(c => c.Id == commandId);
            if (command != null)
            {
                commandHandledInfo = new CommandHandledInfo
                {
                    Result = command.Reply,
                    Id = command.Id
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
            //var deleteSql = string.Format("delete from msgs_UnSentCommands where Id = '{0}'", commandId);
            //Database.ExecuteSqlCommand(deleteSql);
            var unSentCommand = UnSentCommands.Find(commandId);
            if (unSentCommand != null)
            {
                UnSentCommands.Remove(unSentCommand);
                SaveChanges();
            }
        }

        public void RemovePublishedEvent(string eventId)
        {
            //var deleteSql = string.Format("delete from msgs_UnPublishedEvents where Id = '{0}'", eventId);
            //Database.ExecuteSqlCommand(deleteSql);
            var publishedEvent = UnPublishedEvents.Find(eventId);
            if (publishedEvent != null)
            {
                UnPublishedEvents.Remove(publishedEvent);
                SaveChanges();
            }
        }


        public IEnumerable<IMessageContext> GetAllUnSentCommands(
            Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage)
        {
            return GetAllUnSentMessages<UnSentCommand>(wrapMessage);
        }

        public IEnumerable<IMessageContext> GetAllUnPublishedEvents(
            Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage)
        {
            return GetAllUnSentMessages<UnPublishedEvent>(wrapMessage);
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Command>()
                        .OwnsOne(m => m.SagaInfo);

            modelBuilder.Entity<Command>()
                        .Property(c => c.MessageBody)
                        .HasColumnType("ntext");

            modelBuilder.Entity<UnSentCommand>()
                        .Property(c => c.MessageBody)
                        .HasColumnType("ntext");
            modelBuilder.Entity<UnPublishedEvent>()
                        .Property(c => c.MessageBody)
                        .HasColumnType("ntext");

            modelBuilder.Entity<HandledEvent>()
                        .HasKey(e => new {e.Id, e.SubscriptionName});

            modelBuilder.Entity<HandledEvent>()
                        .Property(handledEvent => handledEvent.SubscriptionName)
                        .HasMaxLength(322);


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
                        .Property(c => c.CorrelationId)
                        .HasMaxLength(200);
            //.HasColumnAnnotation(IndexAnnotation.AnnotationName,
            //    new IndexAnnotation(new IndexAttribute("CorrelationIdIndex")));
            modelBuilder.Entity<Command>()
                        .Property(c => c.Name)
                        .HasMaxLength(200);
            //.HasColumnAnnotation(IndexAnnotation.AnnotationName,
            //    new IndexAnnotation(new IndexAttribute("NameIndex")));
            modelBuilder.Entity<Command>()
                        .Property(c => c.Topic)
                        .HasMaxLength(200);
            //.HasColumnAnnotation(IndexAnnotation.AnnotationName,
            //    new IndexAnnotation(new IndexAttribute("TopicIndex")));

            //modelBuilder.Entity<Event>()
            //            .ToTable("msgs_Events")
            //            .Property(e => e.CorrelationId)
            //            .HasMaxLength(200);
            //.HasColumnAnnotation(IndexAnnotation.AnnotationName,
            //    new IndexAnnotation(new IndexAttribute("CorrelationIdIndex")));
            //modelBuilder.Entity<Event>()
            //            .Property(e => e.Name)
            //            .HasMaxLength(200);
            //.HasColumnAnnotation(IndexAnnotation.AnnotationName,
            //    new IndexAnnotation(new IndexAttribute("NameIndex")));
            //modelBuilder.Entity<Event>()
            //            .Property(e => e.AggregateRootId)
            //            .HasMaxLength(200);
            //.HasColumnAnnotation(IndexAnnotation.AnnotationName,
            //    new IndexAnnotation(new IndexAttribute("AGRootIdIndex")));
            //modelBuilder.Entity<Event>()
            //            .Property(e => e.Topic)
            //            .HasMaxLength(200);
            //.HasColumnAnnotation(IndexAnnotation.AnnotationName,
            //    new IndexAnnotation(new IndexAttribute("TopicIndex")));

            modelBuilder.Entity<UnSentCommand>()
                        .OwnsOne(m => m.SagaInfo);
            modelBuilder.Entity<UnSentCommand>()
                        .ToTable("msgs_UnSentCommands");

            modelBuilder.Entity<UnPublishedEvent>()
                        .OwnsOne(m => m.SagaInfo);
            modelBuilder.Entity<UnPublishedEvent>()
                        .ToTable("msgs_UnPublishedEvents");
        }

        protected virtual Command BuildCommand(IMessageContext commandContext, object result)
        {
            return new Command(commandContext, result);
        }

        //protected virtual Event BuildEvent(IMessageContext eventContext)
        //{
        //    return new Event(eventContext);
        //}

        private IEnumerable<IMessageContext> GetAllUnSentMessages<TMessage>(
            Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage)
            where TMessage : UnSentMessage
        {
            var messageContexts = new List<IMessageContext>();
            Set<TMessage>()
                .ToList()
                .ForEach(message =>
                {
                    try
                    {
                        if (message.MessageBody.ToJsonObject(Type.GetType(message.Type)) is IMessage rawMessage)
                        {
                            messageContexts.Add(wrapMessage(message.Id, rawMessage, message.Topic, message.CorrelationId,
                                                            message.ReplyToEndPoint, message.SagaInfo, message.Producer));
                        }
                        else
                        {
                            Set<TMessage>().Remove(message);
                            Logger.LogError("get unsent message error: {0}", message.ToJson());
                        }
                    }
                    catch (Exception)
                    {
                        Set<TMessage>().Remove(message);
                        Logger.LogError("get unsent message error: {0}", message.ToJson());
                    }
                });
            SaveChanges();
            return messageContexts;
        }
    }
}