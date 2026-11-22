namespace NetEvolve.Schemas.Schematron.Pipeline;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Expansion.AbstractPatterns;
using NetEvolve.Schemas.Schematron.Expansion.AbstractRules;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The stage Expand (design #137 AP-26): abstract rules are extended into the rules that use them (SCH-09, #204), then every pattern
/// with <c>is-a</c> becomes an instance of its abstract pattern (SCH-08, #203). The effective model holds neither abstract rules nor
/// abstract patterns. Both expansions run, so the problems of both are reported.
/// </summary>
internal sealed class ModelExpander : IModelExpander
{
    public bool TryExpand(
        ResolvedModel model,
        BuildSettings settings,
        IProblemSink problems,
        out SchematronSchema effective
    )
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(problems);

        var schema = model.Schema;
        var rules = AbstractRuleExpander.TryExpand(schema.Rules, schema.Patterns, problems, out var withRules);
        var patterns = AbstractPatternExpander.TryExpand(withRules, problems, out var expanded);

        effective = rules && patterns ? WithPatterns(schema, expanded) : schema;
        return rules && patterns;
    }

    private static SchematronSchema WithPatterns(
        SchematronSchema schema,
        IReadOnlyList<SchematronPatternBase> patterns
    ) =>
        new(
            schema.Location,
            schema.Title,
            schema.Paragraphs,
            schema.Parameters,
            schema.Lets,
            patterns,
            schema.Diagnostics,
            schema.Properties,
            schema.Id,
            schema.SchemaVersion,
            schema.EditionText,
            schema.Edition,
            schema.DefaultPhase,
            schema.QueryBinding,
            schema.Namespaces,
            schema.Phases,
            schema.Rules,
            schema.ForeignElements,
            schema.Includes
        );
}
