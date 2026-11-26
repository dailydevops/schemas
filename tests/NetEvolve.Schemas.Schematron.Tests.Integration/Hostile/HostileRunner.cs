namespace NetEvolve.Schemas.Schematron.Tests.Integration.Hostile;

using System.Diagnostics;
using System.Text;

/// <summary>Runs a case through <c>SchemaValidator.ForSchematron</c> under a time bound.</summary>
internal static class HostileRunner
{
    /// <summary>The time bound of every case.</summary>
    public static readonly TimeSpan TimeBound = TimeSpan.FromSeconds(20);

    /// <summary>Builds the validator and validates the instance; a case over the time bound throws <see cref="TimeoutException"/>.</summary>
    /// <param name="hostileCase">The case.</param>
    /// <param name="input">How the documents reach the validator.</param>
    /// <param name="defaultResolver">Whether the builder keeps its default resolver instead of the recording one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task<HostileOutcome> RunAsync(
        HostileCase hostileCase,
        HostileInput input,
        bool defaultResolver,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(hostileCase);

        var resolver = new RecordingResolver(hostileCase.Documents);
        var stopwatch = Stopwatch.StartNew();
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var work = Task.Run(
            () => RunCore(hostileCase, input, defaultResolver ? null : resolver, bound.Token),
            bound.Token
        );
        var delay = Task.Delay(TimeBound, bound.Token);
        if (await Task.WhenAny(work, delay) != work)
        {
            // Stop the leftover work so a hostile case cannot bleed into the next one.
            await CancelAsync(bound);
            throw new TimeoutException($"Case '{hostileCase.Id}' ({input}) exceeded the time bound of {TimeBound}.");
        }

        await CancelAsync(bound); // releases the pending delay timer
        var codes = await work;
        return new HostileOutcome(codes, stopwatch.Elapsed, resolver);
    }

    private static Task CancelAsync(CancellationTokenSource source)
    {
#if NET8_0_OR_GREATER
        return source.CancelAsync();
#else
        source.Cancel();
        return Task.CompletedTask;
#endif
    }

    // The span overloads cannot cross an await, so the whole run is synchronous.
    private static IReadOnlyList<int> RunCore(
        HostileCase hostileCase,
        HostileInput input,
        RecordingResolver? resolver,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var schema = Encoding.UTF8.GetBytes(hostileCase.Schema);
        var builder =
            input == HostileInput.Stream
                ? SchemaValidator.ForSchematron(new MemoryStream(schema), hostileCase.SchemaUri)
                : SchemaValidator.ForSchematron(schema.AsSpan(), hostileCase.SchemaUri);
        _ = builder.WithLimits(hostileCase.Limits);
        if (resolver is not null)
        {
            _ = builder.WithResolver(resolver);
        }

        ISchematronValidator validator;
        try
        {
            validator = builder.Build();
        }
        catch (SchemaCompilationException e)
        {
            return [.. e.Issues.Select(static issue => issue.Code.Number)];
        }

        if (hostileCase.Instance is null)
        {
            return [];
        }

        var bytes = Encoding.UTF8.GetBytes(hostileCase.Instance);
        var result =
            input == HostileInput.Stream
                ? validator.Validate(new MemoryStream(bytes), hostileCase.InstanceUri, cancellationToken)
                : validator.Validate(bytes.AsSpan(), hostileCase.InstanceUri, cancellationToken);
        return [.. result.Issues.Select(static issue => issue.Code.Number)];
    }
}
