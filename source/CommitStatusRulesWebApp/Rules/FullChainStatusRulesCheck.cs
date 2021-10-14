using System.Collections.Generic;
using Octokit;

namespace CommitStatusRulesWebApp.Rules
{
    public class FullChainStatusRulesCheck : IStatusCheck
    {
        public string GetContext() => "Chain: Full build and test and create release \\(Octopus Server .*\\)";
        public bool MatchesRules(IEnumerable<PullRequestFile> files) => true;
    }
}