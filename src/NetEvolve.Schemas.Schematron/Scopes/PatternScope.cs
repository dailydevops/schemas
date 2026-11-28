namespace NetEvolve.Schemas.Schematron.Scopes;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// A pattern or group with its variables and the scope its rule <c>context</c> expressions and its dynamic attributes compile in;
/// the variables of its rules are not visible there.
/// </summary>
internal sealed class PatternScope
{
    internal PatternScope(
        SchematronPatternBase pattern,
        IReadOnlyList<VariableSlot> lets,
        QueryStaticScope scope,
        IReadOnlyList<RuleScope> rules
    )
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(lets);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(rules);

        Pattern = pattern;
        Lets = lets;
        Scope = scope;
        Rules = rules;
    }

    internal SchematronPatternBase Pattern { get; }

    internal IReadOnlyList<VariableSlot> Lets { get; }

    internal QueryStaticScope Scope { get; }

    internal IReadOnlyList<RuleScope> Rules { get; }
}
