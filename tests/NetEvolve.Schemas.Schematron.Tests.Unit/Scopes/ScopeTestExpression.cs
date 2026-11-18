namespace NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;

using System;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>An expression of the fake binding: its text, the declared type of the variable it feeds, and the slots it reads.</summary>
internal sealed class ScopeTestExpression(string text, string? declaredType, int[] slots) : QueryExpression
{
    internal string Text { get; } = text;

    internal string? DeclaredType { get; } = declaredType;

    internal override ReadOnlySpan<int> ReferencedSlots => slots;
}
