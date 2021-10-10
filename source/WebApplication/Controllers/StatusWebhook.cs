using System.Collections.Generic;
using System.Linq;
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
    public class StatusWebhook : ControllerBase, IStatusWebhook
    {
        private readonly string _context = "Good to Merge";
        
        private readonly IConfiguration _configuration;

        private readonly IEnumerable<IStatusCheck> _rules;
        
        public StatusWebhook(IConfiguration configuration, IEnumerable<IStatusCheck> rules)
        {
            _rules = rules;
            _configuration = configuration;

            _rules = new List<IStatusCheck> {new FrontEndChainStatusRuleChecks(), new FullChainStatusRulesCheck()};
        }

        [HttpPost("{owner}/{repo}/statuses/{commitHash}")]
        public async Task Receive(string owner, string repo, string commitHash, [FromBody] CommitStatus commitStatus)
        {
            var github = new GitHubClient(new ProductHeaderValue("OctopusDeployCommitStatusRules"))
            {
                Credentials = new Credentials(_configuration.GetValue<string>("GithubApiToken"))
            };

            var commitState = GetCommitState(commitStatus);

            if (commitState == CommitState.Success)
            {
                var rule = _rules.FirstOrDefault(x => x.GetContext() == commitStatus.Context);
                if (rule != null)
                {
                    var files = await github.PullRequest.Files(owner, repo, 2);

                    if (rule.MatchesRules(files))
                    {
                        await github.Repository.Status.Create(owner, repo, commitHash,
                            new NewCommitStatus {State = CommitState.Success, TargetUrl = commitStatus.Target_Url, Context = _context});
                    }
                }
            }

            var allCurrentStatuses = await github.Repository.Status.GetAll(owner, repo, commitHash);
            if (!allCurrentStatuses.Select(x => x.Context).Contains(_context))
            {
                await github.Repository.Status.Create(owner, repo, commitHash,
                    new NewCommitStatus {State = CommitState.Pending, TargetUrl = commitStatus.Target_Url, Context = _context});    
            }
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