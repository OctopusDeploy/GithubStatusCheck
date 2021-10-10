﻿using System.Collections.Generic;
using Octokit;

namespace WebApplication.Rules
{
    public class FullChainStatusRulesCheck : IStatusCheck
    {
        public string GetContext() => "Git Status Test - Full Chain (Corey)";
        public bool MatchesRules(IEnumerable<PullRequestFile> files) => true;
    }
}