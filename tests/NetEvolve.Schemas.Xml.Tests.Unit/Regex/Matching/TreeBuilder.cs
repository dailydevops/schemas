namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Matching;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;

/// <summary>Builds the array-backed syntax tree by hand, the way the parser will emit it.</summary>
internal sealed class TreeBuilder
{
    private readonly List<RegexNode> _nodes = [];
    private readonly List<RegexCharacterSet> _sets = [];

    public int Root { get; private set; } = RegexNode.NoNode;

    public int Add(RegexNode node)
    {
        _nodes.Add(node);
        return _nodes.Count - 1;
    }

    public int Char(char c) => Add(RegexNode.Character(c));

    public int CodePoint(int codePoint) => Add(RegexNode.Character(codePoint));

    public int Any() => Add(RegexNode.AnyChar());

    public int Set(params int[] ranges)
    {
        _sets.Add(new RegexCharacterSet(ranges));
        return Add(RegexNode.Set(_sets.Count - 1));
    }

    public int Concat(params int[] children) => Add(RegexNode.Concat(Link(children)));

    public int Alt(params int[] children) => Add(RegexNode.Alternation(Link(children)));

    public int Repeat(int child, int min, int max) => Add(RegexNode.Repeat(child, min, max, lazy: false));

    public int Group(int child) => Add(RegexNode.Group(child, 1));

    public int Text(string text)
    {
        var kids = new int[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            kids[i] = Char(text[i]);
        }

        return Concat(kids);
    }

    public TreeBuilder WithRoot(int root)
    {
        Root = root;
        return this;
    }

    public RegexProgram Compile(RegexDialect dialect = RegexDialect.Xsd)
    {
        var error = RegexProgram.TryCompile(_nodes.ToArray(), Root, _sets.ToArray(), dialect, out var program);
        return error == RegexError.None ? program! : throw new InvalidOperationException(error.ToString());
    }

    public RegexError TryCompile(RegexDialect dialect = RegexDialect.Xsd) =>
        RegexProgram.TryCompile(_nodes.ToArray(), Root, _sets.ToArray(), dialect, out _);

    private int Link(int[] children)
    {
        for (var i = 0; i < children.Length - 1; i++)
        {
            _nodes[children[i]] = _nodes[children[i]].WithNextSibling(children[i + 1]);
        }

        return children.Length == 0 ? RegexNode.NoNode : children[0];
    }
}
