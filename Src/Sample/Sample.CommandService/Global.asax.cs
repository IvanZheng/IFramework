using EQueue.Clients.Consumers;
using IFramework.Command;
using IFramework.Config;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using IFramework.Message;
using IFramework.MessageQueue.EQueue;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using EQueue.Autofac;
using EQueue.Log4Net;
using EQueue.JsonNet;
using EQueue.Broker;

namespace Sample.CommandService
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        ILogger _Logger;
        ILogger Logger
        {
            get
            {
                return _Logger ?? (_Logger = IoCFactory.Resolve<ILoggerFactory>().Create(this.GetType()));
            }
        }

        // ZeroMQ Application_Start
        /*protected void Application_Start()
        {
            try
            {
                Configuration.Instance.UseLog4Net();

                var commandDistributor = new CommandDistributor("inproc://distributor",
                                                                new string[] { 
                                                                    "inproc://CommandConsumer1"
                                                                    , "inproc://CommandConsumer2"
                                                                    , "inproc://CommandConsumer3"
                                                                }
                                                               );

                Configuration.Instance.RegisterCommandConsumer(commandDistributor, "CommandDistributor")
                             .CommandHandlerProviderBuild(null, "CommandHandlers")
                             .RegisterMvc();

                IoCFactory.Resolve<IEventPublisher>();
                IoCFactory.Resolve<IMessageConsumer>("DomainEventConsumer").Start();

                var commandHandlerProvider = IoCFactory.Resolve<ICommandHandlerProvider>();
                var commandConsumer1 = new CommandConsumer(commandHandlerProvider,
                                                           "inproc://CommandConsumer1");
                var commandConsumer2 = new CommandConsumer(commandHandlerProvider,
                                                           "inproc://CommandConsumer2");
                var commandConsumer3 = new CommandConsumer(commandHandlerProvider,
                                                           "inproc://CommandConsumer3");


                commandConsumer1.Start();
                commandConsumer2.Start();
                commandConsumer3.Start();
                commandDistributor.Start();

                ICommandBus commandBus = IoCFactory.Resolve<ICommandBus>();
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

        // EQueue Application_Start
        
        
        
        protected void Application_Start()
        {
            try
            {
                Configuration.Instance.UseLog4Net();

                Configuration.Instance
                             .CommandHandlerProviderBuild(null, "CommandHandlers")
                             .RegisterMvc();

                //IoCFactory.Resolve<IEventPublisher>();
                //IoCFactory.Resolve<IMessageConsumer>("DomainEventConsumer").Start();

                var commandHandlerProvider = IoCFactory.Resolve<ICommandHandlerProvider>();


                global::EQueue.Configuration
                .Create()
                .UseAutofac()
                .UseLog4Net()
                .UseJsonNet()
                .RegisterFrameworkComponents();

                new BrokerController().Initialize().Start();

                var consumerSettings = ConsumerSettings.Default;

                var commandConsumer1 = new CommandConsumer("consumer1", consumerSettings, 
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSettings.BrokerAddress,
                                                           5000,
                                                           commandHandlerProvider);

                var commandConsumer2 = new CommandConsumer("consumer2", consumerSettings,
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSettings.BrokerAddress,
                                                           5000,
                                                           commandHandlerProvider);

                var commandConsumer3 = new CommandConsumer("consumer3", consumerSettings,
                                                           "CommandConsumerGroup",
                                                           "Command",
                                                           consumerSettings.BrokerAddress,
                                                           5000,
                                                           commandHandlerProvider);

                //commandConsumer1.Start();
                //commandConsumer2.Start();
                //commandConsumer3.Start();

                ICommandBus commandBus = new CommandBus("CommandBus",
                                                        commandHandlerProvider,
                                                        IoCFactory.Resolve<ILinearCommandManager>(),
                                                        consumerSettings.BrokerAddress,
                                                        5000,
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

        protected void Application_Error(object sender, EventArgs e)
        {

            Exception ex = Server.GetLastError().GetBaseException(); //获取错误
            Logger.Debug(ex.Message, ex);
        }
    }
}