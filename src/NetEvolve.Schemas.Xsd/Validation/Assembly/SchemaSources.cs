namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Compilation;

/// <summary>Turns the schema sources of the builder into the inputs of the compiler, bounded by <c>MaxInputSize</c>.</summary>
/// <remarks>
/// A source of memory is checked against the limit and handed on without a copy; a stream is read to its end from its current
/// position into a pooled buffer, with <c>ReadAsync</c> when the build is asynchronous, and is never disposed. A source over the
/// limit ends the build with a <see cref="SchemaCompilationException"/> that holds the core code NES0001.
/// </remarks>
internal static class SchemaSources
{
    /// <summary>Reads the sources synchronously.</summary>
    /// <param name="sources">The sources.</param>
    /// <param name="limits">The limits of the build.</param>
    /// <returns>The inputs.</returns>
    /// <exception cref="SchemaCompilationException">A source exceeds <c>MaxInputSize</c>.</exception>
    internal static List<SchemaInput> Read(IReadOnlyList<SchemaSource> sources, ValidationLimits limits)
    {
        Argument.ThrowIfNull(sources);
        Argument.ThrowIfNull(limits);

        var inputs = new List<SchemaInput>(sources.Count);
        foreach (var source in sources)
        {
            inputs.Add(source.Stream is null ? FromMemory(source, limits) : FromStream(source, limits));
        }

        return inputs;
    }

    /// <summary>Reads the sources, a stream with <c>ReadAsync</c>.</summary>
    /// <param name="sources">The sources.</param>
    /// <param name="limits">The limits of the build.</param>
    /// <param name="cancellationToken">The token that is observed between the documents and at every read.</param>
    /// <returns>The inputs.</returns>
    /// <exception cref="SchemaCompilationException">A source exceeds <c>MaxInputSize</c>.</exception>
    internal static async ValueTask<List<SchemaInput>> ReadAsync(
        IReadOnlyList<SchemaSource> sources,
        ValidationLimits limits,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfNull(sources);
        Argument.ThrowIfNull(limits);

        var inputs = new List<SchemaInput>(sources.Count);
        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            inputs.Add(
                source.Stream is null
                    ? FromMemory(source, limits)
                    : await FromStreamAsync(source, limits, cancellationToken).ConfigureAwait(false)
            );
        }

        return inputs;
    }

    private static SchemaInput FromMemory(in SchemaSource source, ValidationLimits limits)
    {
        if (source.Chars.Length > limits.MaxInputSize || source.Bytes.Length > limits.MaxInputSize)
        {
            throw TooLarge(source.SystemId, limits);
        }

        return source.IsBytes
            ? SchemaInput.FromBytes(source.Bytes, source.SystemId)
            : SchemaInput.FromChars(source.Chars, source.SystemId);
    }

    private static SchemaInput FromStream(in SchemaSource source, ValidationLimits limits)
    {
        var stream = source.Stream!;
        if (ExceedsLimit(stream, limits))
        {
            throw TooLarge(source.SystemId, limits);
        }

        using var buffer = new PooledStreamBuffer(stream, limits.MaxInputSize);
        int read;
        do
        {
            read = stream.Read(buffer.Array, buffer.Offset, buffer.Count);
        } while (read > 0 && buffer.Advance(read));

        return Finish(buffer, read, source.SystemId, limits);
    }

    private static async ValueTask<SchemaInput> FromStreamAsync(
        SchemaSource source,
        ValidationLimits limits,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stream = source.Stream!;
        if (ExceedsLimit(stream, limits))
        {
            throw TooLarge(source.SystemId, limits);
        }

        using var buffer = new PooledStreamBuffer(stream, limits.MaxInputSize);
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

        return Finish(buffer, read, source.SystemId, limits);
    }

    // The loop ends on a read of zero bytes, or when the buffer holds one byte more than the limit allows.
    private static SchemaInput Finish(
        PooledStreamBuffer buffer,
        int lastRead,
        string? systemId,
        ValidationLimits limits
    ) => lastRead > 0 ? throw TooLarge(systemId, limits) : SchemaInput.FromBytes(buffer.Content.ToArray(), systemId);

    // A seekable stream that is already too long is rejected without reading a byte; the read loop covers a wrong length.
    private static bool ExceedsLimit(Stream stream, ValidationLimits limits) =>
        stream.CanSeek && stream.Length - stream.Position > limits.MaxInputSize;

    private static SchemaCompilationException TooLarge(string? systemId, ValidationLimits limits) =>
        new([
            new ValidationIssue(
                CoreDiagnosticCodes.InputSizeLimitExceeded,
                IssueSeverity.Error,
                "The schema document exceeds MaxInputSize of "
                    + limits.MaxInputSize.ToString(CultureInfo.InvariantCulture)
                    + ".",
                new DocumentLocation(systemId, 0, 0)
            ),
        ]);
}
