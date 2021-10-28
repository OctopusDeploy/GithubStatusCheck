using System;
using System.Linq;
using GitHubStatusChecksWebApp.Models;
using GitHubStatusChecksWebApp.Rules;
using NUnit.Framework;
using Shouldly;

namespace GitHubStatusChecksWebApp.Tests
{
    [TestFixture]
    public class RuleFinderFixture
    {
        private readonly IRuleFinder _subject = RuleFinder.CreateWithKnownRules();

        [Test]
        public void GetWithValidFrontendContextReturnsFrontendRule()
        {
            var result = _subject.GetRuleForCommitContext(new CommitStatus
            {
                Context = "Chain: Full build and test frontend (Frontend)"
            });

            result.ShouldNotBeNull().ShouldBeEquivalentTo(new FrontEndChainStatusRuleChecks());
        }

        [Test]
        public void GetWithValidFullBuildChainFullVNextContext()
        {
            var result = _subject.GetRuleForCommitContext(new CommitStatus
            {
                Context = "Chain: Full build and test and create release (Octopus Server vNext)"
            });

            result.ShouldNotBeNull().ShouldBeEquivalentTo(new FullChainStatusRulesCheck());
        }

        [Test]
        public void GetWithValidFullBuildChainFullReleaseBranchContext()
        {
            var result = _subject.GetRuleForCommitContext(new CommitStatus
            {
                Context = "Chain: Full build and test and create release (Octopus Server 2021.2)"
            });

            result.ShouldNotBeNull().ShouldBeEquivalentTo(new FullChainStatusRulesCheck());
        }

        [Test]
        public void GetWithInvalidContextShouldThrowException()
        {
            Should.Throw<Exception>(() =>
            {
                _subject.GetRuleForCommitContext(new CommitStatus
                {
                    Context = "Invalid context"
                });
            });
        }

        [Test]
        public void WhenCustomRulesIsNull_CreateWithCustomRules_ThrowsArgumentException()
        {
            Should.Throw<ArgumentException>(() => { RuleFinder.CreateWithCustomRules(null!); });
        }

        [Test]
        public void WhenCustomRulesIsEmpty_CreateWithCustomRules_ThrowsArgumentException()
        {
            Should.Throw<ArgumentException>(() => { RuleFinder.CreateWithCustomRules(Enumerable.Empty<IStatusCheck>()); });
        }
    }
}