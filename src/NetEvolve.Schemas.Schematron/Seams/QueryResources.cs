namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.XPath.Abstractions;

/// <summary>
/// The resolver adapter handed to every run of one validator. It wraps the core <see cref="IResourceResolver"/> once, so
/// <c>document()</c> stays denied under the default resolver and no third resolver interface exists. It carries the limits of the
/// validator, which cap the documents and texts that the expressions of a run load.
/// </summary>
internal sealed class QueryResources
{
    internal QueryResources(IResourceResolver resolver, ValidationLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        Resolver = resolver;
        Limits = limits ?? ValidationLimits.Default;
        XPathResolver = XPathResourceResolverAdapter.Wrap(resolver)!;
    }

    internal IResourceResolver Resolver { get; }

    internal ValidationLimits Limits { get; }

    internal IXPathResourceResolver XPathResolver { get; }
}
