namespace NetEvolve.Schemas.Schematron.Xslt.Documents;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A function of the XPath package (<c>doc</c>, <c>doc-available</c>, <c>unparsed-text</c>, <c>unparsed-text-available</c>) bound to the
/// schema base URI: the first argument is resolved against it and the catalog function does the rest, so a relative reference never
/// meets the instance, the load goes through the resolver of the context, and the document pool is the one of <c>fn:doc</c>.
/// </summary>
internal sealed class SchemaBoundFunction : XPathFunction
{
    private readonly XPathFunction _inner;
    private readonly string? _schemaBase;

    internal SchemaBoundFunction(string localName, int minArity, int maxArity, Uri? schemaBaseUri)
        : this(SchemaBase.Catalog(localName, minArity), minArity, maxArity, schemaBaseUri) { }

    private SchemaBoundFunction(XPathFunction inner, int minArity, int maxArity, Uri? schemaBaseUri)
        : base(inner.Name, minArity, maxArity, Types(inner, maxArity))
    {
        _inner = inner;
        _schemaBase = SchemaBase.Text(schemaBaseUri);
    }

    /// <inheritdoc />
    public override XPathHostRequirements HostRequirements => _inner.HostRequirements;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments.ToArray();
        if (!arguments[0].IsEmpty)
        {
            _ = arguments[0][0].TryGetString(out var reference);
            arguments[0] = XdmSequence.Create(XdmItem.FromString(SchemaBase.Resolve(reference!, _schemaBase)));
        }

        invocation.Result = _inner.Call(invocation.Context, arguments);
        return XPathEvaluationStatus.Ok;
    }

    private static XPathParameterType[] Types(XPathFunction inner, int count)
    {
        var types = new XPathParameterType[count];
        for (var i = 0; i < count; i++)
        {
            types[i] = inner.ParameterTypeAt(i);
        }

        return types;
    }
}
