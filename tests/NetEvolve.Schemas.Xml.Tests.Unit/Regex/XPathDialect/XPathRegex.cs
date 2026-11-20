namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.XPathDialect;

using System;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Matching;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>Parses and compiles XPath dialect patterns for the tests of this folder.</summary>
internal static class XPathRegex
{
    internal const RegexOptions All =
        RegexOptions.DotAll | RegexOptions.MultiLine | RegexOptions.IgnoreCase | RegexOptions.FreeSpacing;

    internal static RegexSyntax Syntax(RegexOptions flags = RegexOptions.None) =>
        new(RegexDialect.XPath, NameEdition.Xml10FifthEdition, flags);

    internal static RegexTree Tree(string pattern, RegexOptions flags = RegexOptions.None)
    {
        var result = RegexParser.Parse(pattern, Syntax(flags));
        return result.Tree ?? throw new InvalidOperationException($"{pattern}: {result.Error} at {result.Position}");
    }

    internal static RegexProgram Compile(string pattern, RegexOptions flags = RegexOptions.None)
    {
        var error = RegexProgram.TryCompile(Tree(pattern, flags), out var program);
        return error == RegexError.None ? program! : throw new InvalidOperationException($"{pattern}: {error}");
    }

    /// <summary>Searches and returns the outcome with the index and length of the match.</summary>
    internal static (RegexMatchResult Result, int Index, int Length) Search(
        string pattern,
        string input,
        RegexOptions flags = RegexOptions.None,
        RegexStepLimits? limits = null
    )
    {
        var result = Compile(pattern, flags)
            .Find(input, 0, limits ?? RegexStepLimits.Default, out var index, out var length);
        return (result, index, length);
    }

    /// <summary>Returns the matched text, <see langword="null"/> for no match; an aborted search is an error.</summary>
    internal static string? Found(string pattern, string input, RegexOptions flags = RegexOptions.None)
    {
        ArgumentNullException.ThrowIfNull(input);

        var (result, index, length) = Search(pattern, input, flags);
        return result switch
        {
            RegexMatchResult.Match => input.Substring(index, length),
            RegexMatchResult.NoMatch => null,
            _ => throw new InvalidOperationException($"{pattern}: aborted"),
        };
    }

    internal static bool IsMatch(string pattern, string input, RegexOptions flags = RegexOptions.None) =>
        Compile(pattern, flags).Match(input, RegexStepLimits.Default) == RegexMatchResult.Match;
}
