namespace NetEvolve.Schemas.Schematron.Scopes;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>One declared variable: its slot, its compiled value, and what is known about its type before any evaluation.</summary>
internal sealed class VariableSlot
{
    internal VariableSlot(
        string name,
        int slot,
        ScopeLevel level,
        DocumentLocation location,
        QueryExpression? value,
        string? declaredType,
        SchematronProblem? staticProblem,
        bool isParameter = false,
        IReadOnlyList<SchematronNode>? content = null
    )
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(slot, 0);

        Name = name;
        Slot = slot;
        Level = level;
        Location = location;
        Value = value;
        DeclaredType = declaredType;
        StaticProblem = staticProblem;
        IsParameter = isParameter;
        Content = content;
    }

    internal string Name { get; }

    internal int Slot { get; }

    internal ScopeLevel Level { get; }

    internal DocumentLocation Location { get; }

    /// <summary>Gets the compiled value, or <see langword="null"/> for a <c>param</c> without a default.</summary>
    internal QueryExpression? Value { get; }

    /// <summary>Gets the <see langword="as"/> type as written, or <see langword="null"/>.</summary>
    internal string? DeclaredType { get; }

    /// <summary>
    /// Gets the type error of a constant at pattern or rule level, found once at compile; it is reported at the start of every pass
    /// or firing and marks the slot failed, as an evaluate-stage issue (design #150 EP-5).
    /// </summary>
    internal SchematronProblem? StaticProblem { get; }

    /// <summary>Gets a value indicating whether the variable is a schema-level <c>param</c>, which the caller can set.</summary>
    internal bool IsParameter { get; }

    /// <summary>
    /// Gets the element content of a <see langword="let"/> without a <see langword="value"/> attribute, or <see langword="null"/>; the binder
    /// builds it into a temporary tree when the slot is read, and <see cref="Value"/> is <see langword="null"/> then.
    /// </summary>
    internal IReadOnlyList<SchematronNode>? Content { get; }
}
