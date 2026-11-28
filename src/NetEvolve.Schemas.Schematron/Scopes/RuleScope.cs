namespace NetEvolve.Schemas.Schematron.Scopes;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>A rule with its variables and the scope its <c>test</c>, <see langword="select"/>, <c>subject</c>, messages, and later variables compile in.</summary>
internal sealed class RuleScope
{
    internal RuleScope(SchematronRule rule, IReadOnlyList<VariableSlot> lets, QueryStaticScope scope)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(lets);
        ArgumentNullException.ThrowIfNull(scope);

        Rule = rule;
        Lets = lets;
        Scope = scope;
    }

    internal SchematronRule Rule { get; }

    internal IReadOnlyList<VariableSlot> Lets { get; }

    internal QueryStaticScope Scope { get; }
}
