using GitHubStatusChecksWebApp.Models;

namespace GitHubStatusChecksWebApp.Rules
{
    public interface IRuleFinder
    {
        IStatusCheck GetRuleForCommitContext(CommitStatus commitStatus);
    }
}