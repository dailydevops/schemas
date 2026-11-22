namespace NetEvolve.Schemas.Formats;

using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;

/// <summary>Base of every format validator: all overloads funnel into <see cref="ValidateCore"/>.</summary>
/// <remarks>
/// Limitation: a <see cref="Stream"/> is buffered completely before the core runs, so the input is not parsed
/// incrementally and holds up to <c>MaxInputSize + 1</c> bytes of pooled memory. Only <see cref="Stream"/> arguments are
/// null-checked; the core runs synchronously, also for the asynchronous memory and sequence overloads.
/// </remarks>
public abstract class SchemaValidatorBase : ISchemaValidator
{
    private readonly ValidationLimits _limits;
    private readonly IResourceResolver _resolver;

    /// <summary>Initializes a new instance of the <see cref="SchemaValidatorBase"/> class.</summary>
    /// <param name="limits">The limits.</param>
    /// <param name="resolver">The resolver.</param>
    protected SchemaValidatorBase(ValidationLimits limits, IResourceResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(resolver);

        _limits = limits;
        _resolver = resolver;
    }

    /// <summary>Gets the limits.</summary>
    protected ValidationLimits Limits => _limits;

    /// <summary>Gets the resolver.</summary>
    protected IResourceResolver Resolver => _resolver;

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        Stream stream,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => StartStreamAsync(NotNull(stream), systemId, cancellationToken);

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        ReadOnlyMemory<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => RunAsync(DocumentSource.Create(content.Span, systemId), cancellationToken);

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        ReadOnlyMemory<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => RunAsync(DocumentSource.Create(content.Span, systemId), cancellationToken);

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        ReadOnlySequence<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => RunAsync(DocumentSource.Create(content, systemId), cancellationToken);

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateAsync(
        ReadOnlySequence<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => RunAsync(DocumentSource.Create(content, systemId), cancellationToken);

    /// <inheritdoc />
    public ValidationResult Validate(
        Stream stream,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => ReadAndValidate(NotNull(stream), systemId, cancellationToken);

    /// <inheritdoc />
    public ValidationResult Validate(
        ReadOnlySpan<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => Run(DocumentSource.Create(content, systemId), cancellationToken);

    /// <inheritdoc />
    public ValidationResult Validate(
        ReadOnlySpan<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => Run(DocumentSource.Create(content, systemId), cancellationToken);

    /// <inheritdoc />
    public ValidationResult Validate(
        ReadOnlySequence<byte> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => Run(DocumentSource.Create(content, systemId), cancellationToken);

    /// <inheritdoc />
    public ValidationResult Validate(
        ReadOnlySequence<char> content,
        string? systemId = null,
        CancellationToken cancellationToken = default
    ) => Run(DocumentSource.Create(content, systemId), cancellationToken);

    /// <summary>Validates one document. The single synchronous core of the format.</summary>
    /// <param name="source">The document.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The validation result.</returns>
    protected abstract ValidationResult ValidateCore(in DocumentSource source, CancellationToken cancellationToken);

    private static Stream NotNull(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return stream;
    }

    // The caller's own cancellation ends canceled; any other failure, a foreign OperationCanceledException included, is faulted.
    private static ValueTask<ValidationResult> FailedAsync(Exception exception, CancellationToken cancellationToken) =>
        new(
            exception is OperationCanceledException && cancellationToken.IsCancellationRequested
                ? Task.FromCanceled<ValidationResult>(cancellationToken)
                : Task.FromException<ValidationResult>(exception)
        );

    private ValueTask<ValidationResult> RunAsync(in DocumentSource source, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new(Task.FromCanceled<ValidationResult>(cancellationToken));
        }

        try
        {
            return new(Run(in source, cancellationToken));
        }
        catch (Exception ex)
        {
            return FailedAsync(ex, cancellationToken);
        }
    }

    private ValueTask<ValidationResult> StartStreamAsync(
        Stream stream,
        string? systemId,
        CancellationToken cancellationToken
    )
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new(Task.FromCanceled<ValidationResult>(cancellationToken));
        }

        // An async method would turn every OperationCanceledException into a canceled task, so the outcome is set by hand.
        var completion = new TaskCompletionSource<ValidationResult>();
        Func<Task> complete = async () =>
        {
            try
            {
                completion.SetResult(
                    await ReadAndValidateAsync(stream, systemId, cancellationToken).ConfigureAwait(false)
                );
            }
            catch (Exception ex)
            {
                _ =
                    ex is OperationCanceledException && cancellationToken.IsCancellationRequested
                        ? completion.TrySetCanceled(cancellationToken)
                        : completion.TrySetException(ex);
            }
        };
        _ = complete();
        return new(completion.Task);
    }

    private ValidationResult ReadAndValidate(Stream stream, string? systemId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ExceedsLimit(stream))
        {
            return InputSizeExceeded(systemId);
        }

        using var buffer = new PooledStreamBuffer(stream, _limits.MaxInputSize);
        int read;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            read = stream.Read(buffer.Array, buffer.Offset, buffer.Count);
        } while (read > 0 && buffer.Advance(read));

        return Buffered(buffer, read > 0, systemId, cancellationToken);
    }

    private async Task<ValidationResult> ReadAndValidateAsync(
        Stream stream,
        string? systemId,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ExceedsLimit(stream))
        {
            return InputSizeExceeded(systemId);
        }

        using var buffer = new PooledStreamBuffer(stream, _limits.MaxInputSize);
        int read;
        do
        {
#if NETSTANDARD2_0
            read = await stream
                .ReadAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken)
                .ConfigureAwait(false);
#else
            read = await stream.ReadAsync(buffer.Window, cancellationToken).ConfigureAwait(false);
#endif
        } while (read > 0 && buffer.Advance(read));

        return Buffered(buffer, read > 0, systemId, cancellationToken);
    }

    // The span-holding source is created here, outside the async method, because a ref struct cannot live in one.
    private ValidationResult Buffered(
        PooledStreamBuffer buffer,
        bool exceeded,
        string? systemId,
        CancellationToken cancellationToken
    ) =>
        exceeded
            ? InputSizeExceeded(systemId)
            : ValidateCore(DocumentSource.Create(buffer.Content, systemId), cancellationToken);

    private ValidationResult Run(in DocumentSource source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return source.Length > _limits.MaxInputSize
            ? InputSizeExceeded(source.SystemId)
            : ValidateCore(in source, cancellationToken);
    }

    // A seekable stream that is already too long is rejected without reading a byte; the read loop covers a wrong Length.
    private bool ExceedsLimit(Stream stream) =>
        stream.CanSeek && stream.Length - stream.Position > _limits.MaxInputSize;

    private ValidationResult InputSizeExceeded(string? systemId)
    {
        var collector = new ValidationIssueCollector(_limits);
        collector.Stop(
            new ValidationIssue(
                CoreDiagnosticCodes.InputSizeLimitExceeded,
                IssueSeverity.Error,
                "The input exceeds MaxInputSize of "
                    + _limits.MaxInputSize.ToString(CultureInfo.InvariantCulture)
                    + ".",
                new DocumentLocation(systemId, 0, 0)
            )
        );
        return collector.ToResult();
    }
}
