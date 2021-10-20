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

            Log.Logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .Enrich.With(new PropertyEnricher("Application", appName))
                .WriteTo.Console()
                .WriteTo.Seq(Configuration.GetValue<string>("Seq:Url"), apiKey: Configuration.GetValue<string>("Seq:ApiKey"))
                .CreateLogger();
            
            Log.Logger.Information($"Started Web App: {appName}");
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "GitHubStatusChecksWebApp", Version = "v1"});
            });

            services.AddScoped<IStatusCheck, FrontEndChainStatusRuleChecks>();
            services.AddScoped<IStatusCheck, FullChainStatusRulesCheck>();
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