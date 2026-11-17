namespace NetEvolve.Schemas.Xml.Regex.Parsing;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;

/// <summary>The syntax tree of a parsed pattern: the nodes, the index of the root, and the sets the <see cref="RegexNodeKind.Set"/> nodes refer to.</summary>
public sealed class RegexTree
{
    private readonly RegexNode[] _nodes;
    private readonly int _nodeCount;
    private readonly RegexCharacterSet[] _sets;

    internal RegexTree(RegexNode[] nodes, int nodeCount, int root, RegexCharacterSet[] sets)
    {
        _nodes = nodes;
        _nodeCount = nodeCount;
        Root = root;
        _sets = sets;
    }

    /// <summary>Gets the index of the root node.</summary>
    public int Root { get; }

    /// <summary>Gets the nodes; children and siblings are indexes into this span.</summary>
    public ReadOnlySpan<RegexNode> Nodes => new(_nodes, 0, _nodeCount);

    /// <summary>Gets the number of sets.</summary>
    public int SetCount => _sets.Length;

    /// <summary>Returns a set.</summary>
    /// <param name="index">The <see cref="RegexNode.Value"/> of a set node.</param>
    /// <returns>The set.</returns>
    public RegexCharacterSet GetSet(int index)
    {
        Argument.ThrowIfLessThan(index, 0);
        Argument.ThrowIfGreaterThan(index, _sets.Length - 1);
        return _sets[index];
    }
}
