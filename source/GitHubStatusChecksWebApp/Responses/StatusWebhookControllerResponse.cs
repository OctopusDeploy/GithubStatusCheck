using Serilog;

namespace GitHubStatusChecksWebApp.Responses
{
    public class StatusWebhookControllerResponse
    {
        public StatusWebhookControllerResponse(string message)
        {
            Log.Logger.Information(message);
            Message = message;
        }

        public string Message { get; }
    }
}