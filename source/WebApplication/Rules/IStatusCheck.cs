using System.Collections.Generic;
using Octokit;

namespace WebApplication.Rules
{
    public interface IStatusCheck
    {
        string GetContext();
        bool MatchesRules(IEnumerable<PullRequestFile> files);
    }
}