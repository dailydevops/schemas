namespace NetEvolve.Schemas.Xsd.Tests.Integration.DifferentialData;

using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

public class OursEngineTests
{
    private const string Schema =
        "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>\n<xs:element name='a' type='xs:int'/>\n</xs:schema>";

    private const string BadSchema =
        "<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>\n<xs:element name='a' type='xs:nope'/>\n</xs:schema>";

    private static async Task<EngineResult?> ObserveAsync(
        TempDirectory directory,
        string schema,
        string? instance,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var schemaPath = directory.Write("a.xsd", schema);
        var instancePath = instance is null ? null : directory.Write("a.xml", instance);
        return await new OursEngine(directory.Path).ObserveAsync(
            new DifferentialCase("c", [schemaPath], instancePath, Verdict.Valid, Verdict.NotRun),
            cancellationToken
        );
    }

    private static DifferentialCase MissingSchema(TempDirectory directory) =>
        new("c", [Path.Combine(directory.Path, "missing.xsd")], null, Verdict.Valid, Verdict.NotRun);

    [Test]
    public async Task Observe_ValidSchemaWithoutInstance_IsValidAndNotRun(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();

        var result = await ObserveAsync(directory, Schema, null, cancellationToken);

        await Assert.That(result!.Schema).IsEqualTo(Verdict.Valid);
        await Assert.That(result.Instance).IsEqualTo(Verdict.NotRun);
        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task Observe_InvalidSchema_IsInvalidWithIssuePositionsAndTheInstanceNotRun(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();

        var result = await ObserveAsync(directory, BadSchema, "<a>1</a>", cancellationToken);

        await Assert.That(result!.Schema).IsEqualTo(Verdict.Invalid);
        await Assert.That(result.Instance).IsEqualTo(Verdict.NotRun);
        await Assert.That(result.Issues).IsNotEmpty();
    }

    [Test]
    [Arguments("<a>1</a>", true)]
    [Arguments("<a>one</a>", false)]
    public async Task Observe_Instance_HasTheVerdictOfTheValidator(
        string instance,
        bool valid,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();

        var result = await ObserveAsync(directory, Schema, instance, cancellationToken);

        await Assert.That(result!.Schema).IsEqualTo(Verdict.Valid);
        await Assert.That(result.Instance).IsEqualTo(valid ? Verdict.Valid : Verdict.Invalid);
        await Assert.That(result.Issues.Count == 0).IsEqualTo(valid);
    }

    [Test]
    public async Task Observe_MissingSchemaDocument_ObservesNothing(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();

        var result = await new OursEngine(directory.Path).ObserveAsync(MissingSchema(directory), cancellationToken);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Run_MissingSchemaDocument_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        var engine = new OursEngine(directory.Path);
        var missing = MissingSchema(directory);

        await Assert
            .That(async () => await engine.RunAsync(missing, cancellationToken))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ThrowTolerantEngine_ArgumentException_IsAnInvalidSchemaWithoutPositions(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var engine = new ThrowTolerantEngine(
            new FakeDifferentialEngine(_ => throw new ArgumentException("An empty name.", nameof(cancellationToken)))
        );

        var result = await engine.RunAsync(
            new DifferentialCase("c", ["a.xsd"], null, Verdict.Valid, Verdict.NotRun),
            cancellationToken
        );

        await Assert.That(result).IsEqualTo(new EngineResult(Verdict.Invalid, Verdict.NotRun, []));
    }

    [Test]
    public async Task ThrowTolerantEngine_AnyOtherResult_PassesThrough(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var expected = new EngineResult(Verdict.Valid, Verdict.Invalid, [new IssuePosition(3, 4)]);
        var engine = new ThrowTolerantEngine(new FakeDifferentialEngine(_ => expected));

        var result = await engine.RunAsync(
            new DifferentialCase("c", ["a.xsd"], "a.xml", Verdict.Valid, Verdict.Valid),
            cancellationToken
        );

        await Assert.That(result).IsEqualTo(expected);
    }
}
