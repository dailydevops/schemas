namespace NetEvolve.Schemas.Schematron.Tests.Integration.Hostile;

/// <summary>
/// The hostile corpus: every case ends in its expected diagnostic inside the time bound, through <c>SchemaValidator.ForSchematron</c> on
/// stream and memory input, and an external case never loads a document, with the default resolver as well.
/// </summary>
[NotInParallel]
public class HostileCorpusTests
{
    /// <summary>The allocation limit of the cases that grow memory.</summary>
    private const long AllocationLimit = 512L * 1024 * 1024;

    private static readonly string[] BoundPrefixes =
    [
        "growth-",
        "instance-",
        "expression-",
        "rules-",
        "patterns-",
        "svrl-",
    ];

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
            .Where(static id => BoundPrefixes.Any(prefix => id.StartsWith(prefix, StringComparison.Ordinal)))
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
        var outcome = await HostileRunner.RunAsync(hostileCase, input, defaultResolver: false, cancellationToken);

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

        var outcome = await HostileRunner.RunAsync(Find(id), input, defaultResolver: false, cancellationToken);

        await Assert.That(outcome.Resolver.SuccessfulLoads).IsEqualTo(0);
    }

    [Test]
    [MethodDataSource(nameof(ExternalRuns))]
    public async Task ExternalCase_DefaultResolver_EndsWithTheExpectedCode(
        string id,
        HostileInput input,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hostileCase = Find(id);
        var outcome = await HostileRunner.RunAsync(hostileCase, input, defaultResolver: true, cancellationToken);

        await Assert.That(outcome.Codes).Contains(hostileCase.ExpectedCode);
        await Assert.That(outcome.Resolver.Requests).IsEmpty();
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
        _ = await HostileRunner.RunAsync(hostileCase, input, defaultResolver: false, cancellationToken);
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - before;

        await Assert.That(allocated).IsLessThan(AllocationLimit);
    }

    [Test]
    [Arguments(HostileInput.Stream)]
    [Arguments(HostileInput.Memory)]
    public async Task SvrlReportSize_IssuesStayWithinMaxIssues(HostileInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var outcome = await HostileRunner.RunAsync(
            Find("svrl-report-size"),
            input,
            defaultResolver: false,
            cancellationToken
        );

        await Assert.That(outcome.Codes.Count).IsLessThanOrEqualTo(ValidationLimits.Default.MaxIssues);
    }

    [Test]
    public async Task Corpus_CoversEveryScopeCategory(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var families = HostileCorpus
            .All()
            .Select(static c => c.Id[..c.Id.IndexOf('-', StringComparison.Ordinal)])
            .ToHashSet(StringComparer.Ordinal);

        await Assert
            .That(families)
            .IsSupersetOf([
                "include",
                "xinclude",
                "recursion",
                "growth",
                "expression",
                "rules",
                "patterns",
                "instance",
                "schema",
                "svrl",
                "external",
            ]);
    }
}
