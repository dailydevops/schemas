namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Embedding;

using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;
using NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Embedding;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Scopes.ScopeModel;

/// <summary>A <see langword="let"/> with element content is a slot of its level that the binder fills lazily with the temporary tree.</summary>
public class LetContentSlotTests
{
    private static readonly XPathNode Doc = PatternFixture.Node("doc");

    private static SchematronLet ContentLet(string name, string? type = null, string? value = null)
    {
        SchematronNode[] content = [new SchematronForeignElement(Next(), string.Empty, "root", [], [])];
        return new SchematronLet(Next(), name, type, value ?? string.Empty, content);
    }

    private sealed class CountingContent : IEmbeddedContent
    {
        internal int Binds { get; private set; }

        internal int LastSlot { get; private set; } = -1;

        public QueryStatus TryBindLetContent(
            IQueryRun run,
            int slot,
            IReadOnlyList<SchematronNode> content,
            out SchematronProblem problem
        )
        {
            Binds++;
            LastSlot = slot;
            problem = default;
            return QueryStatus.Ok;
        }
    }

    private sealed class FailingContent : IEmbeddedContent
    {
        public QueryStatus TryBindLetContent(
            IQueryRun run,
            int slot,
            IReadOnlyList<SchematronNode> content,
            out SchematronProblem problem
        )
        {
            problem = new SchematronProblem(
                SchematronDiagnosticCodes.EvaluationFailed,
                "content failed",
                default,
                "XTDE0420"
            );
            return QueryStatus.Problem;
        }
    }

    [Test]
    public async Task Plan_OfAContentLet_HasASlotWithContentAndNoValue()
    {
        var fixture = ScopeFixture.Create(Schema(lets: [ContentLet("tree")]));

        await Assert.That(fixture.Ok).IsTrue();
        await Assert.That(fixture.Plan.Globals.Count).IsEqualTo(1);
        var slot = fixture.Plan.Globals[0];
        await Assert.That(slot.Name).IsEqualTo("tree");
        await Assert.That(slot.Content).IsNotNull();
        await Assert.That(slot.Value).IsNull();
        await Assert.That(fixture.Binding.Compiled.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Plan_OfAContentLet_MakesTheNameVisibleToLaterValues()
    {
        var fixture = ScopeFixture.Create(Schema(lets: [ContentLet("tree"), Let("n", "count($tree/root)")]));

        await Assert.That(fixture.Ok).IsTrue();
        await Assert.That(ScopeFixture.Visible(fixture.Binding.Compiled[0].Scope)).IsEqualTo("tree");
    }

    [Test]
    public async Task Plan_OfAContentLetWithAType_IsTheUnsupportedError()
    {
        var fixture = ScopeFixture.Create(Schema(lets: [ContentLet("tree", "xs:string")]));

        await Assert.That(fixture.Ok).IsFalse();
        await Assert.That(ScopeFixture.Codes(fixture.Problems)).IsEqualTo("NES3007");
    }

    [Test]
    public async Task Plan_OfAContentLet_AtPatternAndRuleLevel_KeepsTheContent()
    {
        var fixture = ScopeFixture.Create(
            Schema(patterns: Pattern("p", [ContentLet("a")], Rule("r", "x", ContentLet("b"))))
        );

        await Assert.That(fixture.Ok).IsTrue();
        await Assert.That(fixture.Plan.Patterns[0].Lets[0].Content).IsNotNull();
        await Assert.That(fixture.Plan.Patterns[0].Rules[0].Lets[0].Content).IsNotNull();
    }

    [Test]
    public async Task Binder_BindsTheContentLazilyAndOnce()
    {
        var fixture = ScopeFixture.Create(Schema(lets: [Let("z", "1"), ContentLet("tree")]));
        var embedded = new CountingContent();
        using var run = new MiniRun();
        var binder = new SlotBinder(fixture.Plan, run, new QueryFocus(Doc, 1, 1), new FakeProblemSink(), embedded);

        await Assert.That(embedded.Binds).IsEqualTo(0);
        var first = binder.EnsureBound([1]);
        var second = binder.EnsureBound([1]);

        await Assert.That(first).IsEqualTo(QueryStatus.Ok);
        await Assert.That(second).IsEqualTo(QueryStatus.Ok);
        await Assert.That(embedded.Binds).IsEqualTo(1);
        await Assert.That(embedded.LastSlot).IsEqualTo(1);
    }

    [Test]
    public async Task Binder_BindsTheRealTreeIntoTheRun()
    {
        var fixture = ScopeFixture.Create(Schema(lets: [ContentLet("tree")]));
        using var run = new MiniRun();
        var binder = new SlotBinder(
            fixture.Plan,
            run,
            new QueryFocus(Doc, 1, 1),
            new FakeProblemSink(),
            new EmbeddedContent()
        );

        var status = binder.EnsureBound([0]);

        _ = run.TryGetValue(0, out var value);
        _ = value[0].TryGetNode(out var node);
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(node.Kind).IsEqualTo(XdmNodeKind.Document);
    }

    [Test]
    public async Task Binder_RuleLevelContent_IsBoundOncePerFiring()
    {
        var fixture = ScopeFixture.Create(Schema(patterns: Pattern("p", [], Rule("r", "x", ContentLet("tree")))));
        var embedded = new CountingContent();
        using var run = new MiniRun();
        var binder = new SlotBinder(fixture.Plan, run, new QueryFocus(Doc, 1, 1), new FakeProblemSink(), embedded);
        var slot = fixture.Plan.RuleStart;
        binder.BeginPattern(fixture.Plan.Patterns[0]);

        binder.BeginFiring(fixture.Plan.Patterns[0].Rules[0], new QueryFocus(Doc, 1, 1));
        _ = binder.EnsureBound([slot]);
        _ = binder.EnsureBound([slot]);
        binder.BeginFiring(fixture.Plan.Patterns[0].Rules[0], new QueryFocus(Doc, 1, 1));
        _ = binder.EnsureBound([slot]);

        await Assert.That(embedded.Binds).IsEqualTo(2);
    }

    [Test]
    public async Task Binder_WithoutTheEmbeddedContent_IsTheUnsupportedConstructErrorOnceAtTheFirstRead()
    {
        var fixture = ScopeFixture.Create(Schema(lets: [ContentLet("tree")]));
        var problems = new FakeProblemSink();
        using var run = new MiniRun();
        var binder = new SlotBinder(fixture.Plan, run, new QueryFocus(Doc, 1, 1), problems);

        var first = binder.EnsureBound([0]);
        var second = binder.EnsureBound([0]);

        await Assert.That(first).IsEqualTo(QueryStatus.Problem);
        await Assert.That(second).IsEqualTo(QueryStatus.Problem);
        await Assert.That(ScopeFixture.Codes(problems)).IsEqualTo("NES3204");
    }

    [Test]
    public async Task Binder_ContentThatFails_ReportsItsProblemOnceAndMemoizesTheFailure()
    {
        var fixture = ScopeFixture.Create(Schema(lets: [ContentLet("tree")]));
        var problems = new FakeProblemSink();
        using var run = new MiniRun();
        var binder = new SlotBinder(fixture.Plan, run, new QueryFocus(Doc, 1, 1), problems, new FailingContent());

        var first = binder.EnsureBound([0]);
        var second = binder.EnsureBound([0]);

        await Assert.That(first).IsEqualTo(QueryStatus.Problem);
        await Assert.That(second).IsEqualTo(QueryStatus.Problem);
        await Assert.That(problems.Problems.Count).IsEqualTo(1);
        await Assert.That(problems.Problems[0].Constraint).IsEqualTo("XTDE0420");
    }
}
