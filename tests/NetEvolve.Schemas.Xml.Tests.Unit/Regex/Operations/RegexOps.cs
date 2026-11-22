namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Operations;

using System;
using System.Text;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Tests.Unit.Regex.XPathDialect;

/// <summary>Creates the operations of XPath dialect patterns and renders their results as text for the tests of this folder.</summary>
internal static class RegexOps
{
    internal static RegexOperations Create(string pattern, RegexOptions flags = RegexOptions.None)
    {
        var error = RegexOperations.TryCreate(XPathRegex.Tree(pattern, flags), out var operations);
        return error == RegexError.None ? operations! : throw new InvalidOperationException($"{pattern}: {error}");
    }

    internal static string Repeat(string unit, int count)
    {
        var text = new StringBuilder(unit.Length * count);
        for (var i = 0; i < count; i++)
        {
            _ = text.Append(unit);
        }

        return text.ToString();
    }

    /// <summary>
    /// Renders every match as <c>index,length</c> followed by <c>|index,length</c> per group, or <c>|-</c> for a group that took
    /// no part in the match; the matches are joined by <c>;</c> and an aborted iteration ends with <c>!</c>.
    /// </summary>
    internal static string Matches(RegexOperations operations, string input, RegexStepLimits? limits = null)
    {
        var text = new StringBuilder();
        var matches = operations.EnumerateMatches(input, limits);
        while (matches.MoveNext())
        {
            var match = matches.Current;
            if (text.Length > 0)
            {
                _ = text.Append(';');
            }

            _ = text.Append(match.Index).Append(',').Append(match.Length);
            for (var group = 1; group <= match.GroupCount; group++)
            {
                _ = match.TryGetGroup(group, out var range)
                    ? text.Append('|').Append(range.Offset).Append(',').Append(range.Length)
                    : text.Append("|-");
            }
        }

        var aborted = matches.IsAborted;
        matches.Dispose();
        return aborted ? text.Append('!').ToString() : text.ToString();
    }

    internal static string Matches(
        string pattern,
        string input,
        RegexOptions flags = RegexOptions.None,
        RegexStepLimits? limits = null
    ) => Matches(Create(pattern, flags), input, limits);

    /// <summary>Returns the text of one group of the first match, <c>none</c> when there is no match.</summary>
    internal static string GroupText(string pattern, string input, int group)
    {
        var matches = Create(pattern).EnumerateMatches(input);
        try
        {
            return matches.MoveNext() ? matches.Current.GetGroupText(group).ToString() : "none";
        }
        finally
        {
            matches.Dispose();
        }
    }

    /// <summary>Asks the first match whether a group took part in it.</summary>
    internal static bool IsGroupMatched(string pattern, string input, int group)
    {
        var matches = Create(pattern).EnumerateMatches(input);
        try
        {
            return matches.MoveNext() && matches.Current.TryGetGroup(group, out _);
        }
        finally
        {
            matches.Dispose();
        }
    }

    /// <summary>Returns the replaced text, or the name of the status when the operation did not succeed.</summary>
    internal static string Replace(
        string pattern,
        string input,
        string replacement,
        RegexOptions flags = RegexOptions.None,
        RegexStepLimits? limits = null
    )
    {
        var status = Create(pattern, flags).Replace(input, replacement, limits, out var result);
        return status == RegexOperationStatus.Success ? result! : status.ToString();
    }

    /// <summary>Renders the tokens as <c>[token]</c> each, or the name of the status when the operation did not succeed.</summary>
    internal static string Tokenize(
        string pattern,
        string input,
        RegexOptions flags = RegexOptions.None,
        RegexStepLimits? limits = null
    )
    {
        var status = Create(pattern, flags).TryTokenize(input, limits, out var tokens);
        if (status != RegexOperationStatus.Success)
        {
            return status.ToString();
        }

        var text = new StringBuilder();
        while (tokens.MoveNext())
        {
            _ = text.Append('[').Append(tokens.Current.Slice(input)).Append(']');
        }

        var aborted = tokens.IsAborted;
        tokens.Dispose();
        return aborted ? text.Append('!').ToString() : text.ToString();
    }
}
