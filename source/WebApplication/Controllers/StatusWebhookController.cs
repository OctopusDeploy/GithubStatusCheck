using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Octokit;
using WebApplication.Rules;
using CommitStatus = WebApplication.Models.CommitStatus;

namespace WebApplication.Controllers
{
    public interface IStatusWebhook
    {
    }

    [ApiController]
    [Route("repos")]
    public class StatusWebhookController : ControllerBase, IStatusWebhook
    {
        private readonly string _context = "Good to Merge";
        
        private readonly IConfiguration _configuration;

        private readonly IEnumerable<IStatusCheck> _rules;
        
        public StatusWebhookController(IConfiguration configuration)
        {
            _configuration = configuration;

            _rules = new List<IStatusCheck>
            {
                new FrontEndChainStatusRuleChecks(), 
                new FullChainStatusRulesCheck()
            };
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

                var files = await githubClient.PullRequest.Files(owner, repo, 2);

                if (rule.MatchesRules(files))
                {
                    await githubClient.Repository.Status.Create(owner, repo, commitHash,
                        new NewCommitStatus
                            {State = commitState, TargetUrl = commitStatus.Target_Url, Context = _context});
                }
            }
        }

        private async Task CreateContextStatusIfNotExisting(string owner, string repo, string commitHash,
            CommitStatus commitStatus, GitHubClient githubClient)
        {
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