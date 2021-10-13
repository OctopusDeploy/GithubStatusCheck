using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CommitStatusRulesWebApp.Middleware
{
    public class AuthTokenMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
        {
            if (!context.Request.Headers.TryGetValue("Authorization", out var extractedApiKey))
            {
                throw new Exception("Did not find Authorization Header");
            }

            var headerApiKey = extractedApiKey.First();
            headerApiKey = Encoding.UTF8.GetString(Convert.FromBase64String(headerApiKey.Replace("Basic ", "")));
            headerApiKey = headerApiKey.Replace(":x-oauth-basic", "");
            var applicationApiKey = configuration.GetValue<string>("ApiAuthenticationKey");

            if (headerApiKey != applicationApiKey)
            {
                throw new Exception("Invalid Authentication Token");
            }
            
            await _next(context);
        }
    }
}