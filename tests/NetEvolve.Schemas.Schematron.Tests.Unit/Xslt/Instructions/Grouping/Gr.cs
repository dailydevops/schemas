namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;

/// <summary>Short builders of <c>xsl:for-each-group</c> elements and of the bodies the grouping tests use.</summary>
internal static class Gr
{
    /// <summary>An <c>xsl:for-each-group</c> with any attributes; an attribute whose value is <see langword="null"/> is left out.</summary>
    internal static XmlElem Group(int line, (string Name, string? Value)[] attributes, params object[] content)
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
        return X("for-each-group", line, [.. all]);
    }

    /// <summary>An <c>xsl:for-each-group</c> with <c>group-by</c>.</summary>
    internal static XmlElem By(string? select, string? by, int line, params object[] content) =>
        Group(line, [("select", select), ("group-by", by)], content);

    /// <summary>An <c>xsl:for-each-group</c> with <c>group-adjacent</c>.</summary>
    internal static XmlElem Adjacent(string? select, string? adjacent, int line, params object[] content) =>
        Group(line, [("select", select), ("group-adjacent", adjacent)], content);

    /// <summary>An <c>xsl:for-each-group</c> with <c>group-starting-with</c>.</summary>
    internal static XmlElem Starting(string? select, string? pattern, int line, params object[] content) =>
        Group(line, [("select", select), ("group-starting-with", pattern)], content);

    /// <summary>An <c>xsl:for-each-group</c> with <c>group-ending-with</c>.</summary>
    internal static XmlElem Ending(string? select, string? pattern, int line, params object[] content) =>
        Group(line, [("select", select), ("group-ending-with", pattern)], content);

    /// <summary>The body that writes the items of the current group, then a semicolon: <c>i=a i=b;</c>.</summary>
    internal static XmlElem Members(int line) => X("value-of", line, A("select", "concat(show(current-group()), ';')"));

    /// <summary>The body that writes the grouping key, a colon, the items of the current group, then a semicolon: <c>a:i=a i=b;</c>.</summary>
    internal static XmlElem KeyAndMembers(int line) =>
        X("value-of", line, A("select", "concat(key(current-grouping-key()), ':', show(current-group()), ';')"));

    /// <summary>An <c>xsl:sort</c> with a select and an order.</summary>
    internal static XmlElem Sort(string select, string? order, int line) =>
        X("sort", line, A("select", select), A("order", order ?? "ascending"));
}
