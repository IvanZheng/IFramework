using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.DependencyInjection.Unity;
using IFramework.JsonNet;
using IFramework.Log4Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;

namespace IFramework.KafkaTools
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration.Instance
                         .UseUnityContainer()
                         //.UseAutofacContainer(a => a.GetName().Name.StartsWith("Sample"))
                         .UseConfiguration(configuration)
                         .UseCommonComponents()
                         .UseJsonNet();

        }
        

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
            });
            return ObjectProviderFactory.Instance
                                        .Populate(services)
                                        .Build();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddLog4Net(new Log4NetProviderOptions {EnableScope = true});
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            loggerFactory.CreateLogger<Startup>().LogDebug($"Aplication started {env.EnvironmentName}!");
        }
    }
}
