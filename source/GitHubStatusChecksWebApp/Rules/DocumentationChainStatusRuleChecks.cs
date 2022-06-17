using System.Collections.Generic;
using System.Linq;
using DotNet.Globbing;
using Octokit;

namespace GitHubStatusChecksWebApp.Rules;

public class DocumentationChainStatusRuleChecks : IStatusCheck
{
    public string GetContext() => "Chain: Documentation";
        
    public bool MatchesRules(IEnumerable<PullRequestFile> files)
    {
        var glob = Glob.Parse("**/*.md");

        return files.All(file => glob.IsMatch(file.FileName));
    }
}