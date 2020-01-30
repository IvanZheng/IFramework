using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Command;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.Event;
using IFramework.EventStore.Client;
using IFramework.EventStore.Redis;
using IFramework.Infrastructure.EventSourcing.Repositories;
using IFramework.Infrastructure.EventSourcing.Stores;
using IFramework.JsonNet;
using IFramework.Log4Net;
using IFramework.Message;
using IFramework.Test.Commands;
using IFramework.Test.EventSourcing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IFramework.Test
{
    public class EventStoreTests
    {
        public EventStoreTests()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                    .AddJsonFile("appsettings.json");
            var configuration = builder.Build();
            var services = new ServiceCollection();
            services.AddAutofacContainer()
                    .AddConfiguration(configuration)
                    .AddCommonComponents()
                    //.UseMicrosoftDependencyInjection()
                    //.UseUnityContainer()
                    //.UseAutofacContainer()
                    //.UseConfiguration(configuration)
                    //.UseCommonComponents()
                    .AddJsonNet()
                    .AddLog4Net()
                    .AddEventSourcing()
                    //.AddEventStoreClient()
                ;

            ObjectProviderFactory.Instance.Build(services);

            ObjectProviderFactory.GetService<IEventStore>()
                                 .Connect()
                                 .GetAwaiter()
                                 .GetResult();
            ObjectProviderFactory.GetService<ISnapshotStore>()
                                 .Connect()
                                 .GetAwaiter()
                                 .GetResult();

            ObjectProviderFactory.GetService<IMessageTypeProvider>()
                                 .Register(nameof(UserCreated), typeof(UserCreated))
                                 .Register(nameof(UserModified), typeof(UserModified))
                                 .Register(nameof(CreateUser), typeof(CreateUser));
        }

        [Fact]
        public async Task EventStreamAppendReadTest()
        {
            const string userId = "3";
            var name = $"ivan_{DateTime.Now.Ticks}";
            var correlationId = $"cmd{DateTime.Now.Ticks}";
            var sagaResult = userId;
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            {
                var eventStore = serviceScope.GetService<IEventStore>();
                var events = (await eventStore.GetEvents(userId)
                                              .ConfigureAwait(false))
                             .Cast<IAggregateRootEvent>()
                             .ToArray();
                IEvent @event;
                ICommand command;
                var expectedVersion = events.LastOrDefault()?.Version ?? 0;
                if (expectedVersion == 0)
                {
                    command = new CreateUser {Id = correlationId, UserName = name, UserId = userId};
                    @event = new UserCreated(userId, name, expectedVersion + 1);
                    await eventStore.AppendEvents(userId, 
                                                  expectedVersion,
                                                  command.Id,
                                                  command,
                                                  sagaResult,
                                                  @event)
                                    .ConfigureAwait(false);
                }
                else
                {
                    command = new ModifyUser {Id = correlationId, UserName = name, UserId = userId};
                    @event = new UserModified(userId, name, expectedVersion + 1);
                    await eventStore.AppendEvents(userId,
                                                  expectedVersion,
                                                  command.Id,
                                                  null,
                                                  sagaResult,
                                                  @event)
                                    .ConfigureAwait(false);
                }
                var commandEvents = await eventStore.GetEvents(userId, command.Id)
                                                    .ConfigureAwait(false);
                Assert.Equal(@event.Id, commandEvents.FirstOrDefault()?.Id);
            }
        }

        [Fact]
        public async Task TestSnapshotStore()
        {
            const string userId = "3";
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            {
                var repository = serviceScope.GetService<IEventSourcingRepository<EventSourcingUser>>();
                var user = await repository.GetByKeyAsync(userId)
                                           .ConfigureAwait(false);
                Assert.NotNull(user);
            }

        }

        [Fact]
        public async Task TestEventHandle()
        {
            var subscriber = "subscriber1";
            var eventId = "eventId3";
            var correlationId = $"cmd{DateTime.Now.Ticks}";
            var name = "ivan";
            const string userId = "3";
            var eventResult = "eventResult";
            var commands = new ICommand[] {new CreateUser{Id = correlationId, UserName = name, UserId = userId}};
            var events = new IEvent[] {new UserCreated(userId, name, 0), new UserModified(userId, name, 1)};
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            {
                var messageTypeProvider = serviceScope.GetService<IMessageTypeProvider>();
                messageTypeProvider.Register(nameof(UserCreated), typeof(UserCreated))
                                   .Register(nameof(UserModified), typeof(UserModified))
                                   .Register(nameof(CreateUser), typeof(CreateUser));
                var sagaResult = userId;
                var eventStore = serviceScope.GetService<IEventStore>();
                await eventStore.Connect()
                                .ConfigureAwait(false);
                var result = await eventStore.HandleEvent("subscriber1", eventId, commands, events, sagaResult, eventResult)
                                             .ConfigureAwait(false);
                Assert.NotEmpty(result.Item1);
                Assert.NotEmpty(result.Item2);
            }
        }
    }
}