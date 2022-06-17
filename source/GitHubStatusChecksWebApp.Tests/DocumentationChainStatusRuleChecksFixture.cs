using System.Linq;
using GitHubStatusChecksWebApp.Rules;
using NUnit.Framework;
using Octokit;
using Shouldly;

namespace GitHubStatusChecksWebApp.Tests;

[TestFixture]
public class DocumentationChainStatusRuleChecksFixture
{
    [Test]
    [TestCase(new [] {"newportal/jsfile.js", "newportal/readme.md"}, false)]
    [TestCase(new [] {"newportal/readme.md"}, true)]
    [TestCase(new [] {"source/Octopus.Server/csfile.cs", "readme.md"}, false)]
    [TestCase(new [] {"otherfile.ps1"}, false)]
    [TestCase(new [] {"newportal/readme.md", "readme.md"}, true)]
    public void RulesValidate(string[] fileNames, bool shouldMatch)
    {
        var files = fileNames.Select(fileName => new PullRequestFile("", fileName, "", 0, 0, 0, "", "", "", "", "")).ToList();

        var rule = new DocumentationChainStatusRuleChecks();
        rule.MatchesRules(files).ShouldBe(shouldMatch);
    }
}