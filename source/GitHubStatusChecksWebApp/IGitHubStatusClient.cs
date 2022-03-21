using System.Collections.Generic;
using System.Threading.Tasks;
using GitHubStatusChecksWebApp.Models;
using Octokit;
using CommitStatus = GitHubStatusChecksWebApp.Models.CommitStatus;

namespace GitHubStatusChecksWebApp
{
    public interface IGitHubStatusClient
    {
        string GetGitHubApiToken();

        Task CreateStatusForCommitStateOnPr(
            string owner,
            string repo,
            string commitHash,
            CommitStatus commitStatus,
            CommitState commitState);

        Task<IReadOnlyList<PullRequestFile>> GetFilesForPr(string owner, string repo, PullRequestForCommitHash? pr);
        Task<PullRequestForCommitHash> GetPrForCommitHash(string owner, string repo, string commitHash);
    }
}