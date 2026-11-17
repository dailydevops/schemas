namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.XPath.Abstractions;

/// <summary>
/// The resolver adapter handed to every run of one validator. It wraps the core <see cref="IResourceResolver"/> once, so
/// <c>document()</c> stays denied under the default resolver and no third resolver interface exists.
/// </summary>
internal sealed class QueryResources
{
    internal QueryResources(IResourceResolver resolver)
    {
        Argument.ThrowIfNull(resolver);

        Resolver = resolver;
        XPathResolver = XPathResourceResolverAdapter.Wrap(resolver)!;
    }

    internal IResourceResolver Resolver { get; }

    internal IXPathResourceResolver XPathResolver { get; }
}
