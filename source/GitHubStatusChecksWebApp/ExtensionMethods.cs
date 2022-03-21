using Octokit;
using CommitStatus = GitHubStatusChecksWebApp.Models.CommitStatus;

namespace GitHubStatusChecksWebApp
{
    public static class ExtensionMethods
    {
        public static CommitState ToCommitState(this CommitStatus commitStatus)
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