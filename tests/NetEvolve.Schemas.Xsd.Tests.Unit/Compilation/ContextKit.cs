namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Composition;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

/// <summary>A build context over the units of one schema text, for the tests of the component builders that call them directly.</summary>
internal sealed class ContextKit
{
    public ContextKit(string body, XsdVersion version = XsdVersion.V10)
    {
        Collector = new ValidationIssueCollector(ValidationLimits.Default);
        Reporter = new XsdIssueReporter(Collector);
        Registry = new ComponentRegistry(Reporter);
        XsdBuiltInTypes.Register(Registry, version);
        Context = new BuildContext(version, Registry, Reporter);
        Document = SchemaDocumentParser.Parse(Schema(body).AsSpan(), RootUri, Reporter);
        var set = new SchemaDocumentLoader(DenyAllResourceResolver.Instance, ValidationLimits.Default, Collector).Load(
            Document,
            CancellationToken.None
        );
        Units = [new UnitScope(set.Units[0])];
        Context.Index(Units);
    }

    public ValidationIssueCollector Collector { get; }

    public XsdIssueReporter Reporter { get; }

    public ComponentRegistry Registry { get; }

    public BuildContext Context { get; }

    public SchemaDocument Document { get; }

    public List<UnitScope> Units { get; }

    /// <summary>Gets the top-level node at an index of the document.</summary>
    public SchemaSyntaxNode Node(int index) => Units[0].Unit.Components[index];

    public DiagnosticCode[] Codes()
    {
        var issues = Collector.ToResult().Issues;
        var codes = new DiagnosticCode[issues.Count];
        for (var i = 0; i < codes.Length; i++)
        {
            codes[i] = issues[i].Code;
        }

        return codes;
    }
}
