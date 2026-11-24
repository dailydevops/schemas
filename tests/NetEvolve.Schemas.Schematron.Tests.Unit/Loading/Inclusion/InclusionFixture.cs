namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Loading.Inclusion;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Parsing.Core;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Xslt.Modules;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Reader.Pipeline;

/// <summary>The documents and the resolve call of the inclusion tests.</summary>
internal static class InclusionFixture
{
    internal const string Ns = "http://purl.oclc.org/dsdl/schematron";
    internal const string RootId = "file:///s/root.sch";
    internal const string Edition2025 = " schematronEdition=\"2025\"";

    /// <summary>Wraps a body in an element of the Schematron namespace.</summary>
    internal static string El(string name, string attributes, string body) =>
        $"<{name} xmlns=\"{Ns}\"{attributes}>{body}</{name}>";

    internal static string Schema(string body, string attributes = "") => El("schema", attributes, body);

    internal static string Library(string body) => El("library", string.Empty, body);

    internal static string Pattern(string id, string body = "") => El("pattern", $" id=\"{id}\"", body);

    internal static string Rule(string context, string body) => El("rule", $" context=\"{context}\"", body);

    internal static string AssertEl(string test, string text = "m") => El("assert", $" test=\"{test}\"", text);

    internal static string Include(string href) => $"<include href=\"{href}\"/>";

    internal static XmlNode Document(string xml)
    {
        using var reader = XmlPipelineReader.Create(Encoding.UTF8.GetBytes(xml));
        using var builder = new XmlTreeBuilder();
        var sink = builder;
        if (reader.Pump(ref sink) != XmlReadStatus.Completed)
        {
            throw new InvalidOperationException("The test document is not well formed: " + reader.Diagnostic?.Message);
        }

        return builder.Complete().Document;
    }

    internal static ParsedModule Parse(string xml, string? systemId = RootId)
    {
        var problems = new FakeProblemSink();
        return SchematronCoreParser.TryParse(Document(xml), systemId, problems, out var schema) && schema is not null
            ? new ParsedModule(schema)
            : throw new InvalidOperationException("The test schema must parse; problems: " + problems.Problems.Count);
    }

    internal static Outcome Resolve(
        string root,
        IResourceResolver resources,
        ValidationLimits? limits = null,
        string? systemId = RootId
    )
    {
        var problems = new FakeProblemSink();
        var loader = new ModuleLoader(resources, limits ?? ValidationLimits.Default);
        var ok = new ModelResolver().TryResolve(Parse(root, systemId), loader, problems, out var model);
        return new Outcome(ok, model.Schema, problems.Problems, model.Modules);
    }

    internal sealed class Outcome(
        bool ok,
        SchematronSchema schema,
        List<SchematronProblem> problems,
        XsltModuleSet modules
    )
    {
        internal bool Ok { get; } = ok;

        internal SchematronSchema Schema { get; } = schema;

        internal List<SchematronProblem> Problems { get; } = problems;

        internal XsltModuleSet Modules { get; } = modules;
    }
}
