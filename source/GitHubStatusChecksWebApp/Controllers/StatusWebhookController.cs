using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitHubStatusChecksWebApp.Models;
using GitHubStatusChecksWebApp.Responses;
using GitHubStatusChecksWebApp.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Octokit;
using Serilog.Core;
using CommitStatus = GitHubStatusChecksWebApp.Models.CommitStatus;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace GitHubStatusChecksWebApp.Controllers
{
    [ApiController]
    [Route("repos")]
    public class StatusWebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        private readonly IEnumerable<IStatusCheck> _rules;
        private readonly GitHubStatusClient _gitHubStatusClient;

        public StatusWebhookController(IConfiguration configuration, IEnumerable<IStatusCheck> rules, GitHubStatusClient gitHubStatusClient)
        {
            _rules = rules;
            _gitHubStatusClient = gitHubStatusClient;
            _configuration = configuration;
        }

        [HttpPost("{owner}/{repo}/statuses/{commitHash}")]
        public async Task<StatusWebhookControllerResponse> Receive(string owner, string repo, string commitHash, [FromBody] CommitStatus commitStatus)
        {
            var commitState = GitHubStatusClient.GetCommitState(commitStatus);
            var rule = GetRuleForCommitContext(commitStatus);
            var pr = await _gitHubStatusClient.GetPrForCommitHash(owner, repo, commitHash);
            var files = await _gitHubStatusClient.GetFilesForPr(owner, repo, pr);

            if (!rule.MatchesRules(files))
                return new StatusWebhookControllerResponse($"No matching rules for PR {pr.Number} and context {commitStatus.Context}");
            
            if (commitState == CommitState.Pending)
            {
                await _gitHubStatusClient.AddPendingStatusForContext(owner, repo, commitHash, commitStatus);
                return new StatusWebhookControllerResponse($"Added pending status to PR {pr.Number} from context {commitStatus.Context}");
            }

            await _gitHubStatusClient.CreateStatusForCommitStateOnPr(owner, repo, commitHash, commitStatus, commitState);
            return new StatusWebhookControllerResponse($"Added {commitState} status to PR {pr.Number} for context {commitStatus.Context}");
        }

        [NonAction]
        public IStatusCheck GetRuleForCommitContext(CommitStatus commitStatus)
        {
            var rules = _rules.Where(x => Regex.IsMatch(commitStatus.Context, x.GetContext())).ToList();

            switch (rules.Count)
            {
                case 0:
                    throw new Exception($"No rule found for context {commitStatus.Context}");
                case > 1:
                    throw new Exception(
                        $"Found multiple rules that match the context {commitStatus.Context}: {rules.Select(x => x.GetContext())}");
            }

            var rule = rules.First();
            return rule;
        }
    }
}