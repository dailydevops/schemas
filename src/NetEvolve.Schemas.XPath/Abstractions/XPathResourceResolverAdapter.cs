namespace NetEvolve.Schemas.XPath.Abstractions;

using System.Threading;
using NetEvolve.Schemas.Resources;

/// <summary>Wraps a core resolver into the XPath marker; Schematron and Xsd use it to hand their resolver to XPath.</summary>
internal sealed class XPathResourceResolverAdapter(IResourceResolver inner) : IXPathResourceResolver
{
    /// <summary>Wraps a resolver.</summary>
    /// <param name="resolver">The core resolver, <see langword="null"/> for none.</param>
    /// <returns>The resolver itself when it already is the marker, the adapter otherwise, <see langword="null"/> for none.</returns>
    internal static IXPathResourceResolver? Wrap(IResourceResolver? resolver) =>
        resolver switch
        {
            null => null,
            IXPathResourceResolver marked => marked,
            _ => new XPathResourceResolverAdapter(resolver),
        };

    /// <inheritdoc />
    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken) =>
        inner.Resolve(in request, cancellationToken);
}
