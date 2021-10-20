using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitHubStatusChecksWebApp.Models;
using GitHubStatusChecksWebApp.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Octokit;
using Serilog;
using Serilog.Core;
using CommitStatus = GitHubStatusChecksWebApp.Models.CommitStatus;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace GitHubStatusChecksWebApp.Controllers
{
    public interface IStatusWebhook
    {
    }
    
    public class Response
    {
        public Response(string message)
        {
            Log.Logger.Information(message);
            Message = message;
        }

        public string Message { get; }
    }

    [ApiController]
    [Route("repos")]
    public class StatusWebhookController : ControllerBase, IStatusWebhook
    {
        private readonly string _context = "Build and tests complete";
        
        private readonly IConfiguration _configuration;

        private readonly IEnumerable<IStatusCheck> _rules;

        public StatusWebhookController(IConfiguration configuration, IEnumerable<IStatusCheck> rules)
        {
            _rules = rules;
            _configuration = configuration;
        }

        [HttpPost("{owner}/{repo}/statuses/{commitHash}")]
        public async Task<Response> Receive(string owner, string repo, string commitHash, [FromBody] CommitStatus commitStatus)
        {
            var githubClient = CreateGitHubClient();
            var commitState = GetCommitState(commitStatus);
            var rule = GetRuleForCommitContext(commitStatus);
            var pr = await GetPrForCommitHash(owner, repo, commitHash);
            var files = await GetFilesForPr(owner, repo, githubClient, pr);

            if (!rule.MatchesRules(files))
                return new Response($"No matching rules for PR {pr.Number} and context {commitStatus.Context}");
            
            if (commitState == CommitState.Pending)
            {
                await AddPendingStatusForContext(owner, repo, commitHash, commitStatus, githubClient);
                return new Response($"Added pending status to PR {pr.Number} from context {commitStatus.Context}");
            }

            await CreateStatusForCommitStateOnPr(owner, repo, commitHash, commitStatus, githubClient, commitState);
            return new Response($"Added {commitState} status to PR {pr.Number} for context {commitStatus.Context}");
        }

        public virtual async Task CreateStatusForCommitStateOnPr(string owner, string repo, string commitHash,
            CommitStatus commitStatus, GitHubClient githubClient, CommitState commitState)
        {
            await githubClient.Repository.Status.Create(owner, repo, commitHash,
                new NewCommitStatus
                    {State = commitState, TargetUrl = commitStatus.Target_Url, Context = _context});
        }

        public virtual async Task<IReadOnlyList<PullRequestFile>> GetFilesForPr(string owner, string repo, GitHubClient? githubClient, PullRequestForCommitHash? pr)
        {
            return await githubClient?.PullRequest.Files(owner, repo, pr.Number);
        }

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

        public virtual async Task AddPendingStatusForContext(string owner, string repo, string commitHash,
            CommitStatus commitStatus, GitHubClient githubClient)
        {
            await githubClient.Repository.Status.Create(owner, repo, commitHash,
                new NewCommitStatus
                    {State = CommitState.Pending, TargetUrl = commitStatus.Target_Url, Context = _context});
        }

        public virtual GitHubClient CreateGitHubClient()
        {
            var github = new GitHubClient(new ProductHeaderValue("OctopusDeployCommitStatusRules"))
            {
                Credentials = new Credentials(_configuration.GetValue<string>("GithubApiToken"))
            };
            return github;
        }

        public virtual async Task<PullRequestForCommitHash> GetPrForCommitHash(string owner, string repo, string commitHash)
        {
            // We're doing this ourself here instead of using OctoKit since it hasn't been updated in 8 months and the
            // PR to add the endpoint for getting pull requested from a commit ID isn't in 0.5.0.
            // https://github.com/octokit/octokit.net/pull/2315
            //
            // Can be replaced with:
            // githubClient.Repository.Commits.Pulls(owner, repo, commitHash) once the next version of OctoKit releases
            
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", _configuration.GetValue<string>("GithubApiToken"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OctopusDeployCommitStatusRules", "1.0.0"));

            var prRequestUrl = $"https://api.github.com/repos/{owner}/{repo}/commits/{commitHash}/pulls";
            var response = await client.GetAsync(prRequestUrl);
            var message = await response.Content.ReadAsStringAsync();
            var prs = JsonConvert.DeserializeObject<List<PullRequestForCommitHash>>(message);

            if (prs == null)
            {
                throw new Exception($"Failed to serialize PRs for request url: {prRequestUrl}");
            }

            var pr = prs.FirstOrDefault();
            if (pr == null)
            {
                throw new Exception($"No Pull Requests found for request url: {prRequestUrl}");
            }

            return pr;
        }

        private CommitState GetCommitState(CommitStatus commitStatus)
        {
            var commitState = commitStatus.State switch
            {
                "pending" => CommitState.Pending,
                "success" => CommitState.Success,
                "failure" => CommitState.Failure,
                _ => CommitState.Error
            };

            return commitState;
        }
    }
}