using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace WebApplication
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
                return;
            }

            var headerApiKey = extractedApiKey.First();
            headerApiKey = Encoding.UTF8.GetString(Convert.FromBase64String(headerApiKey.Replace("Basic ", "")));
            headerApiKey = headerApiKey.Replace(":x-oauth-basic", "");
            var applicationApiKey = configuration.GetValue<string>("ApiAuthenticationKey");

            if (headerApiKey != applicationApiKey)
            {
                return;
            }
            
            await _next(context);
        }
    }
}