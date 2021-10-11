using System.Collections.Generic;
using Octokit;

namespace CommitStatusRulesWebApp.Rules
{
    public interface IStatusCheck
    {
        string GetContext();
        bool MatchesRules(IEnumerable<PullRequestFile> files);
    }
}