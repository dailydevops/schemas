namespace NetEvolve.Schemas.Testing.Fakes;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Validator that records the content, system identifier and token of every call and applies the cancellation contract.
/// </summary>
/// <remarks>
/// Limitation: the fake returns no <see cref="ValidationResult"/> instance (<see langword="null"/>) until the result model
/// is implemented (FND-09); callers assert on <see cref="Calls"/>.
/// </remarks>
public sealed class FakeValidator : ISchemaValidator
{
    private readonly List<FakeValidatorCall> _calls = [];

    /// <summary>Gets the recorded calls in invocation order.</summary>
    public IReadOnlyList<FakeValidatorCall> Calls => _calls;

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        Stream stream,
        string? systemId = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(stream);
        return Record(nameof(ValidateAsync), "Stream", ReadAll(stream), null, systemId, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => Record(nameof(ValidateAsync), "ReadOnlyMemory<Byte>", content.ToArray(), null, systemId, cancellationToken);

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        ReadOnlyMemory<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => Record(nameof(ValidateAsync), "ReadOnlyMemory<Char>", null, content.ToString(), systemId, cancellationToken);

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        ReadOnlySequence<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => Record(nameof(ValidateAsync), "ReadOnlySequence<Byte>", content.ToArray(), null, systemId, cancellationToken);

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        ReadOnlySequence<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) =>
        Record(
            nameof(ValidateAsync),
            "ReadOnlySequence<Char>",
            null,
            new string(content.ToArray()),
            systemId,
            cancellationToken
        );

    /// <inheritdoc />
    public ValidationResult Validate(
        Stream stream,
        string? systemId = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(stream);
        return RecordSync(nameof(Validate), "Stream", ReadAll(stream), null, systemId, cancellationToken);
    }

    /// <inheritdoc />
    public ValidationResult Validate(
        ReadOnlySpan<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => RecordSync(nameof(Validate), "ReadOnlySpan<Byte>", content.ToArray(), null, systemId, cancellationToken);

    /// <inheritdoc />
    public ValidationResult Validate(
        ReadOnlySpan<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => RecordSync(nameof(Validate), "ReadOnlySpan<Char>", null, content.ToString(), systemId, cancellationToken);

    /// <inheritdoc />
    public ValidationResult Validate(
        ReadOnlySequence<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => RecordSync(nameof(Validate), "ReadOnlySequence<Byte>", content.ToArray(), null, systemId, cancellationToken);

    /// <inheritdoc />
    public ValidationResult Validate(
        ReadOnlySequence<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) =>
        RecordSync(
            nameof(Validate),
            "ReadOnlySequence<Char>",
            null,
            new string(content.ToArray()),
            systemId,
            cancellationToken
        );

    private static byte[] ReadAll(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    // A canceled token follows the asynchronous failure contract: a canceled ValueTask, never a synchronous throw.
    private ValueTask<ValidationResult> Record(
        string method,
        string parameterType,
        byte[]? bytes,
        string? chars,
        string? systemId,
        CancellationToken cancellationToken
    )
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new ValueTask<ValidationResult>(Task.FromCanceled<ValidationResult>(cancellationToken));
        }

        _calls.Add(new FakeValidatorCall(method, parameterType, bytes, chars, systemId, cancellationToken));
        return new ValueTask<ValidationResult>(result: null!);
    }

    private ValidationResult RecordSync(
        string method,
        string parameterType,
        byte[]? bytes,
        string? chars,
        string? systemId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        _calls.Add(new FakeValidatorCall(method, parameterType, bytes, chars, systemId, cancellationToken));
        return null!;
    }
}
