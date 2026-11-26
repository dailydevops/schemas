namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

using NetEvolve.Schemas.Conformance.Results;

public class XspecEngineTests
{
    private const string Schema = """
        <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" queryBinding="xslt2">
          <sch:phase id="P"><sch:active pattern="p2"/></sch:phase>
          <sch:pattern id="p1"><sch:rule context="a"><sch:assert id="a1" test="@x" role="error">x missing</sch:assert></sch:rule></sch:pattern>
          <sch:pattern id="p2"><sch:rule context="b"><sch:report id="r1" test="true()">found b</sch:report></sch:rule></sch:pattern>
        </sch:schema>
        """;

    private static XspecCase Case(
        XspecFixture fixture,
        string id,
        XspecExpectation expectation,
        string? phase = null,
        string schema = "test/s.sch"
    ) =>
        new(
            id,
            "set",
            Path.Combine(fixture.Root, schema),
            phase,
            new XspecContext(null, "<root><a/><b/></root>"),
            expectation
        );

    private static XspecExpectation Expect(XspecExpectKind kind, string? id = null, string? location = null) =>
        new(kind, id, null, location, null, null, new Dictionary<string, string>());

    [Test]
    public async Task Run_HoldingAndFailingExpectations_AreRecordedAsPassAndFailWithTheReason(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new XspecFixture();
        _ = fixture.Write("test/s.sch", Schema);
        var selection = new XspecSelection(
            [
                Case(fixture, "set/1", Expect(XspecExpectKind.Assert, "a1", "/root/a[1]")),
                Case(fixture, "set/2", Expect(XspecExpectKind.Report, "r1")),
                Case(fixture, "set/3", Expect(XspecExpectKind.NotAssert, "a1")),
            ],
            [new XspecExclusion("set/4", "set", "x:call")]
        );

        var results = XspecEngine.Run(selection, fixture.Root, cancellationToken);

        await Assert
            .That(results.Select(r => r.Outcome))
            .IsEquivalentTo([CaseOutcome.Pass, CaseOutcome.Pass, CaseOutcome.Fail, CaseOutcome.Skipped]);
        await Assert.That(results.Single(r => r.CaseId == "set/3").Reason).IsNotNull();
        await Assert.That(results.Single(r => r.CaseId == "set/4").Reason).IsEqualTo("x:call");
        await Assert.That(results.All(r => r.TestSet == "set")).IsTrue();
    }

    [Test]
    public async Task Run_Phase_LimitsTheActivePatterns(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new XspecFixture();
        _ = fixture.Write("test/s.sch", Schema);
        var selection = new XspecSelection(
            [
                Case(fixture, "set/1", Expect(XspecExpectKind.NotAssert, "a1"), phase: "P"),
                Case(fixture, "set/2", Expect(XspecExpectKind.Report, "r1"), phase: "P"),
            ],
            []
        );

        var results = XspecEngine.Run(selection, fixture.Root, cancellationToken);

        await Assert.That(results.All(r => r.Outcome == CaseOutcome.Pass)).IsTrue();
    }

    [Test]
    public async Task Run_SchemaThatDoesNotCompile_FailsEveryExpectationOfIt(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new XspecFixture();
        _ = fixture.Write(
            "test/bad.sch",
            "<sch:schema xmlns:sch=\"http://purl.oclc.org/dsdl/schematron\"><sch:oops/></sch:schema>"
        );
        var selection = new XspecSelection(
            [Case(fixture, "set/1", Expect(XspecExpectKind.Valid), schema: "test/bad.sch")],
            []
        );

        var result = XspecEngine.Run(selection, fixture.Root, cancellationToken).Single();

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Fail);
        await Assert.That(result.Reason).IsNotNull();
    }

    [Test]
    public async Task Run_ContextFile_IsValidatedFromDisk(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new XspecFixture();
        _ = fixture.Write("test/s.sch", Schema);
        var instance = fixture.Write("test/i.xml", "<root><a x=\"1\"/></root>");
        var selection = new XspecSelection(
            [Case(fixture, "set/1", Expect(XspecExpectKind.Valid)) with { Context = new XspecContext(instance, null) }],
            []
        );

        var result = XspecEngine.Run(selection, fixture.Root, cancellationToken).Single();

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    public async Task Run_CanceledToken_Throws()
    {
        using var fixture = new XspecFixture();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert
            .That(() => XspecEngine.Run(new XspecSelection([], []), fixture.Root, source.Token))
            .Throws<OperationCanceledException>();
    }
}
