namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Regex;

using System;
using System.Text;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.Xml.Regex.Parsing;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;
using NetEvolve.Schemas.Xml.Text.CharClasses;
using NetEvolve.Schemas.XPath.Functions.Regex;

/// <summary>Compiles patterns of the XPath dialect and renders the segments of an input as text for the tests of this folder.</summary>
internal static class RegexHarness
{
    internal static RegexOperations Compile(string pattern, RegexOptions options = RegexOptions.None)
    {
        var parsed = RegexParser.Parse(
            pattern,
            new RegexSyntax(RegexDialect.XPath, NameEdition.Xml10SecondEdition, options)
        );
        RegexOperations? operations = null;
        var error = parsed.Succeeded ? RegexOperations.TryCreate(parsed.Tree!, out operations) : parsed.Error;
        return error == RegexError.None ? operations! : throw new InvalidOperationException($"{pattern}: {error}");
    }

    /// <summary>
    /// Renders every segment: <c>N</c> and the range of a non-matching segment, <c>M</c> and the range of a match followed by
    /// <c>|offset,length</c> per group, or <c>|-</c> for a group that took no part in the match; segments are joined by <c>;</c>, and an
    /// iteration that ended on a step ceiling ends with <c>!</c>.
    /// </summary>
    internal static string Segments(RegexOperations operations, string input, RegexStepLimits? limits = null)
    {
        var text = new StringBuilder();
        var segments = new MatchSegmentEnumerator(operations, input.AsSpan(), limits);
        try
        {
            while (segments.MoveNext())
            {
                var segment = segments.Current;
                if (text.Length > 0)
                {
                    _ = text.Append(';');
                }

                _ = text.Append(segment.Kind == MatchSegmentKind.Match ? 'M' : 'N')
                    .Append(segment.Index)
                    .Append(',')
                    .Append(segment.Length);
                for (var group = 1; group <= segment.GroupCount; group++)
                {
                    _ = segment.TryGetGroup(group, out var range)
                        ? text.Append('|').Append(range.Offset).Append(',').Append(range.Length)
                        : text.Append("|-");
                }
            }

            return segments.IsAborted ? text.Append('!').ToString() : text.ToString();
        }
        finally
        {
            segments.Dispose();
        }
    }

    internal static string Segments(
        string pattern,
        string input,
        RegexOptions options = RegexOptions.None,
        RegexStepLimits? limits = null
    ) => Segments(Compile(pattern, options), input, limits);
}
