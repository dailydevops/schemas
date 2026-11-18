namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;

/// <summary>Short builders of the XSLT elements the function tests use.</summary>
internal static class Fx
{
    /// <summary>An <c>xsl:function</c>; <paramref name="type"/> is the <c>as</c> attribute and <paramref name="name"/> is written as given.</summary>
    internal static XmlElem Function(string? name, string? type, int line, params object[] content)
    {
        var all = new List<object>();
        if (name is not null)
        {
            all.Add(A("name", name));
        }

        if (type is not null)
        {
            all.Add(A("as", type));
        }

        all.AddRange(content);
        return X("function", line, [.. all]);
    }

    /// <summary>An <c>xsl:param</c>.</summary>
    internal static XmlElem Param(string? name, string? type, int line, params object[] content)
    {
        var all = new List<object>();
        if (name is not null)
        {
            all.Add(A("name", name));
        }

        if (type is not null)
        {
            all.Add(A("as", type));
        }

        all.AddRange(content);
        return X("param", line, [.. all]);
    }

    /// <summary>An <c>xsl:variable</c> with a <c>select</c>.</summary>
    internal static XmlElem Variable(string name, string? select, string? type, int line, params object[] content)
    {
        var all = new List<object> { A("name", name) };
        if (select is not null)
        {
            all.Add(A("select", select));
        }

        if (type is not null)
        {
            all.Add(A("as", type));
        }

        all.AddRange(content);
        return X("variable", line, [.. all]);
    }

    /// <summary>An <c>xsl:sequence</c>.</summary>
    internal static XmlElem Sequence(string? select, int line, params object[] content)
    {
        var all = new List<object>();
        if (select is not null)
        {
            all.Add(A("select", select));
        }

        all.AddRange(content);
        return X("sequence", line, [.. all]);
    }

    /// <summary>The function <c>f:name</c> with one <c>xs:integer</c> parameter per name and one <c>xsl:sequence</c> as body.</summary>
    internal static XmlElem Simple(string name, string type, string[] parameters, string select, int line = 10)
    {
        var content = new List<object>();
        for (var i = 0; i < parameters.Length; i++)
        {
            content.Add(Param(parameters[i], "xs:integer", line + 1 + i));
        }

        content.Add(Sequence(select, line + 1 + parameters.Length));
        return Function("f:" + name, type, line, [.. content]);
    }
}
