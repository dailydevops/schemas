namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Documents;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Xslt.Documents;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The harness of the document function tests: the functions of one schema base URI, one call of a function by name over a context
/// that holds a resolver, and the outcome as text: <c>error:CODE</c>, <c>empty</c>, <c>boolean:x</c>, <c>string:x</c>, or <c>nodes:N</c>.
/// </summary>
internal static class DocumentsFixture
{
    internal const string SchemaBase = "file:///schemas/rules/main.sch";

    internal static XPathFunction[] Functions(string? schemaBase = SchemaBase) =>
        XsltDocumentFunctions.Create(schemaBase is null ? null : new Uri(schemaBase));

    internal static XPathFunction Find(XPathFunction[] functions, string local, int arity)
    {
        foreach (var function in functions)
        {
            if (
                string.Equals(function.Name.LocalName, local, StringComparison.Ordinal)
                && arity >= function.MinArity
                && arity <= function.MaxArity
            )
            {
                return function;
            }
        }

        throw new InvalidOperationException("No function " + local + "/" + arity);
    }

    internal static XdmSequence Sequence(params object[] items)
    {
        var values = new XdmItem[items.Length];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = items[i] is XPathNode node ? XdmItem.FromNode(node) : XdmItem.FromString((string)items[i]);
        }

        return XdmSequence.Create(values);
    }

    internal static XdmSequence Call(
        string local,
        XPathEvaluationContext context,
        XPathFunction[] functions,
        params XdmSequence[] arguments
    ) => Find(functions, local, arguments.Length).Call(context, arguments);

    internal static string Run(
        string local,
        XPathEvaluationContext context,
        XPathFunction[] functions,
        params XdmSequence[] arguments
    )
    {
        try
        {
            return Render(Call(local, context, functions, arguments));
        }
        catch (XPathException failure)
        {
            return "error:" + failure.Diagnostic.Code;
        }
    }

    internal static XPathNode NodeWithBase(string baseUri) =>
        new(XmlDocumentLoader.Load("<n/>".AsSpan(), null, new Uri(baseUri)).Document);

    internal static string Render(in XdmSequence result)
    {
        if (result.IsEmpty)
        {
            return "empty";
        }

        var item = result[0];
        if (item.TryGetNode(out _))
        {
            return "nodes:" + result.Count;
        }

        if (item.Kind == XdmItemKind.Boolean && item.TryGetBoolean(out var flag))
        {
            return "boolean:" + (flag ? "true" : "false");
        }

        return item.TryGetString(out var text) ? "string:" + text : "other";
    }

    internal static IXPathResourceResolver Denying() =>
        XPathResourceResolverAdapter.Wrap(DenyAllResourceResolver.Instance)!;

    internal sealed class Resolver : IXPathResourceResolver
    {
        private readonly Dictionary<string, byte[]> _resources = new(StringComparer.Ordinal);

        internal List<string> Requests { get; } = [];

        internal Resolver Add(string uri, string content) => Add(uri, new UTF8Encoding(false).GetBytes(content));

        internal Resolver Add(string uri, byte[] content)
        {
            _resources[uri] = content;
            return this;
        }

        public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = request.ResolvedUri ?? request.Reference;
            Requests.Add(key);
            return _resources.TryGetValue(key, out var bytes)
                ? ResourceResult.Found(bytes, key)
                : ResourceResult.NotFound("not found");
        }
    }
}
