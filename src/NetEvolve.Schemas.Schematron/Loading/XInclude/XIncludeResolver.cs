namespace NetEvolve.Schemas.Schematron.Loading.XInclude;

using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Resolves the XInclude elements of a schema document (stub until the implementation commit).</summary>
internal sealed class XIncludeResolver
{
    private readonly IResourceResolver _resolver;
    private readonly ValidationLimits _limits;

    internal XIncludeResolver(IResourceResolver resolver, ValidationLimits limits)
    {
        Argument.ThrowIfNull(resolver);
        Argument.ThrowIfNull(limits);

        _resolver = resolver;
        _limits = limits;
    }

    internal bool TryResolve(
        XmlNode document,
        string? systemId,
        IProblemSink problems,
        CancellationToken cancellationToken,
        out XmlNode result
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfNull(document.Tree);
        Argument.ThrowIfNull(problems);

        result = document;
        throw NotImplementedFailure.For("xinclude " + systemId + _limits.MaxDepth + _resolver.GetType().Name);
    }
}
