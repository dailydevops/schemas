namespace NetEvolve.Schemas.Xsd.Tests.Integration.Hostile;

/// <summary>
/// The hostile corpus: every case ends in its expected diagnostic inside the time bound, through <c>SchemaValidator.ForXsd</c> on stream and
/// memory input, without a successful external load and within a fixed allocation limit.
/// </summary>
[NotInParallel]
public class HostileCorpusTests
{
    /// <summary>The allocation limit of the occurrence-bound and nesting cases.</summary>
    private const long AllocationLimit = 512L * 1024 * 1024;

    private static readonly HostileInput[] Inputs = [HostileInput.Stream, HostileInput.Memory];

    public static IEnumerable<Func<(string, HostileInput)>> AllRuns() =>
        from id in HostileCorpus.All().Select(static c => c.Id)
        from input in Inputs
        select (Func<(string, HostileInput)>)(() => (id, input));

    public static IEnumerable<Func<(string, HostileInput)>> ExternalRuns() =>
        from id in HostileCorpus.All().Where(static c => c.ExternalAccess).Select(static c => c.Id)
        from input in Inputs
        select (Func<(string, HostileInput)>)(() => (id, input));

    public static IEnumerable<Func<(string, HostileInput)>> BoundRuns() =>
        from id in HostileCorpus
            .All()
            .Select(static c => c.Id)
            .Where(static id =>
                id.StartsWith("occurs-", StringComparison.Ordinal)
                || id.StartsWith("nesting-", StringComparison.Ordinal)
            )
        from input in Inputs
        select (Func<(string, HostileInput)>)(() => (id, input));

    private static HostileCase Find(string id) => HostileCorpus.All().Single(c => c.Id == id);

    [Test]
    [MethodDataSource(nameof(AllRuns))]
    public async Task Case_EndsWithTheExpectedCode_WithinTheTimeBound(
        string id,
        HostileInput input,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hostileCase = Find(id);
        var outcome = await HostileRunner.RunAsync(hostileCase, input, cancellationToken);

        await Assert.That(outcome.Codes).Contains(hostileCase.ExpectedCode);
        await Assert.That(outcome.Elapsed).IsLessThan(HostileRunner.TimeBound);
    }

    [Test]
    [MethodDataSource(nameof(ExternalRuns))]
    public async Task ExternalCase_NeverLoadsADocument(
        string id,
        HostileInput input,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var outcome = await HostileRunner.RunAsync(Find(id), input, cancellationToken);

        await Assert.That(outcome.Resolver.SuccessfulLoads).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(nameof(BoundRuns))]
    public async Task BoundCase_StaysUnderTheAllocationLimit(
        string id,
        HostileInput input,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hostileCase = Find(id);
        var before = GC.GetTotalAllocatedBytes(precise: true);
        _ = await HostileRunner.RunAsync(hostileCase, input, cancellationToken);
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - before;

        await Assert.That(allocated).IsLessThan(AllocationLimit);
    }

    [Test]
    public async Task Corpus_EveryCaseHasAnExpectedCode(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var missing = HostileCorpus.All().Where(static c => c.ExpectedCode < 0).Select(static c => c.Id).ToArray();

        await Assert.That(missing).IsEmpty();
        await Assert.That(HostileCorpus.All().Count()).IsGreaterThan(0);
    }
}
