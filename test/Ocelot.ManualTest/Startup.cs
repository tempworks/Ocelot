using System;
using System.Threading.Tasks;
using CacheManager.Core;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Authentication.Middleware;
using Ocelot.Authorisation.Middleware;
using Ocelot.Cache.Middleware;
using Ocelot.Claims.Middleware;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Provider;
using Ocelot.Configuration.Setter;
using Ocelot.DependencyInjection;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.Errors.Middleware;
using Ocelot.Headers.Middleware;
using Ocelot.Jwt;
using Ocelot.LoadBalancer.Middleware;
using Ocelot.ManualTest.Authentication;
using Ocelot.Middleware;
using Ocelot.QueryStrings.Middleware;
using Ocelot.RateLimit.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester.Middleware;
using Ocelot.RequestId.Middleware;
using Ocelot.Responder.Middleware;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

namespace Ocelot.ManualTest
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("configuration.json")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Action<ConfigurationBuilderCachePart> settings = (x) =>
            {
                x.WithMicrosoftLogging(log =>
                {
                    log.AddConsole(LogLevel.Debug);
                })
                .WithDictionaryHandle();
            };

            services.AddSingleton<TwAuthenticationMiddleware>();

            services.AddOcelot(Configuration, settings);
        }

        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            //app.UseOcelot().Wait();

            await CreateAdministrationArea(app);

            // This is registered to catch any global exceptions that are not handled
            app.UseExceptionHandlerMiddleware();

            // This is registered first so it can catch any errors and issue an appropriate response
            app.UseResponderMiddleware();

            // Initialises downstream request
            app.UseDownstreamRequestInitialiser();

            // Then we get the downstream route information
            app.UseDownstreamRouteFinderMiddleware();

            // We check whether the request is ratelimit, and if there is no continue processing
            app.UseRateLimiting();

            // Now we can look for the requestId
            app.UseRequestIdMiddleware();

            // Now we know where the client is going to go we can authenticate them.
            app.UseAuthenticationMiddleware();

            // The next thing we do is look at any claims transforms in case this is important for authorisation
            app.UseClaimsBuilderMiddleware();

            // Now we have authenticated and done any claims transformation we 
            // can authorise the request
            app.UseAuthorisationMiddleware();

            // Now we can run any header transformation logic
            app.UseHttpRequestHeadersBuilderMiddleware();

            // Now we can run any query string transformation logic
            app.UseQueryStringBuilderMiddleware();

            // Get the load balancer for this request
            app.UseLoadBalancingMiddleware();

            // This takes the downstream route we retrieved earlier and replaces any placeholders with the variables that should be used
            app.UseDownstreamUrlCreatorMiddleware();

            // Not sure if this is the best place for this but we use the downstream url 
            // as the basis for our cache key.
            app.UseOutputCacheMiddleware();

            // Everything should now be ready to build or HttpRequest
            app.UseHttpRequestBuilderMiddleware();
           
            // Convert TempWorks Auth reference token or ServiceRepToken to JWT
            app.UseJwtBuilderMiddleware();

            //We fire off the request and set the response on the scoped data repo
            app.UseHttpRequesterMiddleware();
        }

        private static async Task<IOcelotConfiguration> CreateConfiguration(IApplicationBuilder builder)
        {
            var fileConfig = (IOptions<FileConfiguration>)builder.ApplicationServices.GetService(typeof(IOptions<FileConfiguration>));

            var configSetter = (IFileConfigurationSetter)builder.ApplicationServices.GetService(typeof(IFileConfigurationSetter));

            var configProvider = (IOcelotConfigurationProvider)builder.ApplicationServices.GetService(typeof(IOcelotConfigurationProvider));

            var ocelotConfiguration = await configProvider.Get();

            if (ocelotConfiguration == null || ocelotConfiguration.Data == null || ocelotConfiguration.IsError)
            {
                var config = await configSetter.Set(fileConfig.Value);

                if (config == null || config.IsError)
                {
                    throw new Exception("Unable to start Ocelot: configuration was not set up correctly.");
                }
            }

            ocelotConfiguration = await configProvider.Get();

            if (ocelotConfiguration == null || ocelotConfiguration.Data == null || ocelotConfiguration.IsError)
            {
                throw new Exception("Unable to start Ocelot: ocelot configuration was not returned by provider.");
            }

            return ocelotConfiguration.Data;
        }


        private static async Task CreateAdministrationArea(IApplicationBuilder builder)
        {
            var configuration = await CreateConfiguration(builder);

            var identityServerConfiguration = (IIdentityServerConfiguration)builder.ApplicationServices.GetService(typeof(IIdentityServerConfiguration));

            if (!string.IsNullOrEmpty(configuration.AdministrationPath) && identityServerConfiguration != null)
            {
                var urlFinder = (IBaseUrlFinder)builder.ApplicationServices.GetService(typeof(IBaseUrlFinder));

                var baseSchemeUrlAndPort = urlFinder.Find();

                builder.Map(configuration.AdministrationPath, app =>
                {
                    var identityServerUrl = $"{baseSchemeUrlAndPort}/{configuration.AdministrationPath.Remove(0, 1)}";

                    app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
                    {
                        Authority = identityServerUrl,
                        ApiName = identityServerConfiguration.ApiName,
                        RequireHttpsMetadata = identityServerConfiguration.RequireHttps,
                        AllowedScopes = identityServerConfiguration.AllowedScopes,
                        SupportedTokens = SupportedTokens.Both,
                        ApiSecret = identityServerConfiguration.ApiSecret
                    });

                    app.UseIdentityServer();

                    app.UseMvc();
                });
            }
        }

    }
}

