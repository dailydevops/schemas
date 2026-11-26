namespace NetEvolve.Schemas.Schematron.Tests.Unit.Bindings.Registry;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core;
using NetEvolve.Schemas.Schematron.Xslt.Library.Core;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The current group and the grouping key in the run of the XPath binding (#803): the run copies both values into the host slots of
/// every evaluation, a function body clears them, and the scope of the XSLT bindings offers <c>current-group()</c> and
/// <c>current-grouping-key()</c>.
/// </summary>
public class XPathGroupRunTests
{
    private const string BaseUri = "file:///schema.sch";

    private static readonly XPathNode Document = new(XmlDocumentLoader.Load("<r/>".AsSpan()).Document);

    private static XPathQueryBinding Binding(string name) =>
        new(
            name,
            allowsXsltInstructions: true,
            extension: (IQueryBinding binding, XPathStaticContext statics, out XPathHostRequirements host) =>
            {
                var scope = new XsltFunctionScope(binding, statics);
                host = scope.ActiveHost;
                return scope.StaticContext;
            }
        );

    private static (QueryStatus Status, string Text, string Constraint) Eval(
        XPathQueryBinding binding,
        XPathQueryRun run,
        string text
    )
    {
        _ = binding.TryCompile(
            text.AsSpan(),
            QueryExpressionKind.Value,
            new QueryStaticScope([], [], BaseUri, SchematronEdition.Iso2016, null),
            out var expression,
            out var problem
        );
        var sink = new TextSink();
        var status = run.TryEvaluate(expression, new QueryFocus(Document, 1, 1), sink);
        return (
            status,
            sink.Text,
            status == QueryStatus.Ok ? string.Empty : run.GetProblem().Constraint ?? problem.Constraint ?? string.Empty
        );
    }

    private sealed class TextSink : ISequenceSink
    {
        private readonly System.Text.StringBuilder _text = new();

        internal string Text => _text.ToString();

        public SequenceSinkKind Kind => SequenceSinkKind.Items;

        public NetEvolve.Schemas.Xml.Contracts.IXmlSink Content => throw new NotSupportedException();

        void ISequenceSink.Item(in XdmItem item)
        {
            if (item.TryGetString(out var value))
            {
                _ = _text.Append(value);
            }
        }

        void ISequenceSink.Text(ReadOnlySpan<char> text) { }

        public NetEvolve.Schemas.Xml.Contracts.IXmlSink BeginTree() => throw new NotSupportedException();

        public XdmItem EndTree(NetEvolve.Schemas.Xml.Contracts.IXmlSink tree) => throw new NotSupportedException();
    }

    private static XPathQueryRun Begin(XPathQueryBinding binding) =>
        (XPathQueryRun)binding.BeginRun(new QueryResources(DenyAllResourceResolver.Instance), default);

    private static XdmSequence Group() => XdmSequence.Create([XdmItem.FromString("a"), XdmItem.FromString("b")]);

    [Test]
    public async Task Evaluation_WithAGroupAndAKey_ReadsBothThroughTheFunctions()
    {
        var binding = Binding("xslt2");
        using var run = Begin(binding);
        run.CurrentGroup = Group();
        run.CurrentGroupingKey = XdmSequence.Create(XdmItem.FromString("k"));

        var group = Eval(binding, run, "string(count(current-group()))");
        var key = Eval(binding, run, "current-grouping-key()");

        await Assert.That(group).IsEqualTo((QueryStatus.Ok, "2", string.Empty));
        await Assert.That(key).IsEqualTo((QueryStatus.Ok, "k", string.Empty));
    }

    [Test]
    public async Task Evaluation_WithoutAGroup_IsEmptyUnderXslt2()
    {
        var binding = Binding("xslt2");
        using var run = Begin(binding);

        await Assert.That(Eval(binding, run, "string(count(current-group()))").Text).IsEqualTo("0");
        await Assert.That(Eval(binding, run, "string(count(current-grouping-key()))").Text).IsEqualTo("0");
    }

    [Test]
    [Arguments("current-group()", "XTDE1061")]
    [Arguments("current-grouping-key()", "XTDE1071")]
    public async Task Evaluation_WithoutAGroup_FailsUnderXslt3(string text, string code)
    {
        var binding = Binding("xslt3");
        using var run = Begin(binding);

        var (status, _, constraint) = Eval(binding, run, text);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(constraint).IsEqualTo(code);
    }

    [Test]
    public async Task Evaluation_AfterTheGroupIsCleared_SeesNoGroup()
    {
        var binding = Binding("xslt2");
        using var run = Begin(binding);
        run.CurrentGroup = Group();
        _ = Eval(binding, run, "string(count(current-group()))");
        run.CurrentGroup = null;

        await Assert.That(Eval(binding, run, "string(count(current-group()))").Text).IsEqualTo("0");
    }

    [Test]
    public async Task FunctionBody_SeesNoGroup_AndTheCallerGetsItBack()
    {
        var binding = Binding("xslt2");
        using var run = Begin(binding);
        run.CurrentGroup = Group();
        run.CurrentGroupingKey = XdmSequence.Create(XdmItem.FromString("k"));

        run.EnterFunction();
        var inside = Eval(binding, run, "string(count(current-group()))").Text;
        var insideKey = Eval(binding, run, "string(count(current-grouping-key()))").Text;
        run.ExitFunction();

        await Assert.That(inside).IsEqualTo("0");
        await Assert.That(insideKey).IsEqualTo("0");
        await Assert.That(Eval(binding, run, "string(count(current-group()))").Text).IsEqualTo("2");
        await Assert.That(Eval(binding, run, "current-grouping-key()").Text).IsEqualTo("k");
    }

    [Test]
    public async Task Scope_OfAnXsltBinding_OffersTheGroupingFunctions()
    {
        var scope = CoreFixture.Scope();

        await Assert.That(scope.IsAvailable(XsltFunctionScope.Name("current-group"), 0)).IsTrue();
        await Assert.That(scope.IsAvailable(XsltFunctionScope.Name("current-grouping-key"), 0)).IsTrue();
        await Assert
            .That(CoreFixture.Scope("xpath2").IsAvailable(XsltFunctionScope.Name("current-group"), 0))
            .IsFalse();
    }
}
