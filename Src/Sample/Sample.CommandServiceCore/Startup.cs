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
using IFramework.Log4Net;
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
using System.Net;
using IFramework.Infrastructure;
using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using IFramework.Infrastructure.Mailboxes;
using IFramework.Infrastructure.Mailboxes.Impl;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;

namespace Sample.CommandServiceCore
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _env;
        private static IMessagePublisher _messagePublisher;
        private static ICommandBus _commandBus;
        private static IMessageProcessor _commandConsumer1;
        private static IMessageProcessor _commandConsumer2;
        private static IMessageProcessor _commandConsumer3;
        private static IMessageProcessor _domainEventProcessor;
        private static IMessageProcessor _applicationEventProcessor;
        public static string PathBase;
        private static string _app = "uat";
        private static readonly string TopicPrefix = _app.Length == 0 ? string.Empty : $"{_app}.";
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLog4Net(new Log4NetProviderOptions {EnableScope = true});
            services.AddCustomOptions<MailboxOption>(options => options.BatchCount = 1000);
            services.AddCustomOptions<FrameworkConfiguration>();
            services.AddMvc(options =>
                    {
                        options.InputFormatters.Insert(0, new CommandInputFormatter());
                        options.InputFormatters.Add(new FormDataInputFormatter());
                        options.Filters.Add<ExceptionFilter>();
                    })
                    .AddControllersAsServices()
                    .AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver());
            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AppAuthorization",
                                  policyBuilder => { policyBuilder.Requirements.Add(new AppAuthorizationRequirement()); });
            });
            services.AddSingleton<IApiResultWrapAttribute, ApiResultWrapAttribute>();
            //services.AddMiniProfiler()
            //        .AddEntityFramework();
        }


        public void ConfigureContainer(ContainerBuilder builder)
        {
            var kafkaBrokerList = new[] {
                //new IPEndPoint(Utility.GetLocalIpv4(), 9092).ToString()
                "10.100.7.46:9092"
            };
            var rabbitConnectionFactory = new ConnectionFactory {
                Endpoint = new AmqpTcpEndpoint("10.100.7.46", 9012)
            };
            Configuration.Instance
                         //.UseUnityContainer()
                         .UseAutofacContainer(builder, a => a.GetName().Name.StartsWith("Sample"))
                         .UseConfiguration(_configuration)
                         .UseCommonComponents(_app)
                         .UseJsonNet()
                         .UseEntityFrameworkComponents(typeof(RepositoryBase<>))
                         .UseRelationalMessageStore<SampleModelContext>()
                         //.UseMongoDbMessageStore<SampleModelContext>()
                         //.UseInMemoryMessageQueue()
                         //.UseRabbitMQ(rabbitConnectionFactory)
                         .UseConfluentKafka(string.Join(",", kafkaBrokerList))
                         //.UseEQueue()
                         .UseCommandBus(Environment.MachineName, linerCommandManager: new LinearCommandManager())
                         .UseMessagePublisher("eventTopic")
                         .UseDbContextPool<SampleModelContext>(options =>
                         {
                             //options.EnableSensitiveDataLogging();
                             options.UseLazyLoadingProxies();
                             options.UseSqlServer(Configuration.Instance.GetConnectionString(nameof(SampleModelContext)));
                             //options.UseMySQL(Configuration.Instance.GetConnectionString($"{nameof(SampleModelContext)}.MySql"));
                             //options.UseMongoDb(Configuration.Instance.GetConnectionString($"{nameof(SampleModelContext)}.MongoDb"));
                             //options.UseInMemoryDatabase(nameof(SampleModelContext));
                         });
            ObjectProviderFactory.Instance
                                 .RegisterComponents(RegisterComponents, ServiceLifetime.Scoped);
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
            providerBuilder.RegisterMessageHandlers(new []{"CommandHandlers", "DomainEventSubscriber", "ApplicationEventSubscriber"}, lifetime);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app,
                              IHostingEnvironment env,
                              ILoggerFactory loggerFactory,
                              IMessageTypeProvider messageTypeProvider,
                              IOptions<FrameworkConfiguration> frameworkConfigOptions,
                              IMailboxProcessor mailboxProcessor,
                              IApplicationLifetime applicationLifetime)
        {
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
                               .Register("Modify", typeof(Modify));
           
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
                context.Request.EnableRewind();
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
            app.UseRouting();
            app.UseCors("default"); 
            app.UseEndpoints(endpoints  =>
            {
                endpoints.MapControllerRoute("default",
                                "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });

            app.UseLogLevelController();
            app.UseMessageProcessorDashboardMiddleware();

            var logger = loggerFactory.CreateLogger<Startup>(); 
            logger.SetMinLevel(LogLevel.Information); 
            logger.LogInformation($"Startup configured env: {env.EnvironmentName}");
        }

        private void StartMessageQueueComponents()
        {
            #region Command Consuemrs init

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