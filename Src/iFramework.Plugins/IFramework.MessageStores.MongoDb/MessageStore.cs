using IFramework.Domain;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.Message.Impl;
using IFramework.MessageStores.Abstracts;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blueshift.EntityFrameworkCore.MongoDB;
using IFramework.MessageQueue;
using MongoDB.Driver.Linq;

namespace IFramework.MessageStores.MongoDb
{
    public abstract class MessageStore : Abstracts.MessageStore
    {
        public DbSet<HandledEventBase> HandledEvents { get; set; }

        protected MessageStore(DbContextOptions options) : base(options) { }

        public override Task HandleEventAsync(IMessageContext eventContext,
                                             string subscriptionName,
                                             IEnumerable<IMessageContext> commandContexts,
                                             IEnumerable<IMessageContext> messageContexts)
        {
            HandledEvents.Add(new HandledEvent(eventContext.MessageId, subscriptionName, eventContext.MessageOffset, DateTime.Now));
            commandContexts.ForEach(commandContext =>
            {
                commandContext.CorrelationId = eventContext.MessageId;
                // don't save command here like event that would be published to other bounded context
                UnSentCommands.Add(new UnSentCommand(commandContext));
            });
            messageContexts.ForEach(messageContext =>
            {
                messageContext.CorrelationId = eventContext.MessageId;
                Events.Add(BuildEvent(messageContext));
                UnPublishedEvents.Add(new UnPublishedEvent(messageContext));
            });
            return SaveChangesAsync();
        }

        public override async Task<CommandHandledInfo> GetCommandHandledInfoAsync(string commandId)
        {
            CommandHandledInfo commandHandledInfo = null;
            //var command = await Commands.FirstOrDefaultAsync(c => c.Id == commandId)
            //                            .ConfigureAwait(false);

            var command = await this.GetCollection<Abstracts.Command>()
                                    .AsQueryable()
                                    .Where(c => c.Id == commandId)
                                    .FirstOrDefaultAsync()
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

        public override Task<bool> HasEventHandledAsync(string eventId, string subscriptionName)
        {
            var handledEventId = $"{eventId}_{subscriptionName}";
            return this.GetCollection<HandledEventBase>()
                       .AsQueryable()
                       .AnyAsync(e => e.Id == handledEventId);

            //return await HandledEvents.CountAsync(@event => @event.Id == handledEventId)
            //                          .ConfigureAwait(false) > 0;
        }

        public override Task SaveFailHandledEventAsync(IMessageContext eventContext,
                                                       string subscriptionName,
                                                       Exception e,
                                                       params IMessageContext[] messageContexts)
        {
            HandledEvents.Add(new FailHandledEvent(eventContext.MessageId, subscriptionName, eventContext.MessageOffset, DateTime.Now, e));

            messageContexts.ForEach(messageContext =>
            {
                messageContext.CorrelationId = eventContext.MessageId;
                Events.Add(BuildEvent(messageContext));
                UnPublishedEvents.Add(new UnPublishedEvent(messageContext));
            });
            return SaveChangesAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<Abstracts.HandledEvent>();
            modelBuilder.Ignore<SagaInfo>();
            modelBuilder.Ignore<MessageOffset>();
            modelBuilder.Ignore<AggregateRoot>();
            modelBuilder.Ignore<Entity>();
            modelBuilder.Ignore<TimestampedAggregateRoot>();
            modelBuilder.Ignore<VersionedAggregateRoot>();
            modelBuilder.Ignore<ValueObject<SagaInfo>>();
            modelBuilder.Ignore<UnSentMessage>();
            modelBuilder.Ignore<Abstracts.Message>();

            modelBuilder.Entity<HandledEventBase>()
                        .HasKey(e => e.Id);
            modelBuilder.Entity<UnPublishedEvent>()
                        .HasKey(e => e.Id);
            modelBuilder.Entity<UnSentCommand>()
                        .HasKey(c => c.Id);
            modelBuilder.Entity<Abstracts.Command>()
                        .HasKey(c => c.Id);
            modelBuilder.Entity<Abstracts.Event>()
                        .HasKey(e => e.Id);
        }
    }
}