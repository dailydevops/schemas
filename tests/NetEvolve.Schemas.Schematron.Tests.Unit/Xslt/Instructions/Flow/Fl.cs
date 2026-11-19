namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;

/// <summary>Short builders of the flow instructions; an attribute that is <see langword="null"/> is left out.</summary>
internal static class Fl
{
    private static XmlElem Build(string local, int line, (string Name, string? Value)[] attributes, object[] content)
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
        return X(local, line, [.. all]);
    }

    /// <summary>An <c>xsl:value-of</c>.</summary>
    internal static XmlElem ValueOf(string? select, string? separator, int line, params object[] content) =>
        Build("value-of", line, [("select", select), ("separator", separator)], content);

    /// <summary>An <c>xsl:text</c>; <paramref name="text"/> is its only child when given.</summary>
    internal static XmlElem Text(string? text, int line, string? disableOutputEscaping = null) =>
        Build("text", line, [("disable-output-escaping", disableOutputEscaping)], text is null ? [] : [text]);

    /// <summary>An <c>xsl:if</c>.</summary>
    internal static XmlElem If(string? test, int line, params object[] content) =>
        Build("if", line, [("test", test)], content);

    /// <summary>An <c>xsl:choose</c>.</summary>
    internal static XmlElem Choose(int line, params object[] content) => Build("choose", line, [], content);

    /// <summary>An <c>xsl:when</c>.</summary>
    internal static XmlElem When(string? test, int line, params object[] content) =>
        Build("when", line, [("test", test)], content);

    /// <summary>An <c>xsl:otherwise</c>.</summary>
    internal static XmlElem Otherwise(int line, params object[] content) => Build("otherwise", line, [], content);

    /// <summary>An <c>xsl:for-each</c>.</summary>
    internal static XmlElem ForEach(string? select, int line, params object[] content) =>
        Build("for-each", line, [("select", select)], content);

    /// <summary>An <c>xsl:sort</c>.</summary>
    internal static XmlElem Sort(
        string? select,
        string? order,
        int line,
        string? stable = null,
        string? dataType = null,
        string? collation = null,
        string? lang = null,
        string? caseOrder = null,
        params object[] content
    ) =>
        Build(
            "sort",
            line,
            [
                ("select", select),
                ("order", order),
                ("stable", stable),
                ("data-type", dataType),
                ("collation", collation),
                ("lang", lang),
                ("case-order", caseOrder),
            ],
            content
        );

    /// <summary>An <c>xsl:message</c>.</summary>
    internal static XmlElem Message(string? select, string? terminate, int line, params object[] content) =>
        Build("message", line, [("select", select), ("terminate", terminate)], content);

    /// <summary>An <c>xsl:sequence</c> with a <see langword="select"/>.</summary>
    internal static XmlElem Seq(string select, int line) => Build("sequence", line, [("select", select)], []);

    /// <summary>An <c>xsl:variable</c> with a <see langword="select"/>.</summary>
    internal static XmlElem Var(string name, string select, int line) =>
        Build("variable", line, [("name", name), ("select", select)], []);
}
