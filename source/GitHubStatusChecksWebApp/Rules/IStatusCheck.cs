using System.Collections.Generic;
using Octokit;

namespace GitHubStatusChecksWebApp.Rules
{
    public interface IStatusCheck
    {
        string GetContext();
        bool MatchesRules(IEnumerable<PullRequestFile> files);
    }
}