namespace NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Edition2025;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Parsing.Core;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Parsing.Core.TestTree;

/// <summary>Shared documents and parse calls of the 2025 edition parser tests.</summary>
internal static class Edition2025Fixture
{
    internal const string XslNamespace = "http://www.w3.org/1999/XSL/Transform";

    /// <summary>Creates an element in the XSLT namespace.</summary>
    internal static XmlElem Xsl(string local, int line, int column, params object[] content) =>
        new(local, XslNamespace, line, column, content);

    /// <summary>Creates a <c>schema</c> element at 1:1 with the given edition and content.</summary>
    internal static XmlElem Schema(string? edition, params object[] content) =>
        edition is null ? E("schema", 1, 1, content) : E("schema", 1, 1, [A("schematronEdition", edition), .. content]);

    /// <summary>Creates a <c>schema</c> element of the 2025 edition.</summary>
    internal static XmlElem Schema2025(params object[] content) => Schema("2025", content);

    /// <summary>Creates a <c>schema</c> element of the 2025 edition with a query binding.</summary>
    internal static XmlElem SchemaWithBinding(string queryBinding, params object[] content) =>
        E("schema", 1, 1, [A("schematronEdition", "2025"), A("queryBinding", queryBinding), .. content]);

    internal static (SchematronSchema? Schema, List<SchematronProblem> Problems, bool Result) Parse(XmlElem root) =>
        CoreParserFixture.Parse(root);

    /// <summary>Parses a document that must succeed.</summary>
    internal static SchematronSchema ParseValid(XmlElem root)
    {
        var (schema, problems, result) = Parse(root);
        return result && schema is not null
            ? schema
            : throw new InvalidOperationException("The document must parse; problems: " + problems.Count);
    }

    internal static (SchematronLibrary? Library, List<SchematronProblem> Problems, bool Result) ParseLibrary(
        XmlElem root,
        SchematronEdition edition = SchematronEdition.Iso2025,
        bool xsltBinding = true
    )
    {
        var problems = new FakeProblemSink();
        var result = SchematronCoreParser.TryParseLibrary(
            Build(root),
            CoreParserFixture.SystemId,
            edition,
            xsltBinding,
            problems,
            out var library
        );
        return (library, problems.Problems, result);
    }

    internal static async Task AssertLocation(SchematronNode node, int line, int column)
    {
        await Assert.That(node.Location.Source).IsEqualTo(CoreParserFixture.SystemId);
        await Assert.That(node.Location.Line).IsEqualTo(line);
        await Assert.That(node.Location.Column).IsEqualTo(column);
    }

    /// <summary>Asserts that the parse fails with exactly one problem of the code at the position.</summary>
    internal static async Task AssertSingle(XmlElem root, DiagnosticCode code, int line, int column)
    {
        var (schema, problems, result) = Parse(root);

        await Assert.That(result).IsFalse();
        await Assert.That(schema).IsNull();
        await AssertProblem(problems, code, line, column);
    }

    internal static async Task AssertProblem(
        List<SchematronProblem> problems,
        DiagnosticCode code,
        int line,
        int column
    )
    {
        await Assert.That(problems.Count).IsEqualTo(1);
        await Assert.That(problems[0].Code).IsEqualTo(code);
        await Assert.That(problems[0].Location.Source).IsEqualTo(CoreParserFixture.SystemId);
        await Assert.That(problems[0].Location.Line).IsEqualTo(line);
        await Assert.That(problems[0].Location.Column).IsEqualTo(column);
        await Assert.That(problems[0].Message).IsNotEmpty();
    }
}
