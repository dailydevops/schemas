namespace NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Embedding;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Scopes.ScopeModel;

/// <summary>A global with a sequence constructor (#1160) is built by the embedded content of the run, and reported when there is none.</summary>
public class GlobalContentBinderTests
{
    private static readonly XPathNode Doc = PatternFixture.Node("doc");

    private static ScopePlan Plan()
    {
        var schema = Schema(patterns: Pattern("p", [], Rule("r", "/")));
        var problems = new FakeProblemSink();
        _ = PhaseSelector.TryResolve(schema, "#ALL", problems, out var selection);
        GlobalDeclaration[] globals = [new("g", null, null, false, false, Next(), new object())];
        _ = ScopePlanner.TryPlan(schema, selection, globals, new ScopeTestBinding(), problems, out var plan);
        return plan;
    }

    [Test]
    public async Task Binder_WithoutTheEmbeddedContent_ReportsTheUnsupportedConstruct()
    {
        var problems = new FakeProblemSink();
        using var run = new MiniRun();
        var binder = new SlotBinder(Plan(), run, new QueryFocus(Doc, 1, 1), problems);

        var status = binder.EnsureBound([0]);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(ScopeFixture.Codes(problems)).IsEqualTo("NES3204");
    }

    [Test]
    public async Task EmbeddedContent_WithoutACompiledBodyForTheSlot_IsTheUnsupportedProblem()
    {
        using var run = new MiniRun();
        var binder = new SlotBinder(Plan(), run, new QueryFocus(Doc, 1, 1), new FakeProblemSink());

        var status = new EmbeddedContent().TryBindGlobalContent(run, binder, 0, out var problem);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
    }
}
