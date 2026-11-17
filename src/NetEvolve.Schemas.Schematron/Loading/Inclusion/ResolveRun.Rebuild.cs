namespace NetEvolve.Schemas.Schematron.Loading.Inclusion;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Parsing.Core;

/// <summary>Phase two: the models rebuilt with every reference replaced by the resolved model of its document.</summary>
internal sealed partial class ResolveRun
{
    private SchematronNode Rebuild(InclusionModule module)
    {
        switch (module.Model)
        {
            case SchematronSchema schema:
                var schemaParts = RebuildContainer(schema);
                return new SchematronSchema(
                    schema.Location,
                    schemaParts.Title,
                    schemaParts.Paragraphs.ToArray(),
                    schemaParts.Parameters.ToArray(),
                    schemaParts.Lets.ToArray(),
                    schemaParts.Patterns.ToArray(),
                    schemaParts.Diagnostics,
                    schemaParts.Properties,
                    schema.Id,
                    schema.SchemaVersion,
                    schema.EditionText,
                    schema.Edition,
                    schema.DefaultPhase,
                    schema.QueryBinding,
                    schema.Namespaces,
                    schema.Phases,
                    schemaParts.Rules.ToArray(),
                    schemaParts.Foreign.ToArray()
                );
            case SchematronLibrary library:
                var libraryParts = RebuildContainer(library);
                return new SchematronLibrary(
                    library.Location,
                    libraryParts.Title,
                    libraryParts.Paragraphs.ToArray(),
                    libraryParts.Parameters.ToArray(),
                    libraryParts.Lets.ToArray(),
                    libraryParts.Patterns.ToArray(),
                    libraryParts.Diagnostics,
                    libraryParts.Properties,
                    libraryParts.Rules.ToArray(),
                    libraryParts.Foreign.ToArray()
                );
            case SchematronPatternBase pattern:
                return RebuildPattern(pattern);
            case SchematronRule rule:
                return RebuildRule(rule);
            default:
                return module.Model;
        }
    }

    private ContainerParts RebuildContainer(SchematronContainer container)
    {
        var parts = new ContainerParts
        {
            Title = container.Title,
            Diagnostics = container.Diagnostics,
            Properties = container.Properties,
        };
        parts.Paragraphs.AddRange(container.Paragraphs);

        var patterns = new List<SchematronPatternBase>(container.Patterns.Count);
        foreach (var pattern in container.Patterns)
        {
            patterns.Add(RebuildPattern(pattern));
        }

        var includes = container.Includes;
        Interleave(patterns, parts.Patterns, includes, static i => i.Index, PatternsOf);
        Interleave(container.Lets, parts.Lets, includes, static i => i.LetIndex, LetsOf);
        Interleave(container.Parameters, parts.Parameters, includes, static i => i.ParameterIndex, ParametersOf);
        Interleave(container.ForeignElements, parts.Foreign, includes, static i => i.ForeignIndex, ForeignOf);

        foreach (var rules in container.Rules)
        {
            parts.Rules.Add(RebuildRules(rules));
        }

        foreach (var include in includes)
        {
            if (_children[include].Resolved is SchematronLibrary library)
            {
                parts.Rules.AddRange(library.Rules);
                parts.Diagnostics = Combine(parts.Diagnostics, library.Diagnostics);
                parts.Properties = Combine(parts.Properties, library.Properties);
            }
        }

        return parts;
    }

    // The own entries in order, with the contribution of each include before the entry its index names.
    private void Interleave<T>(
        IReadOnlyList<T> own,
        List<T> into,
        IReadOnlyList<SchematronInclude> includes,
        Func<SchematronInclude, int> index,
        Func<SchematronNode, IReadOnlyList<T>> contribution
    )
    {
        var cursor = 0;
        for (var k = 0; k <= own.Count; k++)
        {
            for (; cursor < includes.Count && index(includes[cursor]) == k; cursor++)
            {
                into.AddRange(contribution(_children[includes[cursor]].Resolved!));
            }

            if (k < own.Count)
            {
                into.Add(own[k]);
            }
        }
    }

    private static IReadOnlyList<SchematronPatternBase> PatternsOf(SchematronNode resolved) =>
        resolved switch
        {
            SchematronLibrary library => library.Patterns,
            SchematronPatternBase pattern => [pattern],
            _ => Array.Empty<SchematronPatternBase>(),
        };

    private static IReadOnlyList<SchematronLet> LetsOf(SchematronNode resolved) =>
        resolved is SchematronLibrary library ? library.Lets : Array.Empty<SchematronLet>();

    private static IReadOnlyList<SchematronParameter> ParametersOf(SchematronNode resolved) =>
        resolved is SchematronLibrary library ? library.Parameters : Array.Empty<SchematronParameter>();

    private static IReadOnlyList<SchematronForeignElement> ForeignOf(SchematronNode resolved) =>
        resolved is SchematronLibrary library ? library.ForeignElements : Array.Empty<SchematronForeignElement>();

    private static SchematronDiagnostics? Combine(SchematronDiagnostics? own, SchematronDiagnostics? other)
    {
        if (own is null || other is null)
        {
            return own ?? other;
        }

        var items = new List<SchematronDiagnostic>(own.Items);
        items.AddRange(other.Items);
        return new SchematronDiagnostics(own.Location, items);
    }

    private static SchematronProperties? Combine(SchematronProperties? own, SchematronProperties? other)
    {
        if (own is null || other is null)
        {
            return own ?? other;
        }

        var items = new List<SchematronProperty>(own.Items);
        items.AddRange(other.Items);
        return new SchematronProperties(own.Location, items);
    }

    private SchematronRules RebuildRules(SchematronRules container)
    {
        var rules = RebuildAll(container.Rules, out var changed);
        return changed
            ? new SchematronRules(container.Location, container.Id, container.Title, container.Paragraphs, rules)
            : container;
    }

    private SchematronRule[] RebuildAll(IReadOnlyList<SchematronRule> source, out bool changed)
    {
        changed = false;
        var rebuilt = new SchematronRule[source.Count];
        for (var i = 0; i < rebuilt.Length; i++)
        {
            rebuilt[i] = RebuildRule(source[i]);
            changed |= !ReferenceEquals(rebuilt[i], source[i]);
        }

        return rebuilt;
    }

    private SchematronPatternBase RebuildPattern(SchematronPatternBase pattern)
    {
        var rebuilt = RebuildAll(pattern.Rules, out var changed);
        if (!changed && pattern.Includes.Count == 0)
        {
            return pattern;
        }

        var rules = new List<SchematronRule>(rebuilt.Length + pattern.Includes.Count);
        Interleave(
            rebuilt,
            rules,
            pattern.Includes,
            static i => i.Index,
            static resolved => [(SchematronRule)resolved]
        );
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

    // An include is replaced by its element; an extends with href by the variables and items of the abstract rule of its
    // library: the variables of the target come first, the items take the position of the extends (as the expansion by id does).
    private SchematronRule RebuildRule(SchematronRule rule)
    {
        if (!HasReference(rule))
        {
            return rule;
        }

        var lets = new List<SchematronLet>();
        var items = new List<SchematronNode>(rule.Items.Count);
        foreach (var item in rule.Items)
        {
            if (item is SchematronInclude include)
            {
                items.Add(_children[include].Resolved!);
            }
            else if (item is SchematronExtends { Href: not null } extends)
            {
                var target = AbstractRuleOf(extends);
                if (target is null)
                {
                    continue;
                }

                lets.AddRange(target.Lets);
                items.AddRange(target.Items);
            }
            else
            {
                items.Add(item);
            }
        }

        lets.AddRange(rule.Lets);
        return new SchematronRule(
            rule.Location,
            rule.Id,
            rule.Context,
            rule.VisitEach,
            rule.Flags,
            rule.Role,
            rule.Subject,
            rule.IsAbstract,
            rule.Title,
            lets,
            items,
            rule.ExpandedFrom
        );
    }

    private static bool HasReference(SchematronRule rule)
    {
        foreach (var item in rule.Items)
        {
            if (item is SchematronInclude or SchematronExtends { Href: not null })
            {
                return true;
            }
        }

        return false;
    }

    private SchematronRule? AbstractRuleOf(SchematronExtends extends)
    {
        var module = _children[extends];
        var library = (SchematronLibrary)module.Resolved!;
        SchematronRule? found = null;
        var count = 0;
        foreach (var container in library.Rules)
        {
            Count(container.Rules, ref found, ref count);
        }

        foreach (var pattern in library.Patterns)
        {
            Count(pattern.Rules, ref found, ref count);
        }

        if (count != 1)
        {
            Report(
                SchematronDiagnosticCodes.ModuleInvalid,
                "The library '" + module.SystemId + "' must hold exactly one abstract rule, but holds " + count + ".",
                extends.Location
            );
        }

        return count == 1 ? found : null;
    }

    private static void Count(IReadOnlyList<SchematronRule> rules, ref SchematronRule? found, ref int count)
    {
        foreach (var rule in rules)
        {
            if (rule.IsAbstract)
            {
                found = rule;
                count++;
            }
        }
    }
}
