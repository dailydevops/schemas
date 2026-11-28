namespace NetEvolve.Schemas.Schematron.Tests.Unit.Bindings.Registry;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Regex;
using NetEvolve.Schemas.Schematron.Xslt.Library.Core;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The regex groups in the run of the XPath binding (#1087): the run copies them into the host slot of every evaluation, a function
/// body clears them, and the scope of the XSLT bindings offers <c>regex-group()</c>.
/// </summary>
public class XPathRegexRunTests
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

    private static XPathRegexGroups Groups() =>
        new("2026-11", [new TextRange(0, 7), new TextRange(0, 4), new TextRange(5, 2)]);

    [Test]
    public async Task Evaluation_WithGroups_ReadsThemThroughRegexGroup()
    {
        var binding = Binding("xslt2");
        using var run = Begin(binding);
        ((IXsltRegexRun)(object)run).RegexGroups = Groups();

        await Assert.That(Eval(binding, run, "regex-group(2)")).IsEqualTo((QueryStatus.Ok, "11", string.Empty));
        await Assert.That(Eval(binding, run, "regex-group(0)").Text).IsEqualTo("2026-11");
    }

    [Test]
    public async Task Evaluation_WithoutGroups_IsTheEmptyString()
    {
        var binding = Binding("xslt2");
        using var run = Begin(binding);

        await Assert.That(Eval(binding, run, "regex-group(1)")).IsEqualTo((QueryStatus.Ok, string.Empty, string.Empty));
    }

    [Test]
    public async Task FunctionBody_SeesNoGroups_AndTheCallerGetsThemBack()
    {
        var binding = Binding("xslt2");
        using var run = Begin(binding);
        ((IXsltRegexRun)(object)run).RegexGroups = Groups();

        run.EnterFunction();
        var inside = Eval(binding, run, "regex-group(1)").Text;
        run.ExitFunction();

        await Assert.That(inside).IsEmpty();
        await Assert.That(Eval(binding, run, "regex-group(1)").Text).IsEqualTo("2026");
    }

    [Test]
    public async Task Scope_OfAnXsltBinding_OffersRegexGroup()
    {
        await Assert.That(CoreFixture.Scope().IsAvailable(XsltFunctionScope.Name("regex-group"), 1)).IsTrue();
        await Assert.That(CoreFixture.Scope("xpath2").IsAvailable(XsltFunctionScope.Name("regex-group"), 1)).IsFalse();
    }
}
