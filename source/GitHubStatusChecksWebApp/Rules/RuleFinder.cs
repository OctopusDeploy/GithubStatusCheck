using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitHubStatusChecksWebApp.Models;

namespace GitHubStatusChecksWebApp.Rules
{
    public class RuleFinder : IRuleFinder
    {
        private readonly IEnumerable<IStatusCheck> _rules;

        private RuleFinder(IEnumerable<IStatusCheck> rules)
        {
            _rules = rules;
        }

        public IStatusCheck GetRuleForCommitContext(CommitStatus commitStatus)
        {
            var rules = _rules.Where(x => Regex.IsMatch(commitStatus.Context, x.GetContext())).ToList();

            switch (rules.Count)
            {
                case 0:
                    throw new Exception($"No rule found for context {commitStatus.Context}");
                case > 1:
                    throw new Exception(
                        $"Found multiple rules that match the context {commitStatus.Context}: {rules.Select(x => x.GetContext())}");
            }

            var rule = rules.First();
            return rule;
        }

        /// <summary>
        /// This creates a new instance of <c>RuleFinder</c> that has all known rules included.
        /// </summary>
        public static RuleFinder CreateWithKnownRules() => new(KnownRules);

        /// <summary>
        /// This creates a new instance of <c>RuleFinder</c> with a custom collection of rules.
        /// </summary>
        /// <param name="customRules">A non-empty custom collection of rules</param>
        public static RuleFinder CreateWithCustomRules(IEnumerable<IStatusCheck> customRules)
        {
            var rules = customRules as IStatusCheck[] ?? customRules.ToArray();

            if (!rules.Any()) throw new ArgumentException("A non-empty collection of custom rules is required", nameof(customRules));

            return new RuleFinder(rules);
        }

        /// <summary>
        /// This is the collection of known rules to the GitHub status check client.
        /// In future, if we have more rules to use in evaluation, we can add them to this collection.
        /// </summary>
        private static readonly IEnumerable<IStatusCheck> KnownRules = new IStatusCheck[]
        {
            new FullChainStatusRulesCheck(),
            new FrontEndChainStatusRuleChecks()
        };
    }
}