namespace NetEvolve.Schemas.Schematron.Expansion.AbstractPatterns;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Expands <c>is-a</c> by pattern id (stage Expand, abstract patterns): the rules and variables of the abstract pattern are
/// copied into the instance with the parameters substituted, and abstract patterns never reach the effective model.
/// </summary>
/// <remarks>
/// <para>
/// Lookup scope: document-wide over every pattern with an id. The instance keeps its own id (phases name it), role, documents,
/// title, and paragraphs, and falls back to those of the definition. Copied nodes carry the location of their definition and
/// <see cref="SchematronPatternBase.ExpandedFrom"/> names the instance. Abstract rules inside the abstract pattern are copied
/// as they are; the abstract rule step drops them.
/// </para>
/// <para>
/// Parameters: when the abstract pattern declares <c>param</c> elements, an instance parameter has to be one of them
/// (<c>SCH-PARAM-UNKNOWN</c>), and a declared parameter needs a value from the instance or from its default
/// (<c>SCH-PARAM-MISSING</c>). Limitation: when the abstract pattern declares none, every instance parameter that has a value is
/// accepted and substituted, because the 2016 grammar declares parameters on the instance only.
/// </para>
/// </remarks>
internal static class AbstractPatternExpander
{
    internal static bool TryExpand(
        IReadOnlyList<SchematronPatternBase> patterns,
        IProblemSink problems,
        out IReadOnlyList<SchematronPatternBase> expanded
    )
    {
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentNullException.ThrowIfNull(problems);

        var catalog = new Dictionary<string, SchematronPatternBase>(StringComparer.Ordinal);
        foreach (var pattern in patterns)
        {
            if (pattern.Id is { } id && !catalog.ContainsKey(id))
            {
                catalog.Add(id, pattern);
            }
        }

        var result = new List<SchematronPatternBase>(patterns.Count);
        var failed = false;
        foreach (var pattern in patterns)
        {
            if (pattern.IsAbstract)
            {
                continue;
            }

            if (pattern.IsA is null)
            {
                result.Add(pattern);
            }
            else if (PatternInstance.TryCreate(pattern, catalog, problems, out var instance))
            {
                result.Add(instance);
            }
            else
            {
                failed = true;
            }
        }

        expanded = failed ? patterns : result;
        return !failed;
    }
}
