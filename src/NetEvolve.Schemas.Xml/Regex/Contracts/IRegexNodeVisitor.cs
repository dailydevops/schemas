namespace NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>A visitor over regex syntax tree nodes, one method per <see cref="RegexNodeKind"/>.</summary>
public interface IRegexNodeVisitor
{
    /// <summary>Visits an empty node.</summary>
    /// <param name="node">The node.</param>
    void VisitEmpty(in RegexNode node);

    /// <summary>Visits a character node.</summary>
    /// <param name="node">The node.</param>
    void VisitCharacter(in RegexNode node);

    /// <summary>Visits a character class node.</summary>
    /// <param name="node">The node.</param>
    void VisitSet(in RegexNode node);

    /// <summary>Visits a concatenation node.</summary>
    /// <param name="node">The node.</param>
    void VisitConcat(in RegexNode node);

    /// <summary>Visits an alternation node.</summary>
    /// <param name="node">The node.</param>
    void VisitAlternation(in RegexNode node);

    /// <summary>Visits a repeat node.</summary>
    /// <param name="node">The node.</param>
    void VisitRepeat(in RegexNode node);

    /// <summary>Visits a group node.</summary>
    /// <param name="node">The node.</param>
    void VisitGroup(in RegexNode node);

    /// <summary>Visits a wildcard node.</summary>
    /// <param name="node">The node.</param>
    void VisitAnyChar(in RegexNode node);

    /// <summary>Visits a back reference node.</summary>
    /// <param name="node">The node.</param>
    void VisitBackReference(in RegexNode node);

    /// <summary>Visits an anchor node.</summary>
    /// <param name="node">The node.</param>
    void VisitAnchor(in RegexNode node);
}
