using System.Reflection;
using GitHubStatusChecksWebApp.Middleware;
using GitHubStatusChecksWebApp.Rules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Serilog.Exceptions;

namespace GitHubStatusChecksWebApp
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var appName = Assembly.GetAssembly(typeof(Startup))?.GetName().Name;
            var loggingLevelSwitch = new LoggingLevelSwitch();

            Log.Logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .Enrich.With(new PropertyEnricher("Application", appName))
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Seq(
                    serverUrl: Configuration.GetValue<string>("Seq:Url"),
                    apiKey: Configuration.GetValue<string>("Seq:ApiKey"),
                    controlLevelSwitch: loggingLevelSwitch)
                .CreateLogger();

            Log.Logger.Information("Started Web App: {AppName}", appName);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "GitHubStatusChecksWebApp", Version = "v1"});
            });

            services.AddScoped<IStatusCheck, DocumentationChainStatusRuleChecks>();
            services.AddScoped<IStatusCheck, FrontEndChainStatusRuleChecks>();
            services.AddScoped<IStatusCheck, FullChainStatusRulesCheck>();
            services.AddScoped<GitHubStatusClient>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "GitHubStatusChecksWebApp v1"));
            }

            app.UseMiddleware<ErrorHandlerMiddleware>();
            app.UseMiddleware<AuthTokenMiddleware>();
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
