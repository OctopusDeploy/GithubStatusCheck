using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Octokit;

namespace CommitStatusRulesWebApp.Models
{
    public class PullRequestForCommitHash : PullRequest
    {
        [JsonConstructor]
        public PullRequestForCommitHash(long id, string nodeId, string url, string htmlUrl, string diffUrl, string patchUrl,
            string issueUrl, string statusesUrl, int number, ItemState state, string title, string body,
            DateTimeOffset createdAt, DateTimeOffset updatedAt, DateTimeOffset? closedAt, DateTimeOffset? mergedAt,
            GitReference head, GitReference @base, User user, User assignee, IReadOnlyList<User> assignees,
            bool draft, bool? mergeable, MergeableState? mergeableState, User mergedBy, string mergeCommitSha,
            int comments, int commits, int additions, int deletions, int changedFiles, Milestone milestone,
            bool locked, bool? maintainerCanModify, IReadOnlyList<User> requestedReviewers,
            IReadOnlyList<Team> requestedTeams, IReadOnlyList<Label> labels) : base(id, nodeId, url, htmlUrl, diffUrl, patchUrl, issueUrl, statusesUrl, number, state, title, body, createdAt, updatedAt, closedAt, mergedAt, head, @base, user, assignee, assignees, draft, mergeable, mergeableState, mergedBy, mergeCommitSha, comments, commits, additions, deletions, changedFiles, milestone, locked, maintainerCanModify, requestedReviewers, requestedTeams, labels)
        {
            // Only exists so we can have the [JsonConstructor]
        }
    }
}