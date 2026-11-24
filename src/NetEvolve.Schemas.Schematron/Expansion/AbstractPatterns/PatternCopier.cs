namespace NetEvolve.Schemas.Schematron.Expansion.AbstractPatterns;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>
/// Copies the content of an abstract pattern with the parameters substituted, and nowhere else than in the rule <c>context</c>,
/// the <c>test</c> of an assertion, the <see langword="value"/> of a variable, the <see langword="select"/> of a <c>value-of</c>, and the <see langword="path"/> of a <c>name</c>. Unchanged nodes
/// are shared, not cloned: the model is immutable and a copy keeps the location of its definition.
/// </summary>
internal sealed class PatternCopier
{
    private readonly SubstitutionTable _table;
    private readonly DocumentLocation _site;

    internal PatternCopier(SubstitutionTable table, DocumentLocation site)
    {
        ArgumentNullException.ThrowIfNull(table);

        _table = table;
        _site = site;
    }

    internal IReadOnlyList<SchematronLet> Lets(IReadOnlyList<SchematronLet> lets)
    {
        ArgumentNullException.ThrowIfNull(lets);

        var result = new SchematronLet[lets.Count];
        for (var i = 0; i < result.Length; i++)
        {
            var let = lets[i];
            var value = _table.Apply(let.Value);
            result[i] = ReferenceEquals(value, let.Value)
                ? let
                : new SchematronLet(let.Location, let.Name, let.DeclaredType, value);
        }

        return result;
    }

    internal IReadOnlyList<SchematronRule> Rules(IReadOnlyList<SchematronRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var result = new SchematronRule[rules.Count];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Rule(rules[i]);
        }

        return result;
    }

    private SchematronRule Rule(SchematronRule rule)
    {
        var items = new SchematronNode[rule.Items.Count];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = Item(rule.Items[i]);
        }

        return new SchematronRule(
            rule.Location,
            rule.Id,
            rule.Context is null ? null : _table.Apply(rule.Context),
            rule.VisitEach,
            rule.Flags,
            rule.Role,
            rule.Subject,
            rule.IsAbstract,
            rule.Title,
            Lets(rule.Lets),
            items,
            _site
        );
    }

    private SchematronNode Item(SchematronNode item) =>
        item switch
        {
            SchematronAssert assert => new SchematronAssert(
                assert.Location,
                _table.Apply(assert.Test),
                assert.Id,
                assert.Flags,
                assert.Role,
                assert.Severity,
                assert.Subject,
                assert.DiagnosticIds,
                assert.PropertyIds,
                Message(assert.Message)
            ),
            SchematronReport report => new SchematronReport(
                report.Location,
                _table.Apply(report.Test),
                report.Id,
                report.Flags,
                report.Role,
                report.Severity,
                report.Subject,
                report.DiagnosticIds,
                report.PropertyIds,
                Message(report.Message)
            ),
            _ => item,
        };

    private SchematronMessage Message(SchematronMessage message)
    {
        var parts = Parts(message.Parts);
        return ReferenceEquals(parts, message.Parts) ? message : new SchematronMessage(message.Location, parts);
    }

    // The same list when no part changed, so an assertion without a value-of shares its message.
    private IReadOnlyList<SchematronMessagePart> Parts(IReadOnlyList<SchematronMessagePart> parts)
    {
        List<SchematronMessagePart>? copy = null;
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            var next = Part(part);
            if (copy is null && !ReferenceEquals(next, part))
            {
                copy = new List<SchematronMessagePart>(parts.Count);
                for (var j = 0; j < i; j++)
                {
                    copy.Add(parts[j]);
                }
            }

            copy?.Add(next);
        }

        return copy ?? parts;
    }

    private SchematronMessagePart Part(SchematronMessagePart part)
    {
        switch (part)
        {
            case SchematronValueOf valueOf:
                var select = _table.Apply(valueOf.Select);
                return ReferenceEquals(select, valueOf.Select) ? part : new SchematronValueOf(valueOf.Location, select);
            case SchematronName { Path: { } path } name:
                var substituted = _table.Apply(path);
                return ReferenceEquals(substituted, path) ? part : new SchematronName(name.Location, substituted);
            case SchematronEmphasis emphasis:
                var emphasized = Parts(emphasis.Parts);
                return ReferenceEquals(emphasized, emphasis.Parts)
                    ? part
                    : new SchematronEmphasis(emphasis.Location, emphasized);
            case SchematronSpan span:
                var spanned = Parts(span.Parts);
                return ReferenceEquals(spanned, span.Parts)
                    ? part
                    : new SchematronSpan(span.Location, span.StyleClass, spanned);
            case SchematronDirection direction:
                var directed = Parts(direction.Parts);
                return ReferenceEquals(directed, direction.Parts)
                    ? part
                    : new SchematronDirection(direction.Location, direction.Value, directed);
            default:
                return part;
        }
    }
}
