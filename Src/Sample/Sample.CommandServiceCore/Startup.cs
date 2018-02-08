using System;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Autofac;
using IFramework.JsonNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.CommandServiceCore.AuthorizationHandlers;
using Sample.CommandServiceCore.ExceptionHandlers;

namespace Sample.CommandServiceCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            //var builder = new ConfigurationBuilder()
            //    .AddJsonFile("appsettings.json")
            //    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
            //    .AddEnvironmentVariables();
            Configuration.Instance
                         .UseAutofacContainer()
                         .UseConfiguration(configuration)
                         .RegisterCommonComponents()
                         .UseJsonNet();
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
            services.AddMvc();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AppAuthorization",
                                  policyBuilder => { policyBuilder.Requirements.Add(new AppAuthorizationRequirement()); });
            });
            return IoCFactory.Instance
                             .RegisterComponents(RegisterComponents, ServiceLifetime.Singleton)
                             .Build(services);
        }

        private void RegisterComponents(IObjectProviderBuilder providerBuilder, ServiceLifetime lifetime)
        {
            // TODO: register other components or services
            providerBuilder.RegisterType<IAuthorizationHandler, AppAuthorizationHandler>(ServiceLifetime.Singleton);
            // providerBuilder.RegisterType<TInterface, TImplement>(lifetime);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddLog4Net();
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

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                                "default",
                                "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}