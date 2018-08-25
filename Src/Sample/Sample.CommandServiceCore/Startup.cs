using System;
using System.Linq;
using System.Net;
using IFramework.AspNet;
using IFramework.Command;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.EntityFrameworkCore;
using IFramework.Infrastructure;
using IFramework.JsonNet;
using IFramework.Log4Net;
using IFramework.Message;
using IFramework.MessageQueue;
using IFramework.MessageQueue.ConfluentKafka;
using IFramework.MessageQueue.InMemory;
using IFramework.MessageQueue.RabbitMQ;
using IFramework.MessageStores.Relational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.Applications;
using Sample.Command;
using Sample.CommandServiceCore.Authorizations;
using Sample.CommandServiceCore.CommandInputExtension;
using Sample.CommandServiceCore.Controllers;
using Sample.CommandServiceCore.ExceptionHandlers;
using Sample.CommandServiceCore.Filters;
using Sample.Domain;
using Sample.Persistence;
using Sample.Persistence.Repositories;

namespace Sample.CommandServiceCore
{
    public class Startup
    {
        private static IMessagePublisher _messagePublisher;
        private static ICommandBus _commandBus;
        private static IMessageProcessor _commandConsumer1;
        private static IMessageProcessor _commandConsumer2;
        private static IMessageProcessor _commandConsumer3;
        private static IMessageProcessor _domainEventProcessor;
        private static IMessageProcessor _applicationEventProcessor;
        public static string PathBase;

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            var kafkaBrokerList = new[]
            {
                new IPEndPoint(Utility.GetLocalIpv4(), 9092).ToString()
                //"10.100.7.46:9092"
            };
            var hostName = "localhost";
            Configuration.Instance
                         .UseAutofacContainer(a => a.GetName().Name.StartsWith("Sample"))
                         .UseConfiguration(configuration)
                         .UseCommonComponents()
                         .UseJsonNet()
                         .UseEntityFrameworkComponents(typeof(RepositoryBase<>))
                         .UseRelationalMessageStore<SampleModelContext>()// true 表示使用inmemorydatabase, 默认为false
                         .UseInMemoryMessageQueue()
                         //.UseRabbitMQ(hostName)
                         //.UseConfluentKafka(string.Join(",", kafkaBrokerList))
                         //.UseEQueue()
                         .UseCommandBus(Environment.MachineName, linerCommandManager: new LinearCommandManager())
                         .UseMessagePublisher("eventTopic")
                         //.UseDbContextPool<SampleModelContext>(options => options.UseInMemoryDatabase(nameof(SampleModelContext)))
                         .UseDbContextPool<SampleModelContext>(options =>
                         {
                             options.EnableSensitiveDataLogging();
                             options.UseSqlServer(Configuration.Instance.GetConnectionString(nameof(SampleModelContext)));
                         })
                         ;
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.InputFormatters.Insert(0, new CommandInputFormatter());
                options.InputFormatters.Add(new FormDataInputFormatter());
                options.Filters.Add<ExceptionFilter>();
            })  
            .AddControllersAsServices();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AppAuthorization",
                                  policyBuilder => { policyBuilder.Requirements.Add(new AppAuthorizationRequirement()); });
            });
            services.AddSingleton<IApiResultWrapAttribute, Filters.ApiResultWrapAttribute>();
            services.AddMiniProfiler()
                    .AddEntityFramework();

            return ObjectProviderFactory.Instance
                                        .Populate(services)
                                        .RegisterComponents(RegisterComponents, ServiceLifetime.Scoped)
                                        .Build();
        }

        private static void RegisterComponents(IObjectProviderBuilder providerBuilder, ServiceLifetime lifetime)
        {
            // TODO: register other components or services
            providerBuilder.Register<IAuthorizationHandler, AppAuthorizationHandler>(ServiceLifetime.Singleton);
            providerBuilder.Register<ICommunityRepository, CommunityRepository>(lifetime); 
            providerBuilder.Register<ICommunityService, CommunityService>(lifetime,
                                                                     new InterfaceInterceptorInjection(),
                                                                     new InterceptionBehaviorInjection());
            providerBuilder.Register<HomeController, HomeController>(lifetime,
                                                                     new VirtualMethodInterceptorInjection(),
                                                                     new InterceptionBehaviorInjection());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.UseLog4Net(new Log4NetProviderOptions { EnableScope = true});
            StartMessageQueueComponents();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(new GlobalExceptionHandlerOptions(loggerFactory, env));
            }
            //app.UseMiniProfiler();
            PathBase = Configuration.Instance[nameof(PathBase)];
            app.UsePathBase(PathBase);

            app.Use(next => context =>
            {
                context.Request.Path = context.Request.Path.Value.Replace("//", "/");
                return next(context);
            });

            //app.Use(async (context, next) =>
            //{
            //    await next();
            //    if (context.Response.StatusCode == StatusCodes.Status404NotFound)
            //    {
            //        context.Request.Path = "/Home/Error";
            //        await next();
            //    }
            //});

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
                MessageQueueFactory.CreateCommandConsumer(commandQueueName, "0", new[] { "CommandHandlers" });
            _commandConsumer1.Start();

            _commandConsumer2 =
                MessageQueueFactory.CreateCommandConsumer(commandQueueName, "1", new[] { "CommandHandlers" });
            _commandConsumer2.Start();

            _commandConsumer3 =
                MessageQueueFactory.CreateCommandConsumer(commandQueueName, "2", new[] { "CommandHandlers" });
            _commandConsumer3.Start();

            #endregion

            #region event subscriber init

            _domainEventProcessor = MessageQueueFactory.CreateEventSubscriber(new []{"DomainEvent", "ProductDomainEvent"}, "DomainEventSubscriber",
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
    }
}