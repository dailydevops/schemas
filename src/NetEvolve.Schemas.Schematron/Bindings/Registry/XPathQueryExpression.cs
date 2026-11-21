namespace NetEvolve.Schemas.Schematron.Bindings.Registry;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// An expression compiled by <see cref="XPathQueryBinding"/>: the compiled XPath expression, the slots it reads with the name each
/// slot has in the scope it was compiled in, and the declared type of the variable it is the value of. Immutable; shared by every run.
/// </summary>
internal sealed class XPathQueryExpression : QueryExpression
{
    private readonly int[] _slots;

    internal XPathQueryExpression(
        XPathExpression expression,
        DocumentLocation location,
        int[] slots,
        XPathQName[] names,
        string? declaredType
    )
    {
        Argument.ThrowIfNull(expression);
        Argument.ThrowIfNull(slots);
        Argument.ThrowIfNull(names);

        Expression = expression;
        Location = location;
        _slots = slots;
        Names = names;
        DeclaredType = declaredType;
    }

    internal XPathExpression Expression { get; }

    /// <summary>Gets the location of problems that the run reports for this expression.</summary>
    internal DocumentLocation Location { get; }

    /// <summary>Gets the names of the slots, parallel to <see cref="ReferencedSlots"/>.</summary>
    internal XPathQName[] Names { get; }

    internal string? DeclaredType { get; }

    internal override ReadOnlySpan<int> ReferencedSlots => _slots;
}
