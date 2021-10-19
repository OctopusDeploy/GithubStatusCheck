using System;
using GitHubStatusChecksWebApp.Controllers;
using GitHubStatusChecksWebApp.Models;
using GitHubStatusChecksWebApp.Rules;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace GitHubStatusChecksWebApp.Tests
{
    public class StatusWebhookControllerFixture
    {
        private StatusWebhookController _controller;

        [SetUp]
        public void Setup()
        {
            _controller = new StatusWebhookController(new Mock<IConfiguration>().Object, new IStatusCheck []
            {
                new FullChainStatusRulesCheck(),
                new FrontEndChainStatusRuleChecks()
            });
        }
        
        [Test]
        public void GetWithValidFrontendContextReturnsFrontendRule()
        {
            _controller.GetRuleForCommitContext(new CommitStatus
            {
                Context = "Chain: Full build and test frontend (Frontend)"
            }).ShouldNotBeNull().ShouldBeEquivalentTo(new FrontEndChainStatusRuleChecks());
        }

        [Test]
        public void GetWithValidFullBuildChainFullVNextContext()
        {
            _controller.GetRuleForCommitContext(new CommitStatus
            {
                Context = "Chain: Full build and test and create release (Octopus Server vNext)"
            }).ShouldNotBeNull().ShouldBeEquivalentTo(new FullChainStatusRulesCheck());
        }
        
        [Test]
        public void GetWithValidFullBuildChainFullReleaseBranchContext()
        {
            _controller.GetRuleForCommitContext(new CommitStatus
            {
                Context = "Chain: Full build and test and create release (Octopus Server 2021.2)"
            }).ShouldNotBeNull().ShouldBeEquivalentTo(new FullChainStatusRulesCheck());
        }

        [Test]
        public void GetWithInvalidContextShouldThrowException()
        {
            Should.Throw<Exception>(() =>
            {
                _controller.GetRuleForCommitContext(new CommitStatus()
                {
                    Context = "Invalid context"
                });
            });
        }
    }
}