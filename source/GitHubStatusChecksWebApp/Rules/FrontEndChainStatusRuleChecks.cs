using System.Collections.Generic;
using DotNet.Globbing;
using Octokit;

namespace GitHubStatusChecksWebApp.Rules
{
    public class FrontEndChainStatusRuleChecks : IStatusCheck
    {
        public string GetContext() => "Chain: Full build and test frontend \\(Octopus Server .*\\)";
        
        public bool MatchesRules(IEnumerable<PullRequestFile> files)
        {
            var newPortalFiles = 0;
            var mdFilesInNewPortal = 0;
            var nonNewPortalFiles = 0;
            
            foreach (var file in files)
            {
                if (Glob.Parse("newportal/**/*").IsMatch(file.FileName))
                {
                    //good, we're in new portal
                    newPortalFiles++;
                }

                if (Glob.Parse("newportal/**/*.md").IsMatch(file.FileName))
                {
                    //Found a md file, let's record the count
                    mdFilesInNewPortal++;
                }

                if (!Glob.Parse("newportal/**/*").IsMatch(file.FileName))
                {
                    nonNewPortalFiles++;
                }
            }

            if (mdFilesInNewPortal == newPortalFiles) return false;
            if (newPortalFiles > 0 && nonNewPortalFiles == 0) return true;

            return false;
        }
    }
}