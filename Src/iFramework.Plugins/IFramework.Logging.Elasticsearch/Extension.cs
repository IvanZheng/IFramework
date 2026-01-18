using System;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.CommonSchema;
using Elastic.Extensions.Logging;
using Elastic.Extensions.Logging.Options;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Transport;
using IFramework.Config;
using IFramework.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Refit;

namespace IFramework.Logging.Elasticsearch
{
    public static class Extension
    {
        public static IServiceCollection AddElasticsearchLogging(this IServiceCollection services, IConfiguration configuration = null, IHostEnvironment env = null)
        {
            return AddElasticsearchLogging(services, null, configuration, env);
        }
        public static IServiceCollection AddElasticsearchLogging(this IServiceCollection services, Action<ElasticsearchLogOptions> optionsAction = null, IConfiguration configuration = null, IHostEnvironment env = null)
        {
            var options = BuildElasticsearchLogOptions(optionsAction, configuration, env);
            var settings = new ElasticsearchClientSettings(new Uri(options.BaseAddress));
            settings.Authentication(new BasicAuthentication(options.UserName, options.Password))
                    .DisableDirectStreaming();
            var client = new ElasticsearchClient(settings);
            return services.AddElasticsearchLogging(client, options);
        }


        public static IServiceCollection AddElasticsearchLogging(this IServiceCollection services, Func<ElasticsearchClientSettings> settingsFunc, Action<ElasticsearchLogOptions> optionsAction = null, IConfiguration configuration = null, IHostEnvironment env = null)
        {
            var options = BuildElasticsearchLogOptions(optionsAction, configuration, env);
            var client = new ElasticsearchClient(settingsFunc());
            return services.AddElasticsearchLogging(client, options);
        }

        private static IServiceCollection AddElasticsearchLogging(this IServiceCollection services, ElasticsearchClient elasticsearchClient, ElasticsearchLogOptions options)
        {
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddElasticsearch(elasticsearchClient.Transport,
                                                log =>
                                                {
                                                    log.Index = new IndexNameOptions
                                                    {
                                                        Format = $"{options.Index}-{DateTime.UtcNow:yyyy.MM.dd}"
                                                    };
                                                },
                                                channel =>
                                                {
                                                    if (channel is IndexChannelOptions<LogEvent> indexChannelOptions)
                                                    {
                                                        options.IndexChannelOptionsAction?.Invoke(indexChannelOptions);
                                                        indexChannelOptions.TimestampLookup = logEvent =>
                                                        {
                                                            if (logEvent.Labels == null)
                                                            {
                                                                logEvent.Labels = new Labels();
                                                            }
                                                            logEvent.Labels[nameof(options.App)] = options.App;
                                                            logEvent.Labels[nameof(options.Env)] = options.Env;
                                                            return DateTimeOffset.Now;
                                                        };
                                                    }
                                                    
                                                    channel.ExportResponseCallback = (response, buffer) =>
                                                    {
                                                        Console.WriteLine($"Written  {buffer.Count} logs to Elasticsearch: {response.ApiCallDetails.HttpStatusCode}");
                                                    };
                                                });
            });
            return services;
        }

        public static IServiceCollection AddLogstash(this IServiceCollection services,
                                                     RefitSettings refitSettings = null,
                                                     LogLevel minLevel = LogLevel.Warning,
                                                     Action<ElasticsearchLogOptions> options = null,
                                                     IConfiguration configuration = null,
                                                     IHostEnvironment env = null)
        {
            services.AddLogStashClient(refitSettings)
                    .AddLogging(config =>
                    {
                        var providerOptions = BuildElasticsearchLogOptions(options, configuration, env);
                        config.AddProvider(new ElasticsearchLoggerProvider(providerOptions, minLevel));
                    });
            return services;
        }

        private static ElasticsearchLogOptions BuildElasticsearchLogOptions(Action<ElasticsearchLogOptions> options, IConfiguration configuration, IHostEnvironment env)
        {
            configuration = configuration ?? Configuration.Instance;
           
            var providerOptions = new ElasticsearchLogOptions();
            if (options != null)
            {
                providerOptions = new ElasticsearchLogOptions();
                options.Invoke(providerOptions);
            }
            else
            {
                var section = configuration.GetSection(nameof(ElasticsearchLogOptions));
                if (section.Exists())
                {
                    providerOptions = section.Get<ElasticsearchLogOptions>();
                }
            }

            if (env != null && string.IsNullOrWhiteSpace(providerOptions.Env))
            {
                providerOptions.Env = env.EnvironmentName;
            }
            return providerOptions;
        }

        public static IServiceCollection AddLogStashClient(this IServiceCollection services,
                                                           RefitSettings refitSettings = null,
                                                           Action<ElasticsearchLogOptions> optionsAction = null)
        {
            if (refitSettings == null)
            {
                refitSettings = new RefitSettings
                {
                    ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        //DateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFF"
                    })
                };
            }

            services.AddCustomOptions(optionsAction);

            return services.AddRefitClient<IElasticSearchService>(refitSettings)
                           .ConfigureHttpClient((provider, httpClient) =>
                           {
                               var options = provider.GetService<IOptions<ElasticsearchLogOptions>>()?.Value;
                               if (string.IsNullOrWhiteSpace(options?.BaseAddress))
                               {
                                   throw new Exception("ElasticsearchLogOptions ApiHost is null");
                               }

                               httpClient.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(options.UserName, options.Password);
                               httpClient.BaseAddress = new Uri(options.BaseAddress);
                           })
                           .Services;
        }
    }
}