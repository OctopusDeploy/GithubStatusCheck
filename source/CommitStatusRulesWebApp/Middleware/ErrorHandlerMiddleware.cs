using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;

namespace CommitStatusRulesWebApp.Middleware
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, e.Message);
            }
        }
    }
}