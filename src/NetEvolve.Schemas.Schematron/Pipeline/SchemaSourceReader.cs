namespace NetEvolve.Schemas.Schematron.Pipeline;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Reads the schema source of a build: a stream from its current position to its end, never disposed, bounded by
/// <see cref="ValidationLimits.MaxResourceSize"/>, and either synchronously by <c>Build</c> or asynchronously by <c>BuildAsync</c>.
/// </summary>
/// <remarks>
/// <para>
/// The bytes of a stream that was read are kept by the source (<see cref="SchemaSource.Remember"/>), so a second build replays them and
/// a stream that cannot seek is never read twice. A schema in memory is bounded by the same limit: bytes count as bytes and characters
/// as characters. A schema over the limit is a <see cref="SchemaCompilationException"/> with
/// <see cref="CoreDiagnosticCodes.ResourceSizeLimitExceeded"/> (design #137 AP-8, provisional OQ-6: the alternative is <c>MaxInputSize</c>).
/// </para>
/// <para>
/// Limitation: the shared schema-source holder that design #137 asks of the core (prerequisite P2) does not exist; this class is the
/// Schematron-private stand-in for it and uses the pooled buffer of the core, which the core grants through <c>InternalsVisibleTo</c>.
/// </para>
/// </remarks>
internal static class SchemaSourceReader
{
    /// <summary>Gets the bytes of a source of kind <see cref="SchemaSourceKind.Bytes"/> or <see cref="SchemaSourceKind.Stream"/>, reading the stream when it has not been read.</summary>
    /// <param name="source">The source.</param>
    /// <param name="limits">The limits of the build.</param>
    /// <param name="cancellationToken">The token of the build.</param>
    /// <returns>The bytes of the schema.</returns>
    /// <exception cref="SchemaCompilationException">The schema is larger than <see cref="ValidationLimits.MaxResourceSize"/>.</exception>
    internal static ReadOnlyMemory<byte> Bytes(
        SchemaSource source,
        ValidationLimits limits,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfNull(source);
        Argument.ThrowIfNull(limits);

        if (source.Kind == SchemaSourceKind.Stream && !source.HasReplay)
        {
            source.Remember(ReadStream(source, limits, cancellationToken));
        }

        var bytes = source.Kind == SchemaSourceKind.Stream ? source.Replay : source.Bytes;
        return bytes.Length > limits.MaxResourceSize ? throw TooLarge(source, limits) : bytes;
    }

    /// <summary>Checks that the characters of a schema in text are within the limit.</summary>
    /// <param name="source">The source of kind <see cref="SchemaSourceKind.Chars"/>.</param>
    /// <param name="limits">The limits of the build.</param>
    /// <returns>The characters of the schema.</returns>
    /// <exception cref="SchemaCompilationException">The schema is larger than <see cref="ValidationLimits.MaxResourceSize"/>.</exception>
    internal static ReadOnlyMemory<char> Chars(SchemaSource source, ValidationLimits limits)
    {
        Argument.ThrowIfNull(source);
        Argument.ThrowIfNull(limits);

        return source.Chars.Length > limits.MaxResourceSize ? throw TooLarge(source, limits) : source.Chars;
    }

    /// <summary>Reads a stream source asynchronously and keeps its bytes; the later synchronous stages then find them.</summary>
    /// <param name="source">The source; one of another kind, or a stream that was read, is left as it is.</param>
    /// <param name="limits">The limits of the build.</param>
    /// <param name="cancellationToken">The token of the build.</param>
    /// <returns>A task that completes when the stream is read.</returns>
    /// <exception cref="SchemaCompilationException">The stream is larger than <see cref="ValidationLimits.MaxResourceSize"/>.</exception>
    internal static async ValueTask PrefetchAsync(
        SchemaSource source,
        ValidationLimits limits,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfNull(source);
        Argument.ThrowIfNull(limits);

        if (source.Kind != SchemaSourceKind.Stream || source.HasReplay)
        {
            return;
        }

        var stream = source.Stream!;
        if (ExceedsLimit(stream, limits))
        {
            throw TooLarge(source, limits);
        }

        using var buffer = new PooledStreamBuffer(stream, limits.MaxResourceSize);
        int read;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
#if NETSTANDARD2_0
            read = await stream
                .ReadAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken)
                .ConfigureAwait(false);
#else
            read = await stream.ReadAsync(buffer.Window, cancellationToken).ConfigureAwait(false);
#endif
        } while (read > 0 && buffer.Advance(read));

        cancellationToken.ThrowIfCancellationRequested();
        source.Remember(read > 0 ? throw TooLarge(source, limits) : buffer.Content.ToArray());
    }

    private static byte[] ReadStream(SchemaSource source, ValidationLimits limits, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stream = source.Stream!;
        if (ExceedsLimit(stream, limits))
        {
            throw TooLarge(source, limits);
        }

        using var buffer = new PooledStreamBuffer(stream, limits.MaxResourceSize);
        int read;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            read = stream.Read(buffer.Array, buffer.Offset, buffer.Count);
        } while (read > 0 && buffer.Advance(read));

        return read > 0 ? throw TooLarge(source, limits) : buffer.Content.ToArray();
    }

    // A seekable stream that is already too long is refused without reading a byte; the read loop covers a wrong Length.
    private static bool ExceedsLimit(Stream stream, ValidationLimits limits) =>
        stream.CanSeek && stream.Length - stream.Position > limits.MaxResourceSize;

    private static SchemaCompilationException TooLarge(SchemaSource source, ValidationLimits limits) =>
        new([
            new ValidationIssue(
                CoreDiagnosticCodes.ResourceSizeLimitExceeded,
                IssueSeverity.Error,
                "The schema is larger than " + limits.MaxResourceSize + " (MaxResourceSize).",
                new DocumentLocation(source.SystemId, 0, 0)
            ),
        ]);
}
