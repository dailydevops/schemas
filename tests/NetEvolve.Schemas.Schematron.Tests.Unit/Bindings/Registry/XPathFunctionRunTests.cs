namespace NetEvolve.Schemas.Schematron.Tests.Unit.Bindings.Registry;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Bindings.Xslt1;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Pipeline;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Modules;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The run of the XPath binding as the run of stylesheet functions (#628): the slot values the caller saves and restores, the
/// problem a failed call hands over, the host state slot, and the signature-only host function without a call table.
/// </summary>
public class XPathFunctionRunTests
{
    private const string BaseUri = "file:///schema.sch";

    private static readonly XPathNode Document = new(XmlDocumentLoader.Load("<r/>".AsSpan()).Document);

    private static XPathQueryBinding Xpath2() => new("xpath2", allowsXsltInstructions: false);

    private static IFunctionHostBinding WithG(IFunctionHostBinding binding) =>
        (IFunctionHostBinding)binding.WithFunctions([new XsltUserFunction(new XPathQName("urn:f", "g"), 1)]);

    private static QueryExpression Compile(IQueryBinding binding, string text)
    {
        var scope = new QueryStaticScope(
            [new SchematronNamespace(new DocumentLocation(BaseUri, 1, 1), "f", "urn:f")],
            [],
            BaseUri,
            SchematronEdition.Iso2016,
            null
        );

        return binding.TryCompile(
            text.AsSpan(),
            QueryExpressionKind.Boolean,
            scope,
            out var expression,
            out var problem
        )
            ? expression
            : throw new InvalidOperationException(problem.Message);
    }

    private static XPathQueryRun Begin(IQueryBinding binding) =>
        (XPathQueryRun)binding.BeginRun(new QueryResources(DenyAllResourceResolver.Instance), default);

    private static XsltFunctionHost EmptyHost(XPathQueryRun run) =>
        new(
            new XsltFunctionTableBuilder().Build(),
            new XsltFunctionCaller(
                run,
                new NoBinder(),
                new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames),
                default
            ),
            run
        );

    [Test]
    public async Task Values_AreBoundReadAndCleared_BySlot()
    {
        using var run = Begin(Xpath2());

        var before = run.TryGetValue(40, out _);
        run.SetValue(40, XdmSequence.Create(XdmItem.FromString("x")));
        var bound = run.TryGetValue(40, out var value);
        run.ClearValue(40);
        run.ClearValue(4000);
        var after = run.TryGetValue(40, out _);

        await Assert.That(before).IsFalse();
        await Assert.That(bound).IsTrue();
        await Assert.That(value.Count).IsEqualTo(1);
        await Assert.That(after).IsFalse();
    }

    [Test]
    public async Task FunctionScope_EnteredAndLeft_KeepsTheValues()
    {
        using var run = Begin(Xpath2());
        run.SetValue(0, XdmSequence.Create(XdmItem.FromString("x")));

        run.EnterFunction();
        run.ExitFunction();

        await Assert.That(run.TryGetValue(0, out _)).IsTrue();
    }

    [Test]
    public async Task FailWith_ThenAFailingEvaluation_ReportsTheProblemOfTheEvaluation()
    {
        var binding = Xpath2();
        using var run = Begin(binding);
        var handed = new SchematronProblem(SchematronDiagnosticCodes.EvaluationFailed, "Handed.", default, "FOER0000");
        run.FailWith(in handed);

        var status = run.TryEvaluateBoolean(Compile(binding, "1 idiv 0 = 1"), new QueryFocus(Document, 1, 1), out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(run.GetProblem().Constraint).IsEqualTo("FOAR0001");
    }

    [Test]
    public async Task Call_WithoutTheCallsOfTheRun_IsXPST0017()
    {
        var binding = WithG(Xpath2());
        using var run = Begin(binding);

        var status = run.TryEvaluateBoolean(Compile(binding, "f:g(1)"), new QueryFocus(Document, 1, 1), out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(run.GetProblem().Constraint).IsEqualTo("XPST0017");
    }

    [Test]
    public async Task Call_OfAFunctionThatTheTableLacks_IsXPST0017_AlsoWhenTheCallsAreSetAfterAnEvaluation()
    {
        var binding = WithG(new XPath10QueryBinding("xslt", allowsXsltInstructions: true));
        using var run = Begin(binding);
        _ = run.TryEvaluateBoolean(Compile(binding, "true()"), new QueryFocus(Document, 1, 1), out _);
        run.FunctionCalls = EmptyHost(run);

        var status = run.TryEvaluateBoolean(Compile(binding, "f:g(1)"), new QueryFocus(Document, 1, 1), out _);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(run.GetProblem().Constraint).IsEqualTo("XPST0017");
        await Assert.That(run.FunctionCalls).IsNotNull();
    }

    [Test]
    public async Task WithFunctions_DoesNotChangeTheSharedBinding()
    {
        var shared = Xpath2();
        var derived = WithG(shared);

        var sharedCompiles = shared.TryCompile(
            "f:g(1)".AsSpan(),
            QueryExpressionKind.Boolean,
            new QueryStaticScope([], [], BaseUri, SchematronEdition.Iso2016, null),
            out _,
            out _
        );

        await Assert.That(sharedCompiles).IsFalse();
        await Assert.That(derived.Name).IsEqualTo("xpath2");
    }

    [Test]
    public async Task WithSignatures_ABindingWithoutHostFunctions_OrAModuleSetWithoutFunctions_ReturnsTheBinding()
    {
        var schema = new SchematronSchema(
            new DocumentLocation(BaseUri, 1, 1),
            null,
            [],
            [],
            [],
            [],
            null,
            null,
            null,
            null,
            null,
            SchematronEdition.Iso2016,
            null,
            null,
            [],
            [],
            [],
            []
        );
        var plain = new MiniBinding();
        var host = Xpath2();

        await Assert.That(XsltDeclarations.WithSignatures(plain, schema, XsltModuleSet.Empty)).IsSameReferenceAs(plain);
        await Assert.That(XsltDeclarations.WithSignatures(host, schema, XsltModuleSet.Empty)).IsSameReferenceAs(host);
    }

    private sealed class NoBinder : ISlotBinder
    {
        public QueryStatus EnsureBound(ReadOnlySpan<int> slots) => QueryStatus.Ok;
    }
}
