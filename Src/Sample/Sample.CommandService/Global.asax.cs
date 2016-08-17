
using IFramework.Command;
using IFramework.Config;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Threading.Tasks;
using IFramework.Command.Impl;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using IFramework.IoC;
using IFramework.MessageQueue.MSKafka.Config;
using Sample.Persistence;
using IFramework.MessageQueue;

namespace Sample.CommandService
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        static ILogger _Logger;
        static IMessagePublisher _MessagePublisher;
        static ICommandBus _CommandBus;
        static IMessageConsumer _CommandConsumer1;
        static IMessageConsumer _CommandConsumer2;
        static IMessageConsumer _CommandConsumer3;
        static IMessageConsumer _DomainEventConsumer;
        static IMessageConsumer _ApplicationEventConsumer;


        public static void Bootstrap()
        {
            try
            {
                Configuration.Instance
                            .UseLog4Net()
                            .MessageQueueUseMachineNameFormat()
                            .UseMessageQueue()
                            .UseMessageStore<SampleModelContext>()
                            .UseKafka("192.168.99.60:2181")
                            .UseCommandBus(Environment.MachineName, needMessageStore:true, linerCommandManager: new Sample.Command.LinearCommandManager())
                            .UseMessagePublisher("eventTopic", true);

                _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(WebApiApplication).Name);


                _Logger.Debug($"App Started");

                #region EventPublisher init
                _MessagePublisher = MessageQueueFactory.GetMessagePublisher();
                _MessagePublisher.Start();
                #endregion

                #region event subscriber init
                _DomainEventConsumer = MessageQueueFactory.CreateEventSubscriber("DomainEvent", "DomainEventSubscriber", Environment.MachineName, "DomainEventSubscriber");
                _DomainEventConsumer.Start();
                #endregion

                #region application event subscriber init
                _ApplicationEventConsumer = MessageQueueFactory.CreateEventSubscriber("AppEvent", "AppEventSubscriber", Environment.MachineName, "ApplicationEventConsumer");
                _ApplicationEventConsumer.Start();
                #endregion

                #region CommandBus init
                _CommandBus = MessageQueueFactory.GetCommandBus();
                _CommandBus.Start();
                #endregion

                #region Command Consuemrs init'
                //var commandQueueName = "commandqueue";
                //_CommandConsumer1 = MessageQueueFactory.CreateCommandConsumer(commandQueueName, "0", "CommandHandlers");
                //_CommandConsumer1.Start();

                //_CommandConsumer2 = MessageQueueFactory.CreateCommandConsumer(commandQueueName, "1", "CommandHandlers");
                //_CommandConsumer2.Start();

                //_CommandConsumer3 = MessageQueueFactory.CreateCommandConsumer(commandQueueName, "2", "CommandHandlers");
                //_CommandConsumer3.Start();
                #endregion
            }
            catch (Exception ex)
            {
                _Logger.Error(ex.GetBaseException().Message, ex);
            }
        }

        // ZeroMQ Application_Start
        protected void Application_Start()
        {
            Bootstrap();
            AreaRegistration.RegisterAllAreas();
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_End()
        {
            try
            {
                Task.WaitAll(
                    Task.Factory.StartNew(() => _CommandConsumer1.Stop()),
                    Task.Factory.StartNew(() => _CommandConsumer2.Stop()),
                    Task.Factory.StartNew(() => _CommandConsumer3.Stop()),
                    Task.Factory.StartNew(() => _CommandBus.Stop()),
                    Task.Factory.StartNew(() => _MessagePublisher.Stop()),
                    Task.Factory.StartNew(() => _DomainEventConsumer.Stop()),
                    Task.Factory.StartNew(() => _ApplicationEventConsumer.Stop())
                   );
            }
            catch (Exception ex)
            {
                _Logger.Error(ex.GetBaseException().Message, ex);
            }
            _Logger.Debug($"App Ended");
        }
        // EQueue Application_Start
        /*
        public static List<CommandConsumer> CommandConsumers = new List<CommandConsumer>();
         
        protected void Application_Start()
        {
            try
            {
                Configuration.Instance.UseLog4Net();

                Configuration.Instance
                             .CommandHandlerProviderBuild(null, "CommandHandlers")
                             .RegisterMvc();

                global::EQueue.Configuration
                .Create()
                .UseAutofac()
                .UseLog4Net()
                .UseJsonNet()
                .RegisterFrameworkComponents();

                new BrokerController().Initialize().Start();
                var consumerSettings = ConsumerSettings.Default;
                consumerSettings.MessageHandleMode = MessageHandleMode.Sequential;
                var producerPort = 5000;
                IEventPublisher eventPublisher = new EventPublisher("domainevent", 
                                                                    consumerSettings.BrokerAddress,
                                                                    producerPort);
                IoCFactory.Instance.CurrentContainer.RegisterInstance(typeof(IEventPublisher), eventPublisher);

                var eventHandlerProvider = IoCFactory.Resolve<IHandlerProvider>("AsyncDomainEventSubscriber");
                IMessageConsumer domainEventSubscriber = new DomainEventSubscriber("domainEventSubscriber1",
                                                                                   consumerSettings,
                                                                                   "DomainEventSubscriber",
                                                                                   "domainevent",
                                                                                   eventHandlerProvider);
                domainEventSubscriber.Start();
                IoCFactory.Instance.CurrentContainer.RegisterInstance("DomainEventConsumer", domainEventSubscriber);



                var commandHandlerProvider = IoCFactory.Resolve<ICommandHandlerProvider>();
                var commandConsumer1 = new CommandConsumer("consumer1", consumerSettings, 
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSettings.BrokerAddress,
                                                           producerPort,
                                                           commandHandlerProvider);

                var commandConsumer2 = new CommandConsumer("consumer2", consumerSettings,
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSettings.BrokerAddress,
                                                           producerPort,
                                                           commandHandlerProvider);

                var commandConsumer3 = new CommandConsumer("consumer3", consumerSettings,
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSettings.BrokerAddress,
                                                           producerPort,
                                                           commandHandlerProvider);

                commandConsumer1.Start();
                commandConsumer2.Start();
                commandConsumer3.Start();

                CommandConsumers.Add(commandConsumer1);
                CommandConsumers.Add(commandConsumer2);
                CommandConsumers.Add(commandConsumer3);

                ICommandBus commandBus = new CommandBus("CommandBus",
                                                        commandHandlerProvider,
                                                        IoCFactory.Resolve<ILinearCommandManager>(),
                                                        consumerSettings.BrokerAddress,
                                                        producerPort,
                                                        consumerSettings,
                                                        "CommandBus",
                                                        "Reply", 
                                                        "Command",
                                                        true);
                IoCFactory.Instance.CurrentContainer.RegisterInstance(typeof(ICommandBus),
                                                                      commandBus,
                                                                      new ContainerControlledLifetimeManager());
                commandBus.Start();

                AreaRegistration.RegisterAllAreas();
                WebApiConfig.Register(GlobalConfiguration.Configuration);
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.GetBaseException().Message, ex);
            }
        }
        */
        protected void Application_Error(object sender, EventArgs e)
        {

            Exception ex = Server.GetLastError().GetBaseException(); //获取错误
            _Logger.Error(ex.Message, ex);
        }
    }
}