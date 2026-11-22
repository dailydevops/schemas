namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Documents;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs the document functions the way a host does: the expression is compiled with a language version and host flags, and evaluated in
/// an evaluation context that holds a resolver and, optionally, a context document. A result is rendered as text, an error as
/// <c>error:CODE</c>.
/// </summary>
internal static class DocumentsHarness
{
    internal const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    /// <summary>Resolves a built-in function of the default catalog for a language version and the host flags that are set.</summary>
    internal static bool TryResolve(
        string local,
        int arity,
        out XPathFunction? function,
        XPathVersion version = XPathVersion.V20,
        XPathHostRequirements host = XPathHostRequirements.None
    ) =>
        FunctionCatalog.Default.Registry.TryResolve(
            XPathStaticContext.Default.ToBuilder().WithVersion(version).Build(),
            new XPathQName(FunctionNamespace, local),
            arity,
            host,
            out function
        );

    /// <summary>Evaluates an expression and renders the outcome.</summary>
    internal static string Run(
        string expression,
        IXPathResourceResolver? resolver = null,
        string? contextXml = null,
        XPathVersion version = XPathVersion.V20,
        XPathHostRequirements host = XPathHostRequirements.None
    )
    {
        using var context = new XPathEvaluationContext { ResourceResolver = resolver };
        if (contextXml is not null)
        {
            context.SetContextItem(XdmItem.FromNode(LoadWithDtd(contextXml)));
        }

        return Run(expression, context, version, host);
    }

    /// <summary>Evaluates an expression in a given context and renders the outcome.</summary>
    internal static string Run(
        string expression,
        XPathEvaluationContext context,
        XPathVersion version = XPathVersion.V20,
        XPathHostRequirements host = XPathHostRequirements.None
    )
    {
        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithVersion(version)
            .WithNamespace("fn", FunctionNamespace)
            .Build();
        var compiled = XPathExpression.TryCompile(expression.AsSpan(), statics, host);
        if (!compiled.Success)
        {
            return "error:" + compiled.Diagnostic.Code;
        }

        var outcome = compiled.Expression!.TryEvaluate(context);
        return outcome.Status == XPathEvaluationStatus.Ok ? Render(outcome.Value) : "error:" + outcome.Diagnostic.Code;
    }

    /// <summary>Calls a function instance with arguments given as strings (<c>()</c> is the empty sequence).</summary>
    internal static string Call(XPathFunction function, XPathEvaluationContext context, params string[] arguments)
    {
        var sequences = new XdmSequence[arguments.Length];
        for (var i = 0; i < sequences.Length; i++)
        {
            sequences[i] = string.Equals(arguments[i], "()", StringComparison.Ordinal)
                ? XdmSequence.Empty
                : XdmSequence.Create(XdmItem.FromString(arguments[i]));
        }

        try
        {
            return Render(function.Call(context, sequences));
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }

    /// <summary>
    /// Calls a built-in function of the default catalog for a language version and host flags, the way a compiled call does. The
    /// compiler enables XPath 2.0 only, so the functions of XPath 3.0 and 3.1 are reached through the registry. An unknown function is
    /// <c>error:XPST0017</c>.
    /// </summary>
    internal static string RunFunction(
        string local,
        XPathVersion version,
        XPathHostRequirements host,
        XPathEvaluationContext context,
        params string[] arguments
    ) =>
        TryResolve(local, arguments.Length, out var function, version, host)
            ? Call(function!, context, arguments)
            : "error:XPST0017";

    /// <summary>Loads a document with document type processing and returns its document node.</summary>
    internal static XPathNode LoadWithDtd(string xml) =>
        new(XmlDocumentLoader.Load(xml.AsSpan(), XmlReaderOptions.Default.WithDtdProcessing(true)).Document);

    /// <summary>Renders the items of a value, separated by a bar.</summary>
    internal static string Render(in XdmSequence value)
    {
        var text = new StringBuilder();
        for (var i = 0; i < value.Count; i++)
        {
            _ = text.Append(i > 0 ? "|" : string.Empty).Append(Render(value[i]));
        }

        return text.ToString();
    }

    private static string LocalName(in XPathNode node) => node.Tree!.Names.GetString(node.Node.Name.Local);

    private static string Render(in XdmItem item)
    {
        if (item.TryGetNode(out var node))
        {
            return node.Kind switch
            {
                XdmNodeKind.Document => "doc",
                XdmNodeKind.Element => "e:" + LocalName(node),
                XdmNodeKind.Attribute => "a:" + LocalName(node) + "=" + node.Node.StringValue,
                _ => node.Kind.ToString(),
            };
        }

        if (item.TryGetBoolean(out var boolean))
        {
            return boolean ? "true" : "false";
        }

        if (item.TryGetInteger(out var integer))
        {
            return integer.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return item.TryGetString(out var value) ? value! : item.Kind.ToString();
    }

    /// <summary>A resolver over fixed bytes by resolved URI that records every request.</summary>
    internal sealed class FakeResolver : IXPathResourceResolver
    {
        private readonly Dictionary<string, byte[]> _resources = new(StringComparer.Ordinal);
        private readonly HashSet<string> _denied = new(StringComparer.Ordinal);

        /// <summary>Gets the requests in call order.</summary>
        internal List<(ResourceKind Kind, string Reference, string? BaseUri, string? ResolvedUri)> Requests { get; } =
        [];

        /// <summary>Gets the number of resolver calls.</summary>
        internal int Calls => Requests.Count;

        /// <summary>Serves bytes under an absolute URI.</summary>
        internal FakeResolver Add(string uri, byte[] content)
        {
            _resources[uri] = content;
            return this;
        }

        /// <summary>Serves UTF-8 text under an absolute URI.</summary>
        internal FakeResolver Add(string uri, string content) => Add(uri, new UTF8Encoding(false).GetBytes(content));

        /// <summary>Denies an absolute URI; a URI that is not served is not found.</summary>
        internal FakeResolver Deny(string uri)
        {
            _ = _denied.Add(uri);
            return this;
        }

        /// <inheritdoc />
        public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add((request.Kind, request.Reference, request.BaseUri, request.ResolvedUri));
            var key = request.ResolvedUri ?? request.Reference;
            return _denied.Contains(key) ? ResourceResult.Denied("denied")
                : _resources.TryGetValue(key, out var bytes) ? ResourceResult.Found(bytes, key)
                : ResourceResult.NotFound("not found");
        }
    }
}
