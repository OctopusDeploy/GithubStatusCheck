﻿using System;
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
        private StatusWebhookController _controller;
        private Mock<GitHubStatusClient> _githubStatusClient;

        [SetUp]
        public void Setup()
        {
            var configuration = new Mock<IConfiguration>().Object;
            
            //Mock methods that contact GitHub API
            _githubStatusClient = new Mock<GitHubStatusClient>(configuration);
            
            _githubStatusClient.Setup(c => c.GetGitHubApiToken())
                .Returns("token");

            _githubStatusClient.Setup(c => c.AddPendingStatusForContext(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CommitStatus>()))
                .Returns(() => Task.CompletedTask);
            
            _githubStatusClient.Setup(c => c.CreateStatusForCommitStateOnPr(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CommitStatus>(),
                    It.IsAny<CommitState>()))
                .Returns(Task.CompletedTask);
            
            _githubStatusClient.Setup(c => c.GetPrForCommitHash(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).Returns(Task.FromResult(new PullRequestForCommitHash(0, "", "", "", "", "", "", "", 1, ItemState.Open, "", "", DateTimeOffset.Now, DateTimeOffset.Now, null, null, new GitReference(), new GitReference(), new User(), new User(), new List<User>(), true, false, MergeableState.Clean, new User(), "", 0, 1, 1, 0, 1, new Milestone(), false, null, new List<User>(), new List<Team>(), new List<Label>())));
            
            _controller = new StatusWebhookController(configuration, new IStatusCheck[]
            {
                new FullChainStatusRulesCheck(),
                new FrontEndChainStatusRuleChecks()
            }, _githubStatusClient.Object);  
        }
        
        [Test]
        public async Task FrontendContextInPendingStateWithFrontendFilesCreatesPendingStatus()
        {
            _githubStatusClient.Setup(c => c.GetFilesForPr(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<PullRequestForCommitHash>()))
                .Returns(Task.FromResult(new List<PullRequestFile>()
                {
                    new("", "newportal/testfile.js", "", 0, 0, 0, "", "", "", "", ""),
                } as IReadOnlyList<PullRequestFile>));

            var output = await _controller.Receive("octopusdeploy", "octopusdeploy", "hash", new CommitStatus()
            {
                Context = "Chain: Full build and test frontend (Frontend)",
                State = "pending"
            });

            output.Message.ShouldStartWith("Added pending status to PR");
        }

        [Test]
        public async Task FrontendContextInPendingStateWithServerFilesDoesNotCreateStatus()
        {
            _githubStatusClient.Setup(c => c.GetFilesForPr(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<PullRequestForCommitHash>()))
                .Returns(Task.FromResult(new List<PullRequestFile>()
                {
                    new("", "source/testfile.cs", "", 0, 0, 0, "", "", "", "", ""),
                } as IReadOnlyList<PullRequestFile>));

            var output = await _controller.Receive("octopusdeploy", "octopusdeploy", "hash", new CommitStatus()
            {
                Context = "Chain: Full build and test frontend (Frontend)",
                State = "pending"
            });

            output.Message.ShouldStartWith("Files do not match rule");
        }

        [Test]
        public async Task FrontendContextInErrorStatusWithFrontendFilesCreatesErrorStatus()
        {
            _githubStatusClient.Setup(c => c.GetFilesForPr(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<PullRequestForCommitHash>()))
                .Returns(Task.FromResult(new List<PullRequestFile>()
                {
                    new("", "newportal/testfile.js", "", 0, 0, 0, "", "", "", "", ""),
                } as IReadOnlyList<PullRequestFile>));

            var output = await _controller.Receive("octopusdeploy", "octopusdeploy", "hash", new CommitStatus()
            {
                Context = "Chain: Full build and test frontend (Frontend)",
                State = "error"
            });

            output.Message.ShouldStartWith("Added error status to PR");
        }
        
        [Test]
        public async Task FrontendContextInSuccessStatusWithFrontendFilesCreatesSuccessStatus()
        {
            _githubStatusClient.Setup(c => c.GetFilesForPr(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<PullRequestForCommitHash>()))
                .Returns(Task.FromResult(new List<PullRequestFile>()
                {
                    new("", "newportal/testfile.js", "", 0, 0, 0, "", "", "", "", ""),
                } as IReadOnlyList<PullRequestFile>));

            var output = await _controller.Receive("octopusdeploy", "octopusdeploy", "hash", new CommitStatus()
            {
                Context = "Chain: Full build and test frontend (Frontend)",
                State = "success"
            });

            output.Message.ShouldStartWith("Added success status to PR");
        }

        [Test]
        public async Task FrontendAndFullContextForServerFilesCreatesStatusOnlyForFullContext()
        {
            _githubStatusClient.Setup(c => c.GetFilesForPr(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<PullRequestForCommitHash>()))
                .Returns(Task.FromResult(new List<PullRequestFile>()
                {
                    new("", "source/testfile.cs", "", 0, 0, 0, "", "", "", "", ""),
                } as IReadOnlyList<PullRequestFile>));

            var output = await _controller.Receive("octopusdeploy", "octopusdeploy", "hash", new CommitStatus()
            {
                Context = "Chain: Full build and test frontend (Frontend)",
                State = "success"
            });
            
            output.Message.ShouldStartWith("Files do not match rule");
            
            output = await _controller.Receive("octopusdeploy", "octopusdeploy", "hash", new CommitStatus()
            {
                Context = "Chain: Full build and test and create release (Octopus Server vNext)",
                State = "pending"
            });

            output.Message.ShouldStartWith("Added pending status to PR");
        }
    }
}