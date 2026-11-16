#pragma warning disable CA1065, MA0025 // Stub per #103: throwing bodies stay until FND-09, FND-10, FND-55 and FND-56 replace them.

namespace NetEvolve.Schemas.Formats;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;

/// <summary>Base of every format validator: all overloads funnel into <see cref="ValidateCore"/>.</summary>
/// <remarks>Limitation: members throw until FND-56 lands.</remarks>
public abstract class SchemaValidatorBase : ISchemaValidator
{
    /// <summary>Initializes a new instance of the <see cref="SchemaValidatorBase"/> class.</summary>
    /// <param name="limits">The limits.</param>
    /// <param name="resolver">The resolver.</param>
    protected SchemaValidatorBase(ValidationLimits limits, IResourceResolver resolver) => throw Stub.For("FND-56");

    /// <summary>Gets the limits.</summary>
    protected ValidationLimits Limits => throw Stub.For("FND-56");

    /// <summary>Gets the resolver.</summary>
    protected IResourceResolver Resolver => throw Stub.For("FND-56");

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        Stream stream,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => throw Stub.For("FND-56");

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => throw Stub.For("FND-56");

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        ReadOnlyMemory<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => throw Stub.For("FND-56");

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        ReadOnlySequence<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => throw Stub.For("FND-56");

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        ReadOnlySequence<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => throw Stub.For("FND-56");

    /// <inheritdoc />
    public ValidationResult Validate(
        Stream stream,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => throw Stub.For("FND-56");

    /// <inheritdoc />
    public ValidationResult Validate(
        ReadOnlySpan<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => throw Stub.For("FND-56");

    /// <inheritdoc />
    public ValidationResult Validate(
        ReadOnlySpan<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => throw Stub.For("FND-56");

    /// <inheritdoc />
    public ValidationResult Validate(
        ReadOnlySequence<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => throw Stub.For("FND-56");

    /// <inheritdoc />
    public ValidationResult Validate(
        ReadOnlySequence<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => throw Stub.For("FND-56");

    /// <summary>Validates one document. The single synchronous core of the format.</summary>
    /// <param name="source">The document.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The validation result.</returns>
    protected abstract ValidationResult ValidateCore(in DocumentSource source, CancellationToken cancellationToken);
}
