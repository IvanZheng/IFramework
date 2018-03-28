using System;
using System.Net;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using IFramework.Command;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.EntityFrameworkCore;
using IFramework.Infrastructure;
using IFramework.JsonNetCore;
using IFramework.Log4Net;
using IFramework.Message;
using IFramework.MessageQueue;
using IFramework.MessageQueueCore.ConfluentKafka;
using IFramework.MessageQueueCore.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.Command;
using Sample.CommandService.App_Start;
using Sample.Domain;
using Sample.Persistence;
using Sample.Persistence.Repositories;

namespace Sample.CommandService
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : HttpApplication
    {
        private static ILogger _Logger;
        private static IMessagePublisher _messagePublisher;
        private static ICommandBus _commandBus;
        private static IMessageProcessor _commandConsumer1;
        private static IMessageProcessor _commandConsumer2;
        private static IMessageProcessor _commandConsumer3;
        private static IMessageProcessor _domainEventProcessor;
        private static IMessageProcessor _applicationEventProcessor;

        private static void RegisterComponents(IObjectProviderBuilder providerBuilder, ServiceLifetime lifetime)
        {
            // TODO: register other components or services
            providerBuilder.Register<ICommunityRepository, CommunityRepository>(lifetime);
        }

        public static void Bootstrap()
        {
            try
            {
                var kafkaBrokerList = new[]
                {
                    new IPEndPoint(Utility.GetLocalIPV4(), 9092).ToString()
                    //"10.100.7.46:9092"
                };
                Configuration.Instance
                             .UseAutofacContainer(Assembly.GetExecutingAssembly().FullName,
                                                  "Sample.CommandHandler",
                                                  "Sample.DomainEventSubscriber",
                                                  "Sample.AsyncDomainEventSubscriber",
                                                  "Sample.ApplicationEventSubscriber")
                             .UseConfiguration(new ConfigurationBuilder().AddJsonFile("appsettings.json")
                                                                         .Build())
                             .UseCommonComponents()
                             .UseJsonNet()
                             .UseLog4Net()
                             .UseEntityFrameworkComponents<SampleModelContext>()
                             .UseMessageStore<SampleModelContext>()
                             .UseInMemoryMessageQueue()
                             //.UseConfluentKafka(string.Join(",", kafkaBrokerList))
                             //.UseEQueue()
                             .UseCommandBus(Environment.MachineName, linerCommandManager: new LinearCommandManager())
                             .UseMessagePublisher("eventTopic")
                             .UseDbContextPool<SampleModelContext>(options => options.UseInMemoryDatabase(nameof(SampleModelContext)))
                              //.UseDbContextPool<SampleModelContext>(options => options.UseSqlServer(Configuration.GetConnectionString(nameof(SampleModelContext))))
                             ;

                    IoCFactory.Instance
                              .RegisterComponents(RegisterComponents, ServiceLifetime.Scoped)
                              .Build();

                StartMessageQueueComponents();
                _Logger = IoCFactory.GetService<ILoggerFactory>().CreateLogger(typeof(WebApiApplication).Name);


                _Logger.LogDebug($"App Started");
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex.GetBaseException().Message, ex);
            }
        }

        private static void StartMessageQueueComponents()
        {
            #region Command Consuemrs init

            var commandQueueName = "commandqueue";
            _commandConsumer1 =
                MessageQueueFactory.CreateCommandConsumer(commandQueueName, "0", new[] {"CommandHandlers"});
            _commandConsumer1.Start();

            _commandConsumer2 =
                MessageQueueFactory.CreateCommandConsumer(commandQueueName, "1", new[] {"CommandHandlers"});
            _commandConsumer2.Start();

            _commandConsumer3 =
                MessageQueueFactory.CreateCommandConsumer(commandQueueName, "2", new[] {"CommandHandlers"});
            _commandConsumer3.Start();

            #endregion

            #region event subscriber init

            _domainEventProcessor = MessageQueueFactory.CreateEventSubscriber("DomainEvent", "DomainEventSubscriber",
                                                                              Environment.MachineName, new[] {"DomainEventSubscriber"});
            _domainEventProcessor.Start();

            #endregion

            #region application event subscriber init

            _applicationEventProcessor = MessageQueueFactory.CreateEventSubscriber("AppEvent", "AppEventSubscriber",
                                                                                   Environment.MachineName, new[] {"ApplicationEventSubscriber"});
            _applicationEventProcessor.Start();

            #endregion

            #region EventPublisher init

            _messagePublisher = MessageQueueFactory.GetMessagePublisher();
            _messagePublisher.Start();

            #endregion

            #region CommandBus init

            _commandBus = MessageQueueFactory.GetCommandBus();
            _commandBus.Start();

            #endregion
        }

        // ZeroMQ Application_Start
        protected void Application_Start()
        {
            Bootstrap();
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configuration
                               .RegisterMvcDependencyInjection()
                               .RegisterWebApiDependencyInjection()
                               .Register();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_End()
        {
            try
            {
                IoCFactory.Instance.ObjectProvider.Dispose();
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex.GetBaseException().Message, ex);
            }
            finally
            {
                _Logger.LogDebug($"App Ended");
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var ex = Server.GetLastError().GetBaseException(); //获取错误
            _Logger.LogError(ex.Message, ex);
        }
    }
}