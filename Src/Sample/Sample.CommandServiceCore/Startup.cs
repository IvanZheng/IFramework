using System;
using System.Linq;
using IFramework.AspNet;
using IFramework.Command;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.DependencyInjection.Unity;
using IFramework.EntityFrameworkCore;
using IFramework.JsonNet;
using IFramework.Message;
using IFramework.MessageQueue;
using IFramework.MessageQueue.ConfluentKafka;
using IFramework.MessageQueue.RabbitMQ;
using IFramework.MessageQueue.InMemory;
using IFramework.MessageStores.Relational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
using ApiResultWrapAttribute = Sample.CommandServiceCore.Filters.ApiResultWrapAttribute;
using System.Collections.Generic;
using IFramework.Infrastructure;
using IFramework.Event;
using IFramework.EventStore;
using IFramework.Infrastructure.Mailboxes;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.Logging.Log4Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;
using IFramework.Infrastructure.EventSourcing.Configurations;
using IFramework.EventStore.Redis;
using IFramework.Infrastructure.EventSourcing;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using IFramework.Infrastructure.EventSourcing.Stores;
using IFramework.Logging.Serilog;
using Sample.DomainEvents.Banks;

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
        private static IMessageProcessor _eventSourcingCommandConsumer;
        private static IMessageProcessor _eventSourcingEventProcessor;
        public static string PathBase;
        private static string _app = "uat";
        private static readonly string TopicPrefix = _app.Length == 0 ? string.Empty : $"{_app}.";
        private readonly IConfiguration _configuration;
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
         
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitConnectionFactory = new ConnectionFactory
            {
                Endpoint = new AmqpTcpEndpoint("10.100.7.46", 9012)
            };
            services//.AddUnityContainer()
                    .AddAutofacContainer(assemblyName => assemblyName.StartsWith("Sample"))
                    .AddConfiguration(_configuration)
                    //.AddLog4Net()
                    .AddSerilog()
                    .AddCommonComponents(_app)
                    .AddJsonNet()
                    .AddEntityFrameworkComponents(typeof(RepositoryBase<>))
                    .AddRelationalMessageStore<SampleModelContext>()
                    //.AddConfluentKafka()
                    .AddInMemoryMessageQueue()
                    //.AddRabbitMQ(rabbitConnectionFactory)
                    .AddMessagePublisher("eventTopic")
                    .AddCommandBus(Environment.MachineName, serialCommandManager: new SerialCommandManager())
                    .AddDbContextPool<SampleModelContext>(options =>
                    {
                        //options.EnableSensitiveDataLogging();
                        //options.UseLazyLoadingProxies();
                        //options.UseSqlServer(Configuration.Instance.GetConnectionString(nameof(SampleModelContext)));
                        options.UseMySql(Configuration.Instance.GetConnectionString($"{nameof(SampleModelContext)}.MySql")
                                         ,b => b.EnableRetryOnFailure()
                                         )
                               .AddInterceptors(new ReadCommittedTransactionInterceptor());
                        //options.UseMongoDb(Configuration.Instance.GetConnectionString($"{nameof(SampleModelContext)}.MongoDb"));
                        //options.UseInMemoryDatabase(nameof(SampleModelContext));
                    })
                    .AddEventSourcing()
                    ;
            //services.AddLog4Net(new Log4NetProviderOptions {EnableScope = false});
            services.AddCustomOptions<MailboxOption>(options => options.BatchCount = 1000);
            services.AddCustomOptions<FrameworkConfiguration>();

            services.AddHealthChecks();
            services.AddControllersWithViews(options =>
            {
                options.InputFormatters.Insert(0, new CommandInputFormatter());
                options.InputFormatters.Add(new FormDataInputFormatter());
                options.Filters.Add<ExceptionFilter>();
            });
            services.AddRazorPages()
                    .AddControllersAsServices()
                    .AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver());
            services.AddHttpContextAccessor();
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AppAuthorization",
                                  policyBuilder => { policyBuilder.Requirements.Add(new AppAuthorizationRequirement()); });
            });
            services.AddSingleton<IApiResultWrapAttribute, ApiResultWrapAttribute>();

            services.AddScoped<HomeController, HomeController>(new VirtualMethodInterceptorInjection(),
                                                               new InterceptionBehaviorInjection());
            services.AddSingleton<IAuthorizationHandler, AppAuthorizationHandler>();
            services.AddScoped<ICommunityRepository, CommunityRepository>();
            services.AddScoped<ICommunityService, CommunityService>(new InterfaceInterceptorInjection(),
                                                                    new InterceptionBehaviorInjection());
            services.RegisterMessageHandlers(new []{"CommandHandlers", "DomainEventSubscriber", "ApplicationEventSubscriber"});

            //services.AddMiniProfiler()
            //        .AddEntityFramework();
        }

        //public void ConfigureContainer(IObjectProviderBuilder providerBuilder)
        //{
        //    var lifetime = ServiceLifetime.Scoped;
        //    providerBuilder.Register<HomeController, HomeController>(lifetime,
        //                                                             new VirtualMethodInterceptorInjection(),
        //                                                             new InterceptionBehaviorInjection());
        //  }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
                              IHostEnvironment env,
                              ILoggerFactory loggerFactory,
                              IMessageTypeProvider messageTypeProvider,
                              IOptions<FrameworkConfiguration> frameworkConfigOptions,
                              IMailboxProcessor mailboxProcessor,
                              IHostApplicationLifetime applicationLifetime
                              //IEventStore eventStore,
                              //ISnapshotStore snapshotStore
            )
        {
            //eventStore.Connect()
            //          .GetAwaiter()
            //          .GetResult();
            //snapshotStore.Connect()
            //             .GetAwaiter()
            //             .GetResult();

            applicationLifetime.ApplicationStopping.Register(() =>
            {
                _commandConsumer1?.Stop();
                _commandConsumer2?.Stop();
                _commandConsumer3?.Stop();
                _domainEventProcessor?.Stop();
                _applicationEventProcessor?.Stop();
                _messagePublisher?.Stop();
                _commandBus?.Stop();
                mailboxProcessor.Stop();
            });
            mailboxProcessor.Start();
            messageTypeProvider.Register(new Dictionary<string, string>
                               {
                                   ["Login"] = "Sample.Command.Login, Sample.Command"
                               })
                               .Register("Modify", typeof(Modify))
                               .Register(nameof(CreateAccount), typeof(CreateAccount))
                               .Register(nameof(AccountCreated), typeof(AccountCreated));
           
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

            app.Use(next => context =>
            {
                context.Request.EnableBuffering();
                context.Response.EnableRewind();
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
            //app.UseRouting();
            app.UseCors("default");
            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapControllerRoute("default",
            //                    "{controller=Home}/{action=Index}/{id?}");
            //    endpoints.MapRazorPages();
            //});

            app.UseRouting();
 
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute("default",
            //                    "{controller=Home}/{action=Index}/{id?}");
            //});

            app.UseLogLevelController();
            app.UseMessageProcessorDashboardMiddleware();

            //loggerFactory.AddLog4NetProvider(new Log4NetProviderOptions {EnableScope = true});
            var logger = loggerFactory.CreateLogger<Startup>(); 
            logger.SetMinLevel(LogLevel.Information); 
            logger.LogInformation($"Startup configured env: {env.EnvironmentName}");
        }

        private void StartMessageQueueComponents()
        {
            #region Command Consuemrs init

            _eventSourcingCommandConsumer = EventSourcingFactory.CreateCommandConsumer($"{TopicPrefix}BankCommandQueue",
                                                                                       "0",
                                                                                       new[]
                                                                                       {
                                                                                           "BankAccountCommandHandlers",
                                                                                           "BankTransactionCommandHandlers"
                                                                                       });

            _eventSourcingCommandConsumer.Start();

            var commandQueueName = $"{TopicPrefix}commandqueue";
            _commandConsumer1 =
                MessageQueueFactory.CreateCommandConsumer(commandQueueName, "0", new[] { "CommandHandlers" });
            _commandConsumer1.Start();

            //_commandConsumer2 =
            //    MessageQueueFactory.CreateCommandConsumer(commandQueueName, "1", new[] { "CommandHandlers" });
            //_commandConsumer2.Start();

            //_commandConsumer3 =
            //    MessageQueueFactory.CreateCommandConsumer(commandQueueName, "2", new[] { "CommandHandlers" });
            //_commandConsumer3.Start();

            #endregion

            #region event subscriber init

            _domainEventProcessor = MessageQueueFactory.CreateEventSubscriber(new[]
                                                                              {
                                                                                  new TopicSubscription($"{TopicPrefix}DomainEvent"),
                                                                                  new TopicSubscription($"{TopicPrefix}ProductDomainEvent")
                                                                              },
                                                                              "DomainEventSubscriber",
                                                                              Environment.MachineName,
                                                                              new[] { "DomainEventSubscriber" });
            _domainEventProcessor.Start();

            _eventSourcingEventProcessor = EventSourcingFactory.CreateEventSubscriber(new[]
                                                                                      {
                                                                                          new TopicSubscription($"{TopicPrefix}BankDomainEvent")
                                                                                      }, 
                                                                                      "BankDomainEventSubscriber",
                                                                                      Environment.MachineName,
                                                                                      new[] {"BankDomainEventSubscriber"});
            _eventSourcingEventProcessor.Start();
            #endregion

            #region application event subscriber init

            _applicationEventProcessor = MessageQueueFactory.CreateEventSubscriber($"{TopicPrefix}AppEvent",
                                                                                   "AppEventSubscriber",
                                                                                   Environment.MachineName,
                                                                                   new[] { "ApplicationEventSubscriber" });
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