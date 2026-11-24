namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Regex;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;

/// <summary>Short builders of <c>xsl:analyze-string</c> elements and of the bodies the regex tests use.</summary>
internal static class Rx
{
    /// <summary>An <c>xsl:analyze-string</c> with any attributes; an attribute whose value is <see langword="null"/> is left out.</summary>
    internal static XmlElem Analyze(int line, (string Name, string? Value)[] attributes, params object[] content)
    {
        var all = new List<object>();
        foreach (var (name, value) in attributes)
        {
            if (value is not null)
            {
                all.Add(A(name, value));
            }
        }

        all.AddRange(content);
        return X("analyze-string", line, [.. all]);
    }

    /// <summary>An <c>xsl:analyze-string</c> with <see langword="select"/>, <c>regex</c>, and <c>flags</c>.</summary>
    internal static XmlElem Analyze(string? select, string? regex, string? flags, int line, params object[] content) =>
        Analyze(line, [("select", select), ("regex", regex), ("flags", flags)], content);

    /// <summary>An <c>xsl:matching-substring</c>.</summary>
    internal static XmlElem Match(int line, params object[] content) => X("matching-substring", line, content);

    /// <summary>An <c>xsl:non-matching-substring</c>.</summary>
    internal static XmlElem NonMatch(int line, params object[] content) => X("non-matching-substring", line, content);

    /// <summary>An <c>xsl:value-of</c>.</summary>
    internal static XmlElem Out(string select, int line) => X("value-of", line, A("select", select));

    /// <summary>The usual instruction: a matching substring writes <c>[text]</c>, a non-matching one writes <c>(text)</c>.</summary>
    internal static XmlElem Brackets(string? select, string? regex, string? flags, int line) =>
        Analyze(
            select,
            regex,
            flags,
            line,
            Match(line + 1, Out("concat('[', ., ']')", line + 2)),
            NonMatch(line + 3, Out("concat('(', ., ')')", line + 4))
        );
}
