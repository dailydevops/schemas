namespace NetEvolve.Schemas.XPath.Syntax.Lexer;

using System;

/// <summary>Stub.</summary>
internal ref struct XPathLexer
{
    /// <summary>Initializes a new instance of the <see cref="XPathLexer"/> struct.</summary>
    /// <param name="text">The expression.</param>
    public XPathLexer(ReadOnlySpan<char> text) => _ = text.Length;

    /// <summary>Reads the next token.</summary>
    /// <returns>The token.</returns>
    public readonly XPathToken Next() => throw new NotSupportedException("Not implemented.");
}
