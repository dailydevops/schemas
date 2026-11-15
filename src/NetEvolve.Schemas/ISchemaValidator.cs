namespace NetEvolve.Schemas;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Validates documents against a compiled schema. Implementations are immutable and thread-safe.</summary>
public interface ISchemaValidator
{
    /// <summary>Validates the remainder of a stream. The caller keeps ownership of the stream.</summary>
    /// <param name="stream">The stream to read from its current position to the end.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    ValueTask<ValidationResult> ValidateAsync(
        Stream stream,
        string? systemId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Validates encoded bytes.</summary>
    /// <param name="content">The document bytes.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    ValueTask<ValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Validates document text.</summary>
    /// <param name="content">The document characters.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    ValueTask<ValidationResult> ValidateAsync(
        ReadOnlyMemory<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Validates bytes held in a sequence.</summary>
    /// <param name="content">The document bytes.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    ValueTask<ValidationResult> ValidateAsync(
        ReadOnlySequence<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Validates characters held in a sequence.</summary>
    /// <param name="content">The document characters.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    ValueTask<ValidationResult> ValidateAsync(
        ReadOnlySequence<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Validates the remainder of a stream. The caller keeps ownership of the stream.</summary>
    /// <param name="stream">The stream to read from its current position to the end.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(Stream stream, string? systemId = null, CancellationToken cancellationToken = default);

    /// <summary>Validates encoded bytes.</summary>
    /// <param name="content">The document bytes.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(
        ReadOnlySpan<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Validates document text. A <see cref="string"/> binds to this overload.</summary>
    /// <param name="content">The document characters.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(
        ReadOnlySpan<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Validates bytes held in a sequence.</summary>
    /// <param name="content">The document bytes.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(
        ReadOnlySequence<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Validates characters held in a sequence.</summary>
    /// <param name="content">The document characters.</param>
    /// <param name="systemId">The system identifier and base URI of the document, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(
        ReadOnlySequence<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    );
}
