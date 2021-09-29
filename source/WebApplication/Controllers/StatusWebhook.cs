using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Octokit;
using CommitStatus = WebApplication.Models.CommitStatus;

namespace WebApplication.Controllers
{
    [ApiController]
    [Route("repos")]
    public class StatusWebhook : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public StatusWebhook(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        [HttpPost("{owner}/{repo}/statuses/{commitHash}")]
        public async Task Receive(string owner, string repo, string commitHash, [FromBody] CommitStatus commitStatus)
        {
            var github = new GitHubClient(new ProductHeaderValue("OctopusDeployCommitStatusRules"))
            {
                Credentials = new Credentials(_configuration.GetValue<string>("GithubApiToken"))
            };

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

            await github.Repository.Status.Create(owner, repo, commitHash,
                new NewCommitStatus {State = commitState, TargetUrl = commitStatus.Target_Url, Context = "Good to Merge"});
        }
    }
}