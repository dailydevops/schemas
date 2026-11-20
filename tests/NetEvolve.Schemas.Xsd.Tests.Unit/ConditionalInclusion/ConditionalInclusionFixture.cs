namespace NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.ConditionalInclusion;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>Reads a schema document through the conditional inclusion filter into the syntax model.</summary>
internal static class ConditionalInclusionFixture
{
    public const string Vc = "http://www.w3.org/2007/XMLSchema-versioning";

    public const string Header = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:vc=\"" + Vc + "\">\n";

    public static (SchemaSyntaxNode? Root, IReadOnlyList<ValidationIssue> Issues) Run(
        string text,
        XsdVersion version = XsdVersion.V11
    )
    {
        var reporter = NewReporter(out var collector);
        var inner = new SchemaSyntaxSink(SystemId, reporter);
        var filter = new ConditionalInclusionSink(inner, version, SystemId, reporter);
        using var reader = XmlPipelineReader.Create(text.AsSpan());
        _ = reader.Pump(ref filter);
        return (inner.Root, collector.ToResult().Issues);
    }

    /// <summary>The names (or local names when unnamed) of the children of the root, in document order.</summary>
    public static string Children(SchemaSyntaxNode root)
    {
        var names = new string[root.ChildCount];
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = root.GetChildNode(i).FindAttribute("name") ?? root.GetChildNode(i).Name.LocalName;
        }

        return string.Join(",", names);
    }

    /// <summary>Wraps declarations into a schema document that declares the prefixes <c>xs</c> and <c>vc</c>.</summary>
    public static string Schema(string body) => Header + body + "\n</xs:schema>";

    /// <summary>One element declaration <c>a</c> that carries the given attributes.</summary>
    public static string Element(string attributes) =>
        Schema("<xs:element name=\"a\" type=\"xs:string\" " + attributes + "/>");
}
