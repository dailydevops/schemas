namespace NetEvolve.Schemas.Xml.Regex.Parsing;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>The outcome of parsing a pattern: the tree, or an error code with the position in the pattern.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct RegexParseResult
{
    internal RegexParseResult(RegexTree tree)
    {
        Tree = tree;
        Error = RegexError.None;
        Position = -1;
    }

    internal RegexParseResult(RegexError error, int position)
    {
        Tree = null;
        Error = error;
        Position = position;
    }

    /// <summary>Gets the tree, or <see langword="null"/> when parsing failed.</summary>
    public RegexTree? Tree { get; }

    /// <summary>Gets the error, <see cref="RegexError.None"/> on success.</summary>
    public RegexError Error { get; }

    /// <summary>Gets the zero-based UTF-16 index in the pattern of the construct at fault (the pattern length for an unexpected end), or -1 on success.</summary>
    public int Position { get; }

    /// <summary>Gets a value indicating whether the pattern parsed.</summary>
    public bool Succeeded => Error == RegexError.None;
}
