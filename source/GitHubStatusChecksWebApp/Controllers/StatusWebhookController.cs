using System.Threading.Tasks;
using GitHubStatusChecksWebApp.Models;
using GitHubStatusChecksWebApp.Rules;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace GitHubStatusChecksWebApp.Controllers
{
    [ApiController]
    [Route("repos")]
    public class StatusWebhookController : ControllerBase
    {
        private readonly IGitHubStatusClient _gitHubStatusClient;
        private readonly IRuleFinder _ruleFinder;

        public StatusWebhookController(IGitHubStatusClient gitHubStatusClient, IRuleFinder ruleFinder)
        {
            _gitHubStatusClient = gitHubStatusClient;
            _ruleFinder = ruleFinder;
        }

        [HttpPost("{owner}/{repo}/statuses/{commitHash}")]
        public async Task Receive(string owner, string repo, string commitHash, [FromBody] CommitStatus commitStatus)
        {
            var commitState = commitStatus.ToCommitState();
            var rule = _ruleFinder.GetRuleForCommitContext(commitStatus);
            var pr = await _gitHubStatusClient.GetPrForCommitHash(owner, repo, commitHash);
            var files = await _gitHubStatusClient.GetFilesForPr(owner, repo, pr);

            if (!rule.MatchesRules(files))
            {
                Log.Logger.Verbose("Files do not match rule {ruleContext} for PR {pr} and context {context}", rule.GetContext(), pr.Number, commitStatus.Context);
                return;
            }

            await _gitHubStatusClient.CreateStatusForCommitStateOnPr(owner, repo, commitHash, commitStatus, commitState);
            Log.Logger.Verbose("Added {commitState} status to PR {pr} from context {context}", commitState, pr.Number, commitStatus.Context);
        }
    }
}