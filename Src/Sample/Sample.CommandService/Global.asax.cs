
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
                            .UseKafka("localhost:2181")
                            //.UseEQueue()
                            .UseCommandBus(Environment.MachineName, linerCommandManager: new Sample.Command.LinearCommandManager(), mailboxProcessBatchCount: 100)
                            .UseMessagePublisher("eventTopic");

                _Logger = IoCFactory.Resolve<ILoggerFactory>().Create(typeof(WebApiApplication).Name);


                _Logger.Debug($"App Started");

                #region EventPublisher init
                _MessagePublisher = MessageQueueFactory.GetMessagePublisher();
                _MessagePublisher.Start();
                #endregion

                #region event subscriber init
                _DomainEventConsumer = MessageQueueFactory.CreateEventSubscriber("DomainEvent", "DomainEventSubscriber", Environment.MachineName, 100, "DomainEventSubscriber");
                _DomainEventConsumer.Start();
                #endregion

                #region application event subscriber init
                _ApplicationEventConsumer = MessageQueueFactory.CreateEventSubscriber("AppEvent", "AppEventSubscriber", Environment.MachineName, 100, "ApplicationEventSubscriber");
                _ApplicationEventConsumer.Start();
                #endregion

                #region CommandBus init
                _CommandBus = MessageQueueFactory.GetCommandBus();
                _CommandBus.Start();
                #endregion

                #region Command Consuemrs init'
                var commandQueueName = "commandqueue";
                _CommandConsumer1 = MessageQueueFactory.CreateCommandConsumer(commandQueueName, "0", 100, "CommandHandlers");
                _CommandConsumer1.Start();

                _CommandConsumer2 = MessageQueueFactory.CreateCommandConsumer(commandQueueName, "1", 100, "CommandHandlers");
                _CommandConsumer2.Start();

                _CommandConsumer3 = MessageQueueFactory.CreateCommandConsumer(commandQueueName, "2", 100, "CommandHandlers");
                _CommandConsumer3.Start();
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
                IoCFactory.Instance.CurrentContainer.Dispose();
                var endSuccesss = Task.WaitAll(new Task[] {
                    Task.Run(() => _CommandConsumer1?.Stop()),
                    Task.Run(() => _CommandConsumer2?.Stop()),
                    Task.Run(() => _CommandConsumer3?.Stop()),
                    Task.Run(() => _DomainEventConsumer?.Stop()),
                    Task.Run(() => _ApplicationEventConsumer?.Stop()),
                    Task.Run(() => _CommandBus?.Stop()),
                    Task.Run(() => _MessagePublisher?.Stop())
                }, 10000);
                if (!endSuccesss)
                {
                    throw new Exception($"stop message queue client timeout!");
                }
            }
            catch (Exception ex)
            {
                _Logger.Error(ex.GetBaseException().Message, ex);
            }
            _Logger.Debug($"App Ended");
        }
      
        protected void Application_Error(object sender, EventArgs e)
        {

            Exception ex = Server.GetLastError().GetBaseException(); //获取错误
            _Logger.Error(ex.Message, ex);
        }
    }
}