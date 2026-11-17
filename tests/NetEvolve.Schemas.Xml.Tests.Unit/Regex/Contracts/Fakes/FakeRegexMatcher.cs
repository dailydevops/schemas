namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Contracts.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Regex;

/// <summary>A scripted matcher: matches the literal pattern, and aborts when the input is longer than the step ceiling.</summary>
internal sealed class FakeRegexMatcher(string pattern, RegexDialect dialect) : IRegexMatcher
{
    public RegexDialect Dialect => dialect;

    public RegexMatchResult Match(ReadOnlySpan<char> input, RegexStepLimits limits)
    {
        if (input.Length > limits.MaxNfaSteps)
        {
            return RegexMatchResult.Aborted;
        }

        var matches =
            input.SequenceEqual(pattern.AsSpan())
            || (dialect == RegexDialect.XPath && input.IndexOf(pattern.AsSpan()) >= 0);

        return matches ? RegexMatchResult.Match : RegexMatchResult.NoMatch;
    }
}

/// <summary>A struct visitor that records the kinds it saw into a shared list.</summary>
internal readonly struct RecordingNodeVisitor(List<RegexNodeKind> seen) : IRegexNodeVisitor
{
    public void VisitEmpty(in RegexNode node) => seen.Add(node.Kind);

    public void VisitChar(in RegexNode node) => seen.Add(node.Kind);

    public void VisitSet(in RegexNode node) => seen.Add(node.Kind);

    public void VisitConcat(in RegexNode node) => seen.Add(node.Kind);

    public void VisitAlternation(in RegexNode node) => seen.Add(node.Kind);

    public void VisitRepeat(in RegexNode node) => seen.Add(node.Kind);

    public void VisitGroup(in RegexNode node) => seen.Add(node.Kind);

    public void VisitAnyChar(in RegexNode node) => seen.Add(node.Kind);

    public void VisitBackReference(in RegexNode node) => seen.Add(node.Kind);

    public void VisitAnchor(in RegexNode node) => seen.Add(node.Kind);
}
