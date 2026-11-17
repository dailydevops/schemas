namespace NetEvolve.Schemas.Xml.Regex.Contracts;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;

/// <summary>
/// One node of the array-backed regex syntax tree. Nodes refer to each other by index into the array that holds them: a
/// parent points to its first child and every child to its next sibling.
/// </summary>
/// <remarks>
/// <see cref="Value"/> is the code point of a <see cref="RegexNodeKind.Character"/>, the index of the set of a
/// <see cref="RegexNodeKind.Set"/>, the group number of a <see cref="RegexNodeKind.Group"/> (<see cref="NoGroup"/> for a
/// non-capturing group), the group number of a <see cref="RegexNodeKind.BackReference"/>, and the
/// <see cref="RegexAnchor"/> of an <see cref="RegexNodeKind.Anchor"/>. <see langword="default"/> is an empty node.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly record struct RegexNode
{
    /// <summary>The index that stands for no node.</summary>
    public const int NoNode = -1;

    /// <summary>The group number of a group that does not capture.</summary>
    public const int NoGroup = -1;

    /// <summary>The <see cref="Max"/> of a repeat without an upper bound.</summary>
    public const int Unbounded = -1;

    private const int MaxCodePoint = 0x10FFFF;

    private RegexNode(RegexNodeKind kind, int value, int firstChild, int min, int max, bool isLazy, int nextSibling)
    {
        Kind = kind;
        Value = value;
        FirstChild = firstChild;
        Min = min;
        Max = max;
        IsLazy = isLazy;
        NextSibling = nextSibling;
    }

    /// <summary>Gets the kind.</summary>
    public RegexNodeKind Kind { get; }

    /// <summary>Gets the kind-specific value; see the remarks of the type.</summary>
    public int Value { get; }

    /// <summary>Gets the index of the first child, <see cref="NoNode"/> for none.</summary>
    public int FirstChild { get; }

    /// <summary>Gets the minimum count of a repeat.</summary>
    public int Min { get; }

    /// <summary>Gets the maximum count of a repeat, <see cref="Unbounded"/> for none.</summary>
    public int Max { get; }

    /// <summary>Gets a value indicating whether a repeat is lazy.</summary>
    public bool IsLazy { get; }

    /// <summary>Gets the index of the next sibling, <see cref="NoNode"/> for none.</summary>
    public int NextSibling { get; }

    /// <summary>Creates an empty node, which matches the empty string.</summary>
    /// <returns>The node.</returns>
    public static RegexNode Empty() => Create(RegexNodeKind.Empty, 0, NoNode);

    /// <summary>Creates a node for a single code point.</summary>
    /// <param name="codePoint">The code point, 0 to 0x10FFFF.</param>
    /// <returns>The node.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="codePoint"/> is outside the Unicode range.</exception>
    public static RegexNode Character(int codePoint)
    {
        Argument.ThrowIfLessThan(codePoint, 0);
        Argument.ThrowIfGreaterThan(codePoint, MaxCodePoint);

        return Create(RegexNodeKind.Character, codePoint, NoNode);
    }

    /// <summary>Creates a node for a character class, resolved to a set held by the owner of the tree.</summary>
    /// <param name="setIndex">The index of the set, zero or greater.</param>
    /// <returns>The node.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="setIndex"/> is negative.</exception>
    public static RegexNode Set(int setIndex)
    {
        Argument.ThrowIfLessThan(setIndex, 0);

        return Create(RegexNodeKind.Set, setIndex, NoNode);
    }

    /// <summary>Creates a concatenation of its children.</summary>
    /// <param name="firstChild">The index of the first child.</param>
    /// <returns>The node.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="firstChild"/> is below <see cref="NoNode"/>.</exception>
    public static RegexNode Concat(int firstChild)
    {
        Argument.ThrowIfLessThan(firstChild, NoNode);

        return Create(RegexNodeKind.Concat, 0, firstChild);
    }

    /// <summary>Creates an alternation of its children.</summary>
    /// <param name="firstChild">The index of the first child.</param>
    /// <returns>The node.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="firstChild"/> is below <see cref="NoNode"/>.</exception>
    public static RegexNode Alternation(int firstChild)
    {
        Argument.ThrowIfLessThan(firstChild, NoNode);

        return Create(RegexNodeKind.Alternation, 0, firstChild);
    }

    /// <summary>Creates a repeat of its child.</summary>
    /// <param name="child">The index of the repeated node.</param>
    /// <param name="min">The minimum count, zero or greater.</param>
    /// <param name="max">The maximum count, at least <paramref name="min"/>, or <see cref="Unbounded"/>.</param>
    /// <param name="lazy"><see langword="true"/> for a lazy repeat.</param>
    /// <returns>The node.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">A count is out of range.</exception>
    public static RegexNode Repeat(int child, int min, int max, bool lazy)
    {
        Argument.ThrowIfLessThan(child, NoNode);
        Argument.ThrowIfLessThan(min, 0);
        if (max != Unbounded)
        {
            Argument.ThrowIfLessThan(max, min);
        }

        return new(RegexNodeKind.Repeat, 0, child, min, max, lazy, NoNode);
    }

    /// <summary>Creates a group around its child.</summary>
    /// <param name="child">The index of the grouped node.</param>
    /// <param name="groupNumber">The group number, or <see cref="NoGroup"/> for a non-capturing group.</param>
    /// <returns>The node.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="groupNumber"/> is below <see cref="NoGroup"/>.</exception>
    public static RegexNode Group(int child, int groupNumber)
    {
        Argument.ThrowIfLessThan(groupNumber, NoGroup);

        return Create(RegexNodeKind.Group, groupNumber, child);
    }

    /// <summary>Creates a node that matches any character; it is a wildcard whose line-break handling depends on the flags.</summary>
    /// <returns>The node.</returns>
    public static RegexNode AnyChar() => Create(RegexNodeKind.AnyChar, 0, NoNode);

    /// <summary>Creates a back reference, legal in the XPath dialect only.</summary>
    /// <param name="groupNumber">The referenced group number, one or greater.</param>
    /// <returns>The node.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="groupNumber"/> is below one.</exception>
    public static RegexNode BackReference(int groupNumber)
    {
        Argument.ThrowIfLessThan(groupNumber, 1);

        return Create(RegexNodeKind.BackReference, groupNumber, NoNode);
    }

    /// <summary>Creates an anchor, legal in the XPath dialect only.</summary>
    /// <param name="anchor">The anchor.</param>
    /// <returns>The node.</returns>
    public static RegexNode Anchor(RegexAnchor anchor) => Create(RegexNodeKind.Anchor, (int)anchor, NoNode);

    /// <summary>Returns a copy with another next sibling.</summary>
    /// <param name="nextSibling">The index of the next sibling, or <see cref="NoNode"/>.</param>
    /// <returns>The new node.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="nextSibling"/> is below <see cref="NoNode"/>.</exception>
    public RegexNode WithNextSibling(int nextSibling)
    {
        Argument.ThrowIfLessThan(nextSibling, NoNode);

        return new(Kind, Value, FirstChild, Min, Max, IsLazy, nextSibling);
    }

    /// <summary>Calls the visitor method for the kind of this node.</summary>
    /// <typeparam name="TVisitor">The visitor type, a class or a mutable struct.</typeparam>
    /// <param name="visitor">The visitor; passed by reference so a mutable struct keeps its state.</param>
    public void Accept<TVisitor>(ref TVisitor visitor)
        where TVisitor : IRegexNodeVisitor
    {
        switch (Kind)
        {
            case RegexNodeKind.Character:
                visitor.VisitCharacter(this);
                break;
            case RegexNodeKind.Set:
                visitor.VisitSet(this);
                break;
            case RegexNodeKind.Concat:
                visitor.VisitConcat(this);
                break;
            case RegexNodeKind.Alternation:
                visitor.VisitAlternation(this);
                break;
            case RegexNodeKind.Repeat:
                visitor.VisitRepeat(this);
                break;
            case RegexNodeKind.Group:
                visitor.VisitGroup(this);
                break;
            case RegexNodeKind.AnyChar:
                visitor.VisitAnyChar(this);
                break;
            case RegexNodeKind.BackReference:
                visitor.VisitBackReference(this);
                break;
            case RegexNodeKind.Anchor:
                visitor.VisitAnchor(this);
                break;
            default:
                visitor.VisitEmpty(this);
                break;
        }
    }

    private static RegexNode Create(RegexNodeKind kind, int value, int firstChild) =>
        new(kind, value, firstChild, 0, 0, false, NoNode);
}
