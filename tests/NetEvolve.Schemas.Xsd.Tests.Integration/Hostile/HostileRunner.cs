namespace NetEvolve.Schemas.Xsd.Tests.Integration.Hostile;

using System.Diagnostics;
using System.Text;

/// <summary>Runs a case through <c>SchemaValidator.ForXsd</c> under a time bound.</summary>
internal static class HostileRunner
{
    /// <summary>The time bound of every case.</summary>
    public static readonly TimeSpan TimeBound = TimeSpan.FromSeconds(20);

    /// <summary>Builds the validator and validates the instance; a case over the time bound throws <see cref="TimeoutException"/>.</summary>
    public static async Task<HostileOutcome> RunAsync(
        HostileCase hostileCase,
        HostileInput input,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(hostileCase);

        var resolver = new RecordingResolver(hostileCase.Documents);
        var stopwatch = Stopwatch.StartNew();
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var work = Task.Run(() => RunCoreAsync(hostileCase, input, resolver, bound.Token), bound.Token);
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

    /// <summary>Builds the validator of the case without validating an instance.</summary>
    public static IXsdValidator Build(HostileCase hostileCase, RecordingResolver resolver, HostileInput input)
    {
        var builder = SchemaValidator
            .ForXsd()
            .WithSchemaLocationHints(hostileCase.SchemaLocationHints)
            .WithLimits(hostileCase.Limits)
            .WithResolver(resolver);
        var bytes = Encoding.UTF8.GetBytes(hostileCase.Schema);
        _ =
            input == HostileInput.Stream
                ? builder.AddSchema(new MemoryStream(bytes), hostileCase.SchemaUri)
                : builder.AddSchema(bytes.AsMemory(), hostileCase.SchemaUri);
        return builder.Build();
    }

    private static async Task<IReadOnlyList<int>> RunCoreAsync(
        HostileCase hostileCase,
        HostileInput input,
        RecordingResolver resolver,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        IXsdValidator validator;
        try
        {
            validator = Build(hostileCase, resolver, input);
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
                ? await validator.ValidateAsync(new MemoryStream(bytes), hostileCase.InstanceUri, cancellationToken)
                : await validator.ValidateAsync(bytes.AsMemory(), hostileCase.InstanceUri, cancellationToken);
        return [.. result.Issues.Select(static issue => issue.Code.Number)];
    }
}
