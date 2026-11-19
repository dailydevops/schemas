namespace NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;

using System;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>A predicate of the pattern test binding: its text and the variable slots it reads.</summary>
internal sealed class PatternTestExpression(string text, int[] slots) : QueryExpression
{
    internal string Text { get; } = text;

    internal override ReadOnlySpan<int> ReferencedSlots => slots;
}
