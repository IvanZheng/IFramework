using System;
using System.Net;
using IFramework.Command;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.EntityFrameworkCore;
using IFramework.Infrastructure;
using IFramework.JsonNetCore;
using IFramework.Message;
using IFramework.MessageQueue;
using IFramework.MessageQueueCore.ConfluentKafka;
using IFramework.MessageQueueCore.InMemory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.Command;
using Sample.CommandServiceCore.Authorizations;
using Sample.CommandServiceCore.CommandInputExtension;
using Sample.CommandServiceCore.ExceptionHandlers;
using Sample.Domain;
using Sample.Persistence;
using Sample.Persistence.Repositories;

namespace Sample.CommandServiceCore
{
    public class Startup
    {
        private static IMessagePublisher _messagePublisher;
        private static ICommandBus _commandBus;
        private static IMessageConsumer _commandConsumer1;
        private static IMessageConsumer _commandConsumer2;
        private static IMessageConsumer _commandConsumer3;
        private static IMessageConsumer _domainEventConsumer;
        private static IMessageConsumer _applicationEventConsumer;

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            //var builder = new ConfigurationBuilder()
            //    .AddJsonFile("appsettings.json")
            //    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
            //    .AddEnvironmentVariables();
            var kafkaBrokerList = new[]
            {
                new IPEndPoint(Utility.GetLocalIPV4(), 9092).ToString()
                //"192.168.99.60:9092"
            };
            Configuration.Instance
                         .UseAutofacContainer("Sample.CommandHandler",
                                              "Sample.DomainEventSubscriber",
                                              "Sample.AsyncDomainEventSubscriber",
                                              "Sample.ApplicationEventSubscriber")
                         .UseConfiguration(configuration)
                         .UseCommonComponents()
                         .UseJsonNet()
                         .UseEntityFrameworkComponents<SampleModelContext>()
                         .UseMessageStore<SampleModelContext>()
                         .UseInMemoryMessageQueue()
                         //.UseConfluentKafka(string.Join(",", kafkaBrokerList))
                         //.UseEQueue()
                         .UseCommandBus(Environment.MachineName, linerCommandManager: new LinearCommandManager())
                         .UseMessagePublisher("eventTopic");
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        //public void ConfigureServices(IServiceCollection services)
        //{
        //    Configuration.UseServiceContainer(services.BuildServiceProvider())
        //                 .RegisterCommonComponents();
        //    services.AddMvc();
        //}

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options => { options.InputFormatters.Insert(0, new CommandInputFormatter()); });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AppAuthorization",
                                  policyBuilder => { policyBuilder.Requirements.Add(new AppAuthorizationRequirement()); });
            });
            //services.AddDbContextPool<SampleModelContext>(options => options.UseInMemoryDatabase(nameof(SampleModelContext)));
            services.AddDbContextPool<SampleModelContext>(options => options.UseSqlServer(Configuration.GetConnectionString(nameof(SampleModelContext))));
            return IoCFactory.Instance
                             .RegisterComponents(RegisterComponents, ServiceLifetime.Scoped)
                             .Populate(services)
                             .Build();
        }

        private static void RegisterComponents(IObjectProviderBuilder providerBuilder, ServiceLifetime lifetime)
        {
            // TODO: register other components or services
            providerBuilder.Register<IAuthorizationHandler, AppAuthorizationHandler>(ServiceLifetime.Singleton);
            providerBuilder.Register<ICommunityRepository, CommunityRepository>(lifetime);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddLog4Net();

            StartMessageQueueComponents();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(new ExceptionHandlerOptions
                {
                    ExceptionHandlingPath = new PathString("/Home/Error"),
                    ExceptionHandler = AppExceptionHandler.Handle
                });
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute("default",
                                "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void StartMessageQueueComponents()
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

            _domainEventConsumer = MessageQueueFactory.CreateEventSubscriber("DomainEvent", "DomainEventSubscriber",
                                                                             Environment.MachineName, new[] {"DomainEventSubscriber"});
            _domainEventConsumer.Start();

            #endregion

            #region application event subscriber init

            _applicationEventConsumer = MessageQueueFactory.CreateEventSubscriber("AppEvent", "AppEventSubscriber",
                                                                                  Environment.MachineName, new[] {"ApplicationEventSubscriber"});
            _applicationEventConsumer.Start();

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
    }
}