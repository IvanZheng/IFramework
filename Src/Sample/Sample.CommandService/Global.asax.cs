using IFramework.Command;
using IFramework.Config;
using IFramework.Event;
using IFramework.Infrastructure;
using IFramework.Message;
using IFramework.MessageQueue.ZeroMQ;
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

                var commandDistributor = new CommandDistributer(string.Empty,
                                                "inproc://ReplyReceiver",
                                                new string[] { "inproc://CommandConsumer1",
                                                    "inproc://CommandConsumer2",
                                                    "inproc://CommandConsumer3"},
                                                linearCommandManager);
               
                Configuration.Instance
                             .RegisterCommandConsumer(commandDistributor, "CommandConsumer")
                             .CommandHandlerProviderBuild(null, "CommandHandlers")
                             .RegisterMvc()
                             .MvcIgnoreResouceRoute(RouteTable.Routes);

                IoCFactory.Resolve<IEventPublisher>();
                IoCFactory.Resolve<IMessageConsumer>("DomainEventConsumer").StartConsuming();


                var commandHandlerProvider = IoCFactory.Resolve<ICommandHandlerProvider>();
                var commandConsumer1 = new CommandConsumer(commandHandlerProvider,
                                                           "inproc://ReplyReceiver",
                                                           "inproc://CommandConsumer1");
                var commandConsumer2 = new CommandConsumer(commandHandlerProvider,
                                                           "inproc://ReplyReceiver",
                                                           "inproc://CommandConsumer2");
                var commandConsumer3 = new CommandConsumer(commandHandlerProvider,
                                                           "inproc://ReplyReceiver",
                                                           "inproc://CommandConsumer3");
                commandDistributor.StartConsuming();

                commandConsumer1.StartConsuming();
                commandConsumer2.StartConsuming();
                commandConsumer3.StartConsuming();

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