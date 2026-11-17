namespace NetEvolve.Schemas.Schematron.Expansion.AbstractRules;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>The state of one expansion: the rule catalog, the memoized abstract bodies, and the failure flag.</summary>
internal sealed class ExpansionRun
{
    private readonly Dictionary<string, SchematronRule> _catalog = new(StringComparer.Ordinal);

    // A null body is a failed abstract rule: its problem was reported once, later uses stay silent.
    private readonly Dictionary<string, RuleBody?> _bodies = new(StringComparer.Ordinal);

    private readonly HashSet<string> _visiting = new(StringComparer.Ordinal);
    private readonly IProblemSink _problems;

    internal ExpansionRun(
        IReadOnlyList<SchematronRules> containers,
        IReadOnlyList<SchematronPatternBase> patterns,
        IProblemSink problems
    )
    {
        _problems = problems;
        foreach (var container in containers)
        {
            Catalog(container.Rules);
        }

        foreach (var pattern in patterns)
        {
            Catalog(pattern.Rules);
        }
    }

    internal bool Failed { get; private set; }

    internal SchematronPatternBase ExpandPattern(SchematronPatternBase pattern)
    {
        var changed = false;
        var rules = new List<SchematronRule>(pattern.Rules.Count);
        foreach (var rule in pattern.Rules)
        {
            if (rule.IsAbstract)
            {
                changed = true;
                continue;
            }

            var expanded = ExpandRule(rule);
            changed |= !ReferenceEquals(expanded, rule);
            rules.Add(expanded);
        }

        if (!changed)
        {
            return pattern;
        }

        return pattern is SchematronGroup
            ? new SchematronGroup(
                pattern.Location,
                pattern.Id,
                pattern.IsAbstract,
                pattern.IsA,
                pattern.Documents,
                pattern.Role,
                pattern.Title,
                pattern.Paragraphs,
                pattern.Parameters,
                pattern.Lets,
                rules,
                pattern.ExpandedFrom
            )
            : new SchematronPattern(
                pattern.Location,
                pattern.Id,
                pattern.IsAbstract,
                pattern.IsA,
                pattern.Documents,
                pattern.Role,
                pattern.Title,
                pattern.Paragraphs,
                pattern.Parameters,
                pattern.Lets,
                rules,
                pattern.ExpandedFrom
            );
    }

    private void Catalog(IReadOnlyList<SchematronRule> rules)
    {
        foreach (var rule in rules)
        {
            if (rule.Id is { } id && !_catalog.ContainsKey(id))
            {
                _catalog.Add(id, rule);
            }
        }
    }

    private SchematronRule ExpandRule(SchematronRule rule)
    {
        var body = Merge(rule);
        return body is null || ReferenceEquals(body.Items, rule.Items)
            ? rule
            : new SchematronRule(
                rule.Location,
                rule.Id,
                rule.Context,
                rule.VisitEach,
                rule.Flags,
                rule.Role,
                rule.Subject,
                rule.IsAbstract,
                rule.Title,
                body.Lets,
                body.Items,
                rule.ExpandedFrom
            );
    }

    // The lets and items of the rule with every extends by id replaced by the expanded body of its target.
    // ponytail: recursion depth is the longest extends chain (bounded by the rules in the document); iterate if that proves too deep.
    private RuleBody? Merge(SchematronRule rule)
    {
        if (!HasRuleExtends(rule))
        {
            return new RuleBody(rule.Lets, rule.Items);
        }

        var lets = new List<SchematronLet>();
        var items = new List<SchematronNode>(rule.Items.Count);
        var ok = true;
        foreach (var item in rule.Items)
        {
            if (item is not SchematronExtends { Rule: { } target } extends)
            {
                items.Add(item);
                continue;
            }

            var body = Target(target, extends);
            if (body is null)
            {
                ok = false;
                continue;
            }

            lets.AddRange(body.Lets);
            items.AddRange(body.Items);
        }

        if (!ok)
        {
            Failed = true;
            return null;
        }

        lets.AddRange(rule.Lets);
        return new RuleBody(lets, items);
    }

    private RuleBody? Target(string id, SchematronExtends extends)
    {
        if (!_catalog.TryGetValue(id, out var target))
        {
            Report(
                SchematronDiagnosticCodes.IdrefUnresolved,
                $"The extends element names the rule '{id}', which does not exist.",
                extends
            );
            return null;
        }

        if (!target.IsAbstract)
        {
            Report(
                SchematronDiagnosticCodes.AbstractTargetInvalid,
                $"The extends element names the rule '{id}', which is not abstract.",
                extends
            );
            return null;
        }

        if (_bodies.TryGetValue(id, out var known))
        {
            return known;
        }

        if (!_visiting.Add(id))
        {
            Report(
                SchematronDiagnosticCodes.AbstractTargetInvalid,
                $"The extends element closes a cycle through the abstract rule '{id}'.",
                extends
            );
            return null;
        }

        var body = Merge(target);
        _ = _visiting.Remove(id);
        _bodies[id] = body;
        return body;
    }

    private void Report(DiagnosticCode code, string message, SchematronExtends at)
    {
        Failed = true;
        _problems.Add(new SchematronProblem(code, message, at.Location));
    }

    private static bool HasRuleExtends(SchematronRule rule)
    {
        foreach (var item in rule.Items)
        {
            if (item is SchematronExtends { Rule: not null })
            {
                return true;
            }
        }

        return false;
    }

    private sealed class RuleBody
    {
        internal RuleBody(IReadOnlyList<SchematronLet> lets, IReadOnlyList<SchematronNode> items)
        {
            Lets = lets;
            Items = items;
        }

        internal IReadOnlyList<SchematronLet> Lets { get; }

        internal IReadOnlyList<SchematronNode> Items { get; }
    }
}
