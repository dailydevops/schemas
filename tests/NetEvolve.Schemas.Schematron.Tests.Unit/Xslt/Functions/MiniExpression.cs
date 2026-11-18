namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>An expression of the mini binding: its tree, the declared type of the variable it feeds, and the slots it reads.</summary>
internal sealed class MiniExpression(string text, MiniQuery.Node node, string? declaredType, int[] slots)
    : QueryExpression
{
    internal string Text { get; } = text;

    internal MiniQuery.Node Node { get; } = node;

    internal string? DeclaredType { get; } = declaredType;

    internal override ReadOnlySpan<int> ReferencedSlots => slots;
}
