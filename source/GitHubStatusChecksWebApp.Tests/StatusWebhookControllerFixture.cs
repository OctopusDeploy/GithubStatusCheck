using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitHubStatusChecksWebApp.Controllers;
using GitHubStatusChecksWebApp.Models;
using GitHubStatusChecksWebApp.Rules;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Octokit;
using Shouldly;
using CommitStatus = GitHubStatusChecksWebApp.Models.CommitStatus;

namespace GitHubStatusChecksWebApp.Tests
{
    [TestFixture]
    public class StatusWebhookControllerFixture
    {
        private Mock<StatusWebhookController> _controller;
        [SetUp]
        public void Setup()
        {
            _controller = new Mock<StatusWebhookController>(new Mock<IConfiguration>().Object, new IStatusCheck[]
            {
                new FullChainStatusRulesCheck(),
                new FrontEndChainStatusRuleChecks()
            });  
            
            //Mock methods that contact GitHub API
            _controller.Setup(c => c.CreateGitHubClient())
                .Returns(new GitHubClient(new ProductHeaderValue("GitHubStatusChecksTest")));
            
            _controller.Setup(c => c.AddPendingStatusForContext(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CommitStatus>(),
                    It.IsAny<GitHubClient>()))
                .Returns(() => Task.CompletedTask);
            
            _controller.Setup(c => c.CreateStatusForCommitStateOnPr(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CommitStatus>(),
                    It.IsAny<GitHubClient>(),
                    It.IsAny<CommitState>()))
                .Returns(Task.CompletedTask);
            
            _controller.Setup(c => c.GetPrForCommitHash(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).Returns(Task.FromResult(new PullRequestForCommitHash(0, "", "", "", "", "", "", "", 1, ItemState.Open, "", "", DateTimeOffset.Now, DateTimeOffset.Now, null, null, new GitReference(), new GitReference(), new User(), new User(), new List<User>(), true, false, MergeableState.Clean, new User(), "", 0, 1, 1, 0, 1, new Milestone(), false, null, new List<User>(), new List<Team>(), new List<Label>())));
        }
        
        [Test]
        public async Task FrontendContextInPendingStateWithFrontendFilesCreatesPendingStatus()
        {
            _controller.Setup(c => c.GetFilesForPr(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<GitHubClient>(),
                    It.IsAny<PullRequestForCommitHash>()))
                .Returns(Task.FromResult(new List<PullRequestFile>()
                {
                    new("", "newportal/testfile.js", "", 0, 0, 0, "", "", "", "", ""),
                } as IReadOnlyList<PullRequestFile>));

            var output = await _controller.Object.Receive("octopusdeploy", "octopusdeploy", "hash", new CommitStatus()
            {
                Context = "Chain: Full build and test frontend (Frontend)",
                State = "pending"
            });

            output.Message.ShouldContain("Added pending status to PR");
        }

        [Test]
        public async Task FrontendContextInPendingStateWithServerFilesDoesNotCreateStatus()
        {
            _controller.Setup(c => c.GetFilesForPr(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<GitHubClient>(),
                    It.IsAny<PullRequestForCommitHash>()))
                .Returns(Task.FromResult(new List<PullRequestFile>()
                {
                    new("", "source/testfile.cs", "", 0, 0, 0, "", "", "", "", ""),
                } as IReadOnlyList<PullRequestFile>));

            var output = await _controller.Object.Receive("octopusdeploy", "octopusdeploy", "hash", new CommitStatus()
            {
                Context = "Chain: Full build and test frontend (Frontend)",
                State = "pending"
            });

            output.Message.ShouldContain("No matching rules for PR");
        }

        [Test]
        public async Task FrontendContextInErrorStatusWithFrontendFilesCreatesErrorStatus()
        {
            _controller.Setup(c => c.GetFilesForPr(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<GitHubClient>(),
                    It.IsAny<PullRequestForCommitHash>()))
                .Returns(Task.FromResult(new List<PullRequestFile>()
                {
                    new("", "newportal/testfile.js", "", 0, 0, 0, "", "", "", "", ""),
                } as IReadOnlyList<PullRequestFile>));

            var output = await _controller.Object.Receive("octopusdeploy", "octopusdeploy", "hash", new CommitStatus()
            {
                Context = "Chain: Full build and test frontend (Frontend)",
                State = "error"
            });

            output.Message.ShouldContain("Added error status to PR");
        }
        
        [Test]
        public async Task FrontendContextInSuccessStatusWithFrontendFilesCreatesSuccessStatus()
        {
            _controller.Setup(c => c.GetFilesForPr(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<GitHubClient>(),
                    It.IsAny<PullRequestForCommitHash>()))
                .Returns(Task.FromResult(new List<PullRequestFile>()
                {
                    new("", "newportal/testfile.js", "", 0, 0, 0, "", "", "", "", ""),
                } as IReadOnlyList<PullRequestFile>));

            var output = await _controller.Object.Receive("octopusdeploy", "octopusdeploy", "hash", new CommitStatus()
            {
                Context = "Chain: Full build and test frontend (Frontend)",
                State = "success"
            });

            output.Message.ShouldContain("Added success status to PR");
        }
    }
}