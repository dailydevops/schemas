namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Keys;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

/// <summary>Short builders of the <c>xsl:key</c> elements the key tests use.</summary>
internal static class Kx
{
    /// <summary>An <c>xsl:key</c>; a <see langword="null"/> attribute is left out and <paramref name="name"/> is written as given.</summary>
    internal static XmlElem Key(string? name, string? match, string? use, int line, params object[] content)
    {
        var all = new List<object>();
        if (name is not null)
        {
            all.Add(FunctionTree.A("name", name));
        }

        if (match is not null)
        {
            all.Add(FunctionTree.A("match", match));
        }

        if (use is not null)
        {
            all.Add(FunctionTree.A("use", use));
        }

        all.AddRange(content);
        return FunctionTree.X("key", line, [.. all]);
    }

    /// <summary>An <c>xsl:text</c>, the content form of a key value.</summary>
    internal static XmlElem Text(string value, int line) => FunctionTree.X("text", line, value);
}
