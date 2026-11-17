namespace NetEvolve.Schemas.XPath.Syntax.Lexer;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>A token as a kind plus its range in the input; the text is never copied.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XPathToken
{
    /// <summary>Initializes a new instance of the <see cref="XPathToken"/> struct.</summary>
    /// <param name="kind">The kind.</param>
    /// <param name="span">The range in the input.</param>
    /// <param name="error">The reason when <paramref name="kind"/> is <see cref="XPathTokenKind.Error"/>.</param>
    public XPathToken(XPathTokenKind kind, SourceSpan span, XPathLexicalError error = XPathLexicalError.None)
    {
        Kind = kind;
        Span = span;
        Error = error;
    }

    /// <summary>Gets the kind.</summary>
    public XPathTokenKind Kind { get; }

    /// <summary>Gets the range in the input.</summary>
    public SourceSpan Span { get; }

    /// <summary>Gets the reason of an error token, otherwise <see cref="XPathLexicalError.None"/>.</summary>
    public XPathLexicalError Error { get; }
}
