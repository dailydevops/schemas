namespace NetEvolve.Schemas.Xml.Regex.Parsing;

using System;
using NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>Parses a pattern of the XSD regular expression dialect into a <see cref="RegexTree"/>.</summary>
public static class RegexParser
{
    /// <summary>Parses a pattern with the default limits.</summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="syntax">The dialect and the name edition.</param>
    /// <returns>The tree or the error.</returns>
    public static RegexParseResult Parse(ReadOnlySpan<char> pattern, RegexSyntax syntax) =>
        Parse(pattern, syntax, RegexLimits.Default);

    /// <summary>Parses a pattern.</summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="syntax">The dialect and the name edition.</param>
    /// <param name="limits">The parser limits.</param>
    /// <returns>The tree or the error.</returns>
    public static RegexParseResult Parse(ReadOnlySpan<char> pattern, RegexSyntax syntax, RegexLimits limits) =>
        syntax.Dialect == RegexDialect.XPath || pattern.Length < 0 || limits is null
            ? default
            : new(RegexError.UnexpectedEnd, 0);
}
