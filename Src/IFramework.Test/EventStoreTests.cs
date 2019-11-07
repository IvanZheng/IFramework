using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.Event;
using IFramework.EventStore.Client;
using IFramework.JsonNet;
using IFramework.Log4Net;
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
                    .AddEventStoreClient();

            ObjectProviderFactory.Instance.Build(services);
        }

        [Fact]
        public async Task EventStreamAppendReadTest()
        {
            var streamId = "1";
            var expectedVersion = 0;
            using (var serviceScope = ObjectProviderFactory.CreateScope())
            {
                var eventStore = serviceScope.GetService<IEventStore>();
                var events = await eventStore.GetEvents(streamId)
                                             .ConfigureAwait(false);
                //expectedVersion = events.FirstOrDefault().
                //eventStore.AppendEvents(streamId, expectedVersion, events);
            }
        }
    }
}
