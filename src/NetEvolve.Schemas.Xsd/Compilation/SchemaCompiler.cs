namespace NetEvolve.Schemas.Xsd.Compilation;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The composition root of the schema compilation pipeline (design #146 DP-5): turns schema documents into a checked, immutable
/// schema set by composing the real component builders, the resolver, the constraint checks, and the content model compiler.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline order is read, compose, build components, resolve, check constraints, compile content models (Structures 5.1 and
/// 3.15). Every schema error is collected with its location and rule code instead of the first one stopping the run; the result
/// carries a set only when there is none. A schema document comes from a span, from memory, or through the resolver for the
/// documents that it includes, imports, redefines, or overrides.
/// </para>
/// <para>
/// A compiler holds no state between calls and is safe to share; each call runs one <see cref="CompilationRun"/>. See
/// <see cref="CompilationRun"/> for the limitations of the pipeline.
/// </para>
/// </remarks>
internal sealed class SchemaCompiler
{
    private readonly XsdVersion _version;
    private readonly IResourceResolver _resolver;
    private readonly ValidationLimits _limits;

    /// <summary>Initializes a new instance of the <see cref="SchemaCompiler"/> class.</summary>
    /// <param name="version">The XSD version; <see cref="XsdVersion.V10"/> is strict.</param>
    /// <param name="resolver">The resolver for the documents that a schema document refers to.</param>
    /// <param name="limits">The limits of the compilation.</param>
    internal SchemaCompiler(XsdVersion version, IResourceResolver resolver, ValidationLimits limits)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(limits);

        _version = version;
        _resolver = resolver;
        _limits = limits;
    }

    /// <summary>Gets the steps one compilation composes: the real implementations behind the seams of the contract.</summary>
    internal IReadOnlyList<object> Steps => new CompilationRun(_version, _resolver, _limits).Steps;

    /// <summary>Compiles a schema document given as characters.</summary>
    /// <param name="schema">The document.</param>
    /// <param name="baseUri">The base URI of the document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The token to cancel the compilation.</param>
    /// <returns>The outcome.</returns>
    internal SchemaCompilation Compile(
        ReadOnlySpan<char> schema,
        string? baseUri = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = new CompilationRun(_version, _resolver, _limits);
        return run.Execute([run.Read(schema, baseUri)], cancellationToken);
    }

    /// <summary>Compiles a schema document given as bytes; the encoding follows XML 1.0 Appendix F.</summary>
    /// <param name="schema">The document.</param>
    /// <param name="baseUri">The base URI of the document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The token to cancel the compilation.</param>
    /// <returns>The outcome.</returns>
    internal SchemaCompilation Compile(
        ReadOnlySpan<byte> schema,
        string? baseUri = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var run = new CompilationRun(_version, _resolver, _limits);
        return run.Execute([run.Read(schema, baseUri)], cancellationToken);
    }

    /// <summary>Compiles several root documents into one schema; a document that two of them reach counts once.</summary>
    /// <param name="inputs">The documents, given as memory.</param>
    /// <param name="cancellationToken">The token to cancel the compilation.</param>
    /// <returns>The outcome; a schema without any document is valid and empty.</returns>
    internal SchemaCompilation Compile(IReadOnlyList<SchemaInput> inputs, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(inputs);

        var run = new CompilationRun(_version, _resolver, _limits);
        var documents = new List<SchemaDocument>(inputs.Count);
        for (var i = 0; i < inputs.Count; i++)
        {
            documents.Add(
                inputs[i].Location is null ? run.Read(inputs[i]) : run.ReadLocation(inputs[i], cancellationToken)
            );
        }

        return run.Execute(documents, cancellationToken);
    }
}
