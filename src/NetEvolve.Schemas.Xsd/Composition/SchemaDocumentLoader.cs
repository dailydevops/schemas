namespace NetEvolve.Schemas.Xsd.Composition;

using System;
using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>Stub of the loader; the implementation follows in the next commit.</summary>
internal sealed class SchemaDocumentLoader
{
    internal SchemaDocumentLoader(
        IResourceResolver resolver,
        ValidationLimits limits,
        ValidationIssueCollector collector
    )
    {
        Argument.ThrowIfNull(resolver);
        Argument.ThrowIfNull(limits);
        Argument.ThrowIfNull(collector);
    }

    internal SchemaDocumentSet Load(SchemaDocument root, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
