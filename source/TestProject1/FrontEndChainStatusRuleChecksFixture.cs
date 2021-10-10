using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Octokit;
using Shouldly;
using WebApplication.Rules;

namespace TestProject1
{
    public class FrontEndChainStatusRuleChecksFixture
    {
        
        [Test]
        [TestCase(new [] {"newportal/jsfile.js", "newportal/readme.md"}, true)]
        [TestCase(new [] {"newportal/someotherfolder/jsfile.js"}, true)]
        [TestCase(new [] {"newportal/jsfile.js", "newportal/readme.md", "otherfile.ps1"}, false)]
        [TestCase(new [] {"otherfile.ps1"}, false)]
        [TestCase(new [] {"newportal/readme.md"}, false)]
        public void RulesValidate(string[] fileNames, bool shouldMatch)
        {
            var files = fileNames.Select(fileName => new PullRequestFile("", fileName, "", 0, 0, 0, "", "", "", "", "")).ToList();

            var rule = new FrontEndChainStatusRuleChecks();
            rule.MatchesRules(files).ShouldBe(shouldMatch);
        }
    }
}