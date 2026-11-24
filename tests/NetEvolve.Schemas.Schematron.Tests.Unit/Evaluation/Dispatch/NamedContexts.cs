namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Dispatch;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Gives the scripted contexts of a harness the name tests a real pattern matcher reports: <c>name</c> and <c>@name</c> list their
/// expanded name, every other context lists none. The matching itself stays with the scripted matcher.
/// </summary>
internal sealed class NamedContexts(ScriptedMatcher inner) : IPatternMatcher
{
    /// <summary>Wraps the contexts of the sets and returns an evaluator that matches them with this matcher.</summary>
    internal static PatternEvaluator Evaluator(PatternHarness harness, out List<CompiledRuleSet> sets)
    {
        var matcher = new NamedContexts(harness.Matcher);
        sets = [];
        foreach (var set in harness.Sets)
        {
            var rules = new CompiledRule[set.Rules.Length];
            for (var i = 0; i < rules.Length; i++)
            {
                var rule = set.Rules[i];
                rules[i] = new CompiledRule(
                    rule.Scope,
                    new NamedPattern(rule.Context, NamesOf(rule.ContextText)),
                    rule.Id,
                    rule.ContextText,
                    rule.Assertions,
                    rule.Annotations
                );
            }

            sets.Add(
                new CompiledRuleSet(set.Scope, set.Id, set.Documents, set.Name, set.IsGroup, rules, set.Annotations)
            );
        }

        return new PatternEvaluator(
            matcher,
            harness.Run,
            harness.Binder,
            harness.Assertions,
            harness.Mapper,
            harness.Problems,
            CancellationToken.None
        );
    }

    public bool TryCompile(
        ReadOnlySpan<char> pattern,
        PatternUse use,
        QueryStaticScope scope,
        out CompiledPattern compiled,
        out SchematronProblem problem
    ) => inner.TryCompile(pattern, use, scope, out compiled, out problem);

    public QueryStatus Matches(CompiledPattern compiled, IQueryRun run, in XPathNode candidate, out bool matched) =>
        inner.Matches(((NamedPattern)compiled).Inner, run, in candidate, out matched);

    private static XPathQName[] NamesOf(string context)
    {
        var name = context is ['@', ..] ? context.Substring(1) : context;
        return name is "*" or "/" or "@*" or "text()" or "node()" or "" ? [] : [new XPathQName(string.Empty, name)];
    }

    private sealed class NamedPattern(CompiledPattern inner, XPathQName[] names)
        : CompiledPattern(inner.NodeKinds, names)
    {
        internal CompiledPattern Inner { get; } = inner;

        internal override ReadOnlySpan<int> ReferencedSlots => Inner.ReferencedSlots;
    }
}
