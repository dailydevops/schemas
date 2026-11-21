namespace NetEvolve.Schemas.Xsd.Compilation;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The composition root of the schema compilation pipeline (design #146 DP-5).</summary>
internal sealed class SchemaCompiler
{
    internal SchemaCompiler(XsdVersion version, IResourceResolver resolver, ValidationLimits limits)
    {
        Version = version;
        Resolver = resolver;
        Limits = limits;
    }

    internal XsdVersion Version { get; }

    internal IResourceResolver Resolver { get; }

    internal ValidationLimits Limits { get; }

    /// <summary>Gets the step instances the pipeline composes.</summary>
    internal IReadOnlyList<object> Steps { get; } = [];

    internal SchemaCompilation Compile(
        ReadOnlySpan<char> schema,
        string? baseUri = null,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException("The pipeline is not implemented yet.");

    internal SchemaCompilation Compile(
        ReadOnlySpan<byte> schema,
        string? baseUri = null,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException("The pipeline is not implemented yet.");

    internal SchemaCompilation Compile(
        IReadOnlyList<SchemaInput> inputs,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException("The pipeline is not implemented yet.");
}
