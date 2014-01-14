using IFramework.Command;
using IFramework.Config;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.MessageQueue.ZeroMQ;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Sample.CommandService
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            try
            {
                var linearCommandManager = IoCFactory.Resolve<ILinearCommandManager>();

                var commandDistributer = new CommandDistributer(linearCommandManager,
                                                "inproc://distributer",
                                                new string[] { "inproc://CommandConsumer1"
                                                    , "inproc://CommandConsumer2"
                                                    , "inproc://CommandConsumer3"
                                                }
                                                );

                Configuration.Instance
                             .RegisterCommandConsumer(commandDistributer, "CommandConsumer")
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
                commandDistributer.Start();

                ICommandBus commandBus = IoCFactory.Resolve<ICommandBus>();
                (commandBus as IMessageConsumer).Start();
                /*
                 * One Consumer Case
                Configuration.Instance
                             .CommandHandlerProviderBuild(null, "CommandHandlers")
                             .RegisterMvc()
                             .MvcIgnoreResouceRoute(RouteTable.Routes);
                 
                var commandHandlerProvider = IoCFactory.Resolve<ICommandHandlerProvider>();
                var commandConsumer = new CommandConsumer(commandHandlerProvider, "", "");
                commandConsumer.StartConsuming();
                Configuration.Instance
                             .RegisterCommandConsumer(commandConsumer, "CommandConsumer");

                IoCFactory.Resolve<IEventPublisher>();
                IoCFactory.Resolve<IMessageConsumer>("DomainEventConsumer").StartConsuming();
                 */

            }
            catch (Exception ex)
            {
                if (ex.InnerException is System.IO.FileLoadException)
                {
                    var typeLoadException = ex.InnerException as System.IO.FileLoadException;
                    var loaderExceptions = typeLoadException.FusionLog;
                    // var str = string.Empty;
                    //loaderExceptions.ForEach(le => str += le.Message + " " + le.Data.ToJson());
                    Console.WriteLine(loaderExceptions);
                }
            }
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_Error(object sender, EventArgs e)
        {

            Exception objErr = Server.GetLastError().GetBaseException(); //获取错误
            Response.Write(objErr.Message + objErr.StackTrace);
        }
    }
}