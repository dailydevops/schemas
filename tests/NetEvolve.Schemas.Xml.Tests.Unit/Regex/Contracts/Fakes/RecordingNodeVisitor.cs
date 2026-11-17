namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Contracts.Fakes;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>A struct visitor that records the kinds it saw into a shared list.</summary>
internal readonly struct RecordingNodeVisitor(List<RegexNodeKind> seen) : IRegexNodeVisitor
{
    public void VisitEmpty(in RegexNode node) => seen.Add(node.Kind);

    public void VisitCharacter(in RegexNode node) => seen.Add(node.Kind);

    public void VisitSet(in RegexNode node) => seen.Add(node.Kind);

    public void VisitConcat(in RegexNode node) => seen.Add(node.Kind);

    public void VisitAlternation(in RegexNode node) => seen.Add(node.Kind);

    public void VisitRepeat(in RegexNode node) => seen.Add(node.Kind);

    public void VisitGroup(in RegexNode node) => seen.Add(node.Kind);

    public void VisitAnyChar(in RegexNode node) => seen.Add(node.Kind);

    public void VisitBackReference(in RegexNode node) => seen.Add(node.Kind);

    public void VisitAnchor(in RegexNode node) => seen.Add(node.Kind);
}
