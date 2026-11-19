namespace NetEvolve.Schemas.XPath.Collations;

using System;

/// <summary>The Unicode codepoint collation.</summary>
internal sealed class CodepointCollation : Collation
{
    private CodepointCollation()
        : base("http://www.w3.org/2005/xpath-functions/collation/codepoint") { }

    /// <summary>Gets the one instance.</summary>
    public static CodepointCollation Instance { get; } = new();

    /// <inheritdoc />
    public override int Compare(string first, string second) => 0;

    /// <inheritdoc />
    public override bool StartsWith(ReadOnlySpan<char> text, ReadOnlySpan<char> value) => false;

    /// <inheritdoc />
    public override bool EndsWith(ReadOnlySpan<char> text, ReadOnlySpan<char> value) => false;

    /// <inheritdoc />
    public override bool TryFind(ReadOnlySpan<char> text, ReadOnlySpan<char> value, out int index, out int length)
    {
        index = -1;
        length = 0;
        return false;
    }
}
