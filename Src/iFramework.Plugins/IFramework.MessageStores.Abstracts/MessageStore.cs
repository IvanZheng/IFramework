﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFramework.DependencyInjection;
using IFramework.EntityFrameworkCore;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageQueue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFramework.MessageStores.Abstracts
{
    public abstract class MessageStore : MsDbContext, IMessageStore
    {
        protected readonly ILogger Logger;
        protected readonly MessageQueueOptions Options;
        protected MessageStore(DbContextOptions options)
            : base(options)
        {
            Options = ObjectProviderFactory.GetService<MessageQueueOptions>();
            Logger = ObjectProviderFactory.GetService<ILoggerFactory>().CreateLogger(GetType());
#pragma warning disable EF1001 // Internal EF Core API usage.
            InMemoryStore = options.FindExtension<InMemoryOptionsExtension>() != null;
#pragma warning restore EF1001 // Internal EF Core API usage.
        }

        public DbSet<Command> Commands { get; set; }
        public DbSet<Event> Events { get; set; }

        public DbSet<UnSentCommand> UnSentCommands { get; set; }
        public DbSet<UnPublishedEvent> UnPublishedEvents { get; set; }
        public bool InMemoryStore { get; }

        public Task SaveCommandAsync(IMessageContext commandContext,
                                     object result = null,
                                     params IMessageContext[] messageContexts)
        {
            var needSaveChanges = false;
            if (commandContext != null && Options.EnsureIdempotent)
            {
                var command = BuildCommand(commandContext, result);
                Commands.Add(command);
                needSaveChanges = true;
            }

            
            messageContexts?.ForEach(eventContext =>
            {
                eventContext.CorrelationId = commandContext?.MessageId;
                if (Options.PersistEvent)
                {
                    Events.Add(BuildEvent(eventContext));
                    needSaveChanges = true;
                }

                if (Options.EnsureArrival)
                {
                    UnPublishedEvents.Add(new UnPublishedEvent(eventContext));
                    needSaveChanges = true;
                }
            });

            
            return needSaveChanges ? SaveChangesAsync() : Task.CompletedTask;
        }

        public Task SaveFailedCommandAsync(IMessageContext commandContext,
                                           Exception ex = null,
                                           params IMessageContext[] eventContexts)
        {
            var needSaveChanges = false;

            if (commandContext != null && Options.EnsureIdempotent)
            {
                var command = BuildCommand(commandContext, ex);
                command.Status = MessageStatus.Failed;
                Commands.Add(command);
                needSaveChanges = true;
            }

            eventContexts?.ForEach(eventContext =>
            {
                eventContext.CorrelationId = commandContext?.MessageId;
                if (Options.PersistEvent)
                {
                    Events.Add(BuildEvent(eventContext));
                    needSaveChanges = true;
                }

                if (Options.EnsureArrival)
                {
                    UnPublishedEvents.Add(new UnPublishedEvent(eventContext));
                    needSaveChanges = true;
                }
            });

            return needSaveChanges ? SaveChangesAsync() : Task.CompletedTask;
        }


        public abstract Task HandleEventAsync(IMessageContext eventContext,
                                              string subscriptionName,
                                              IEnumerable<IMessageContext> commandContexts,
                                              IEnumerable<IMessageContext> messageContexts);

        public abstract Task<bool> HasEventHandledAsync(string eventId, string subscriptionName);

        public abstract Task SaveFailHandledEventAsync(IMessageContext eventContext,
                                                       string subscriptionName,
                                                       Exception e,
                                                       params IMessageContext[] messageContexts);

        public virtual async Task<CommandHandledInfo> GetCommandHandledInfoAsync(string commandId)
        {
            CommandHandledInfo commandHandledInfo = null;

            if (!Options.EnsureIdempotent)
            {
                return null;
            }


            var command = await Commands.FirstOrDefaultAsync(c => c.Id == commandId)
                                        .ConfigureAwait(false);
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


        public IEnumerable<IMessageContext> GetAllUnSentCommands(
            Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage)
        {
            if (!Options.EnsureArrival)
            {
                return new IMessageContext[0];
            }
            return GetAllUnSentMessages<UnSentCommand>(wrapMessage);
        }

        public IEnumerable<IMessageContext> GetAllUnPublishedEvents(
            Func<string, IMessage, string, string, string, SagaInfo, string, IMessageContext> wrapMessage)
        {
            if (!Options.EnsureArrival)
            {
                return new IMessageContext[0];
            }
            return GetAllUnSentMessages<UnPublishedEvent>(wrapMessage);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<Command>()
                        .OwnsOne(m => m.SagaInfo);

            modelBuilder.Entity<Command>()
                        .Ignore(c => c.Reply)
                        .Property(c => c.CorrelationId)
                        .HasMaxLength(200)
                ;

            modelBuilder.Entity<HandledEvent>()
                        .HasKey(e => new {e.Id, e.SubscriptionName});

            //modelBuilder.Entity<HandledEvent>()
            //            .Property(handledEvent => handledEvent.SubscriptionName)
            //            .HasMaxLength(322)
            //  ;

            modelBuilder.Entity<HandledEvent>()
                        .OwnsOne(e => e.MessageOffset);

            modelBuilder.Entity<Command>()
                        .Property(c => c.Name)
                        .HasMaxLength(200);

            modelBuilder.Entity<Command>()
                        .Property(c => c.Topic)
                        .HasMaxLength(200);


            var eventEntityBuilder = modelBuilder.Entity<Event>();
            eventEntityBuilder.HasIndex(e => e.AggregateRootId);
            eventEntityBuilder.HasIndex(e => e.CorrelationId);
            eventEntityBuilder.HasIndex(e => e.Name);


            eventEntityBuilder.Property(e => e.Name)
                              .HasMaxLength(200);
            eventEntityBuilder.Property(e => e.AggregateRootId)
                              .HasMaxLength(200);
            eventEntityBuilder.Property(e => e.CorrelationId)
                              .HasMaxLength(200);
            eventEntityBuilder.Property(e => e.Topic)
                              .HasMaxLength(200);

            eventEntityBuilder.OwnsOne(e => e.SagaInfo);

            modelBuilder.Entity<UnSentCommand>()
                        .OwnsOne(m => m.SagaInfo);


            modelBuilder.Entity<UnPublishedEvent>()
                        .OwnsOne(m => m.SagaInfo);
        }

        protected virtual Command BuildCommand(IMessageContext commandContext, object result)
        {
            return new Command(commandContext, result);
        }

        protected virtual Event BuildEvent(IMessageContext eventContext)
        {
            return new Event(eventContext);
        }

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
                        if (message.MessageBody.ToJsonObject(Type.GetType(message.Type), true) is IMessage rawMessage)
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
                    catch (Exception ex)
                    {
                        Set<TMessage>().Remove(message);
                        Logger.LogError(ex, "get unsent message error: {0}", message.ToJson());
                    }
                });
            SaveChanges();
            return messageContexts;
        }
    }
}