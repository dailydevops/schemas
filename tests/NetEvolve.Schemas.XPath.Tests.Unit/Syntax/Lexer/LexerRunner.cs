namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Lexer;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Syntax.Lexer;

internal static class LexerRunner
{
    /// <summary>Tokenizes up to and excluding the end of the input.</summary>
    internal static List<XPathToken> Lex(string text)
    {
        var tokens = new List<XPathToken>();
        var lexer = new XPathLexer(text.AsSpan());
        while (true)
        {
            var token = lexer.Next();
            if (token.Kind == XPathTokenKind.EndOfInput)
            {
                return tokens;
            }

            tokens.Add(token);
        }
    }

    /// <summary>Gets the kinds, space separated.</summary>
    internal static string Kinds(string text) => string.Join(" ", Lex(text).ConvertAll(t => t.Kind.ToString()));

    /// <summary>Gets the covered text of each token, joined with a bar.</summary>
    internal static string Texts(string text) =>
        string.Join("|", Lex(text).ConvertAll(t => text.Substring(t.Span.Offset, t.Span.Length)));
}
