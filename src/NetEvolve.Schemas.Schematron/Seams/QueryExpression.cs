namespace NetEvolve.Schemas.Schematron.Seams;

using System;

/// <summary>An opaque compiled expression, owned by the binding that compiled it.</summary>
internal abstract class QueryExpression
{
    /// <summary>Gets the variable slots the expression reads directly; empty when it reads none.</summary>
    internal virtual ReadOnlySpan<int> ReferencedSlots => default;
}
