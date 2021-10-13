using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommitStatusRulesWebApp.Models;
using CommitStatusRulesWebApp.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Octokit;
using CommitStatus = CommitStatusRulesWebApp.Models.CommitStatus;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace CommitStatusRulesWebApp.Controllers
{
    public interface IStatusWebhook
    {
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
        public async Task Receive(string owner, string repo, string commitHash, [FromBody] CommitStatus commitStatus)
        {
            var githubClient = CreateGitHubClient();

            var commitState = GetCommitState(commitStatus);

            if (commitState == CommitState.Pending)
            {
                await CreateContextStatusIfNotExisting(owner, repo, commitHash, commitStatus, githubClient);
            }
            else
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

                var pr = await GetPrForCommitHash(owner, repo, commitHash);
                var files = await githubClient.PullRequest.Files(owner, repo, pr.Number);

                if (rule.MatchesRules(files))
                {
                    await githubClient.Repository.Status.Create(owner, repo, commitHash,
                        new NewCommitStatus
                            {State = commitState, TargetUrl = commitStatus.Target_Url, Context = _context});
                }
            }
        }

        private async Task<PullRequest> GetPullRequestForCommitHash(string owner, string repo, string commitHash)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("token", _configuration.GetValue<string>("GithubApiToken"));

            var pr = await client.GetAsync($"https://api.github.com/repos/{owner}/{repo}/commits/{commitHash}/pulls");

            return new PullRequest();
        }

        private async Task CreateContextStatusIfNotExisting(string owner, string repo, string commitHash,
            CommitStatus commitStatus, GitHubClient githubClient)
        {
            //TODO: How do we handle re-runs?
            var allCurrentStatuses = await githubClient.Repository.Status.GetAll(owner, repo, commitHash);
            if (!allCurrentStatuses.Select(x => x.Context).Contains(_context))
            {
                await githubClient.Repository.Status.Create(owner, repo, commitHash,
                    new NewCommitStatus
                        {State = CommitState.Pending, TargetUrl = commitStatus.Target_Url, Context = _context});
            }
        }

        private GitHubClient CreateGitHubClient()
        {
            var github = new GitHubClient(new ProductHeaderValue("OctopusDeployCommitStatusRules"))
            {
                Credentials = new Credentials(_configuration.GetValue<string>("GithubApiToken"))
            };
            return github;
        }

        private async Task<PullRequestForCommitHash> GetPrForCommitHash(string owner, string repo, string commitHash)
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

            var response = await client.GetAsync($"https://api.github.com/repos/{owner}/{repo}/commits/{commitHash}/pulls");
            var message = await response.Content.ReadAsStringAsync();
            var prs = JsonConvert.DeserializeObject<IEnumerable<PullRequestForCommitHash>>(message);

            return prs?.FirstOrDefault();
        }

        private static CommitState GetCommitState(CommitStatus commitStatus)
        {
            CommitState commitState;
            switch (commitStatus.State)
            {
                case "pending":
                    commitState = CommitState.Pending;
                    break;
                case "success":
                    commitState = CommitState.Success;
                    break;
                case "failure":
                    commitState = CommitState.Failure;
                    break;
                case "error":
                default:
                    commitState = CommitState.Error;
                    break;
            }

            return commitState;
        }
    }
}