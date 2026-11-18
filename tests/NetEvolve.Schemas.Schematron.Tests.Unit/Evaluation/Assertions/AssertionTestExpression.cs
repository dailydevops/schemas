namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>An expression of the fake binding: its text, its kind, and the slots it reads.</summary>
internal sealed class AssertionTestExpression(string text, QueryExpressionKind kind, int[] slots) : QueryExpression
{
    internal string Text { get; } = text;

    internal QueryExpressionKind Kind { get; } = kind;

    internal override ReadOnlySpan<int> ReferencedSlots => slots;
}
