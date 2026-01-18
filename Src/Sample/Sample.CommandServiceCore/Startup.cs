using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autofac;
using IFramework.AspNet;
using IFramework.AspNet.Swagger;
using IFramework.Command;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.EntityFrameworkCore;
using IFramework.EventStore.Redis;
using IFramework.Exceptions;
using IFramework.Infrastructure.EventSourcing;
using IFramework.Infrastructure.Mailboxes;
using IFramework.Infrastructure.Mailboxes.Impl;
using IFramework.JsonNet;
using IFramework.Logging.Elasticsearch;
using IFramework.Logging.Serilog;
using IFramework.Message;
using IFramework.MessageQueue;
using IFramework.MessageQueue.ConfluentKafka;
using IFramework.MessageQueue.InMemory;
using IFramework.MessageQueue.RocketMQ;
using IFramework.MessageStores.Relational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;
using Sample.Applications;
using Sample.Command;
using Sample.CommandServiceCore.Authorizations;
using Sample.CommandServiceCore.CommandInputExtension;
using Sample.CommandServiceCore.Controllers;
using Sample.CommandServiceCore.ExceptionHandlers;
using Sample.CommandServiceCore.Filters;
using Sample.Domain;
using Sample.DomainEvents.Banks;
using Sample.Persistence;
using Sample.Persistence.Repositories;
using static IdentityModel.ClaimComparer;
using ApiResultWrapAttribute = Sample.CommandServiceCore.Filters.ApiResultWrapAttribute;

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
        private const string QueueNameSplit = "-";
        private const string App = "uat";
        private static readonly string TopicPrefix = App.Length == 0 ? string.Empty : $"{App}{QueueNameSplit}";
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitConnectionFactory = new ConnectionFactory
            {
                Endpoint = new AmqpTcpEndpoint("10.100.7.46", 9012)
            };
            services //.AddUnityContainer()
                .AddAutofacContainer(assemblyName => assemblyName.StartsWith("Sample"))
                .AddConfiguration(_configuration)
                //.AddLog4Net()
                .AddSerilog()
                .AddCommonComponents(App, QueueNameSplit)
                .AddJsonNet()
                .AddEntityFrameworkComponents(typeof(RepositoryBase<>))
                .AddRelationalMessageStore<SampleModelContext>()
                //.AddConfluentKafka()
                //.AddRocketMQ()
                .AddInMemoryMessageQueue()
                //.AddRabbitMQ(rabbitConnectionFactory)
                .AddMessagePublisher("eventTopic")
                .AddCommandBus(Environment.MachineName, serialCommandManager: new SerialCommandManager())
                .AddDbContextPool<SampleModelContext>(options =>
                {
                    var connectionString = Configuration.Instance.GetConnectionString($"{nameof(SampleModelContext)}.MySql");
                    //options.EnableSensitiveDataLogging();
                    //options.UseLazyLoadingProxies();
                    //options.UseSqlServer(Configuration.Instance.GetConnectionString(nameof(SampleModelContext)));
                    //options.UseMySql(connectionString,
                    //                 ServerVersion.AutoDetect(connectionString),
                    //                 b => b.EnableRetryOnFailure())
                    //       .AddInterceptors(new ReadCommittedTransactionInterceptor())
                    //       .UseLazyLoadingProxies();
                    //options.UseMongoDb(Configuration.Instance.GetConnectionString($"{nameof(SampleModelContext)}.MongoDb"));
                    options.UseInMemoryDatabase(nameof(SampleModelContext));
                    //options.UseDm(Configuration.Instance.GetConnectionString($"{nameof(SampleModelContext)}.Dm"),
                    //              b => b.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name));
                    options.ConfigureWarnings(b => { b.Ignore(InMemoryEventId.TransactionIgnoredWarning); });
                })
                .AddDatabaseDeveloperPageExceptionFilter()
                .AddEventSourcing()
                //.AddElasticsearchLogging(_configuration, _env)
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
                options.ModelBinderProviders.RemoveType<DateTimeModelBinderProvider>();
            });
            services.AddRazorPages()
                    .AddControllersAsServices()
                    .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

                        var namesContractResolver = new CamelCasePropertyNamesContractResolver();
                        if (namesContractResolver.NamingStrategy != null)
                        {
                            namesContractResolver.NamingStrategy.ProcessDictionaryKeys = false;
                        }
                        options.SerializerSettings.ContractResolver = namesContractResolver;
                        
                        options.SerializerSettings.Converters.Add(new EnumConverter(typeof(ErrorCode)));
                        options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm";
                    });
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
            services.RegisterMessageHandlers(new[] { "CommandHandlers", "DomainEventSubscriber", "ApplicationEventSubscriber" });

            services.AddSwaggerGen(c =>
            {
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Sample.CommandServiceCore.xml"));
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sample.CommandServiceCore.API", Version = "v1" });
                c.OperationFilter<SwaggerDefaultValues>();
                c.DocumentFilter<SwaggerAddEnumDescriptions>(nameof(Sample));
                c.SchemaFilter<EnumSchemaFilter>();
            });

            //services.AddMiniProfiler()
            //        .AddEntityFramework();
        }

        public void ConfigureContainer(IObjectProviderBuilder providerBuilder)
        {
            providerBuilder.RegisterDbContextPool<SampleModelContext>();
        }

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
            using var scopeProvider = app.ApplicationServices.CreateScope();
            var dbContext = scopeProvider.ServiceProvider.GetRequiredService<SampleModelContext>();
            dbContext.Database.EnsureCreated();
            //eventStore.Connect()
            //          .GetAwaiter()
            //          .GetResult();
            //snapshotStore.Connect()
            //             .GetAwaiter()
            //             .GetResult();
            var logger = loggerFactory.CreateLogger<Startup>();
            logger.SetMinLevel(LogLevel.Information);
            logger.LogInformation($"Startup configured env: {env.EnvironmentName} " +
                                  $"version: {FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion} " +
                                  Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);

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

            var basePath = _configuration["BasePath"];
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                app.UsePathBase(basePath);
            }

            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint($"{basePath}/swagger/v1/swagger.json", "IFramework.Sample.API V1"); });


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
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
                if (!string.IsNullOrWhiteSpace(context.Request.Path.Value))
                {
                    context.Request.Path = context.Request.Path.Value.Replace("//", "/");
                }

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
                MessageQueueFactory.CreateCommandConsumer(commandQueueName,
                                                          "0",
                                                          new[] { "CommandHandlers" });
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
                                                                                      new[] { "BankDomainEventSubscriber" });
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