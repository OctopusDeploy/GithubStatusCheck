using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using GitHubStatusChecksWebApp.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Octokit;
using CommitStatus = GitHubStatusChecksWebApp.Models.CommitStatus;
using ProductHeaderValue = Octokit.ProductHeaderValue;
using Serilog;
using Serilog.Context;

namespace GitHubStatusChecksWebApp
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class GitHubStatusClient
    {
        private readonly IConfiguration _configuration;
        private const string Context = "Build and tests complete";

        private readonly GitHubClient _gitHubClient;

        public GitHubStatusClient(IConfiguration configuration)
        {
            _configuration = configuration;
            _gitHubClient = new GitHubClient(new ProductHeaderValue("OctopusDeployCommitStatusRules"))
            {
                // ReSharper disable once VirtualMemberCallInConstructor
                Credentials = new Credentials(GetGitHubApiToken())
            };
        }

        public virtual string GetGitHubApiToken()
        {
            return _configuration.GetValue<string>("GithubApiToken");
        }

        public virtual async Task CreateStatusForCommitStateOnPr(string owner, string repo, string commitHash,
            CommitStatus commitStatus, CommitState commitState)
        {
            await _gitHubClient.Repository.Status.Create(owner, repo, commitHash,
                new NewCommitStatus
                    {State = commitState, TargetUrl = commitStatus.Target_Url, Context = Context});
        }

        public virtual async Task<IReadOnlyList<PullRequestFile>> GetFilesForPr(string owner, string repo, PullRequestForCommitHash? pr)
        {
            return await _gitHubClient.PullRequest.Files(owner, repo, pr.Number);
        }

        public virtual async Task<PullRequestForCommitHash> GetPrForCommitHash(string owner, string repo, string commitHash)
        {
            // We're doing this ourself here instead of using OctoKit since it hasn't been updated in 8 months and the
            // PR to add the endpoint for getting pull requested from a commit ID isn't in 0.5.0.
            // https://github.com/octokit/octokit.net/pull/2315
            //
            // Can be replaced with:
            // githubClient.Repository.Commits.Pulls(owner, repo, commitHash) once the next version of OctoKit releases

            Log.Logger.Information("Querying Github to find PRs for {Owner}/{Repository} for commit {Commit}", owner, repo, commitHash);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _configuration.GetValue<string>("GithubApiToken"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OctopusDeployCommitStatusRules", "1.0.0"));

            var prRequestUrl = $"https://api.github.com/repos/{owner}/{repo}/commits/{commitHash}/pulls";
            var response = await client.GetAsync(prRequestUrl);
            var message = await response.Content.ReadAsStringAsync();
            List<PullRequestForCommitHash>? prs = null;
            using (LogContext.PushProperty("Json", message))
            {
                try
                {
                    prs = JsonConvert.DeserializeObject<List<PullRequestForCommitHash>>(message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to deserialize response from GitHub");
                    throw;
                }
                if (prs == null)
                {
                    throw new Exception($"Failed to serialize PRs for request url: {prRequestUrl}");
                }
            }
            Log.Logger.Information("Github found PRs {PullRequests} in {Owner}/{Repository} for commit {Commit}",
                prs.Select(x => x.Number), owner, repo, commitHash);

            var pr = prs.FirstOrDefault();
            if (pr == null)
            {
                throw new Exception($"No Pull Requests found for request url: {prRequestUrl}");
            }

            return pr;
        }

        public static CommitState GetCommitState(CommitStatus commitStatus)
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
