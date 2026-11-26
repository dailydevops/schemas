namespace NetEvolve.Schemas.Schematron.Tests.Unit.Bindings.Registry;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The keys host slot of the run of the XPath binding (#776): a provider assigned before or after the first evaluation reaches the
/// evaluation contexts of the run.
/// </summary>
public class XPathKeyRunTests
{
    private const string BaseUri = "file:///schema.sch";

    private static readonly XPathNode Document = new(
        NetEvolve.Schemas.Xml.Model.Loading.XmlDocumentLoader.Load("<r/>".AsSpan()).Document
    );

    private sealed class EmptyKeys : IXPathKeyProvider
    {
        public bool TryLookup(
            in XPathQName name,
            in XdmSequence values,
            in XPathNode node,
            bool subtree,
            out XdmSequence nodes
        )
        {
            nodes = XdmSequence.Empty;
            return true;
        }
    }

    private static QueryExpression Compile(XPathQueryBinding binding, string text) =>
        binding.TryCompile(
            text.AsSpan(),
            QueryExpressionKind.Boolean,
            new QueryStaticScope([], [], BaseUri, SchematronEdition.Iso2016, null),
            out var expression,
            out var problem
        )
            ? expression
            : throw new InvalidOperationException(problem.Message);

    private static XPathQueryRun Begin(XPathQueryBinding binding) =>
        (XPathQueryRun)binding.BeginRun(new QueryResources(DenyAllResourceResolver.Instance), default);

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Keys_AssignedBeforeOrAfterAnEvaluation_AreReadBack(bool afterEvaluation)
    {
        var binding = new XPathQueryBinding("xpath2", allowsXsltInstructions: false);
        var expression = Compile(binding, "true()");
        using var run = Begin(binding);
        var keys = new EmptyKeys();
        if (afterEvaluation)
        {
            _ = run.TryEvaluateBoolean(expression, new QueryFocus(Document, 1, 1), out _);
        }

        run.Keys = keys;
        var status = run.TryEvaluateBoolean(expression, new QueryFocus(Document, 1, 1), out var value);

        await Assert.That(run.Keys).IsSameReferenceAs(keys);
        await Assert.That(status).IsEqualTo(QueryStatus.Ok);
        await Assert.That(value).IsTrue();
    }
}
