using System.Collections.Generic;
using DotNet.Globbing;
using Octokit;

namespace WebApplication.Rules
{
    public class FrontEndChainStatusRuleChecks : IStatusCheck
    {
        public string GetContext() => "Git Status Test - Frontend Chain \\(Corey .*\\)";
        
        public bool MatchesRules(IEnumerable<PullRequestFile> files)
        {
            var newportalFiles = 0;
            var mdFilesInNewportal = 0;
            var nonNewPortalFiles = 0;
            
            foreach (var file in files)
            {
                if (Glob.Parse("newportal/**/*").IsMatch(file.FileName))
                {
                    //good, we're in new portal
                    newportalFiles++;
                }

                if (Glob.Parse("newportal/**/*.md").IsMatch(file.FileName))
                {
                    //Found a md file, let's record the count
                    mdFilesInNewportal++;
                }

                if (!Glob.Parse("newportal/**/*").IsMatch(file.FileName))
                {
                    nonNewPortalFiles++;
                }
            }

            if (newportalFiles == 0 && mdFilesInNewportal != 0) return false;
            if (newportalFiles > 0 && nonNewPortalFiles == 0) return true;

            return false;
        }
    }
}