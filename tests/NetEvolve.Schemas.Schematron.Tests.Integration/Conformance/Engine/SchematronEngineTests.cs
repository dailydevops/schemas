namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

using NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;
using static NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine.EngineFixtures;

public class SchematronEngineTests
{
    private static Task<SchematronObservation> Run(
        SchematronCase schematronCase,
        CancellationToken cancellationToken
    ) => SchematronEngine.Instance.ValidateAsync(schematronCase, cancellationToken);

    [Test]
    public async Task ValidateAsync_NoFailedAssertion_IsValid(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var observation = await Run(Case(SchematronOutcome.Valid, [Schema("true()")]), cancellationToken);

        await Assert.That(observation).IsEqualTo(new SchematronObservation(SchematronOutcome.Valid));
    }

    [Test]
    public async Task ValidateAsync_FailedAssertion_IsInvalid(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var observation = await Run(Case(SchematronOutcome.Invalid, [Schema("false()")]), cancellationToken);

        await Assert.That(observation).IsEqualTo(new SchematronObservation(SchematronOutcome.Invalid));
    }

    [Test]
    public async Task ValidateAsync_SuccessfulReport_IsInvalid(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var observation = await Run(Case(SchematronOutcome.Invalid, [Schema("true()", "report")]), cancellationToken);

        await Assert.That(observation.Outcome).IsEqualTo(SchematronOutcome.Invalid);
    }

    [Test]
    public async Task ValidateAsync_SchemaThatDoesNotCompile_IsError(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var observation = await Run(Case(SchematronOutcome.Error, [Schema("(")]), cancellationToken);

        await Assert.That(observation.Outcome).IsEqualTo(SchematronOutcome.Error);
    }

    [Test]
    public async Task ValidateAsync_AssertionOnTheSvrlHolds_HasNoReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var schematronCase = Case(
            SchematronOutcome.Invalid,
            [Schema("false()")],
            ["//svrl:failed-assert/@test = 'false()'"]
        );

        var observation = await Run(schematronCase, cancellationToken);

        await Assert.That(observation).IsEqualTo(new SchematronObservation(SchematronOutcome.Invalid));
    }

    [Test]
    public async Task ValidateAsync_AssertionOnTheSvrlDoesNotHold_NamesItInTheReason(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var schematronCase = Case(
            SchematronOutcome.Invalid,
            [Schema("false()")],
            ["//svrl:failed-assert/@test = 'true()'"]
        );

        var observation = await Run(schematronCase, cancellationToken);

        await Assert.That(observation.Outcome).IsEqualTo(SchematronOutcome.Invalid);
        await Assert.That(observation.Reason).Contains("//svrl:failed-assert/@test = 'true()'");
    }

    [Test]
    public async Task ValidateAsync_FiredAssertionIsMissingInTheSvrl_FailsTheAssertion(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var schematronCase = Case(SchematronOutcome.Valid, [Schema("true()")], ["count(//svrl:failed-assert) = 1"]);

        var observation = await Run(schematronCase, cancellationToken);

        await Assert.That(observation.Reason).Contains("count(//svrl:failed-assert) = 1");
    }

    [Test]
    public async Task ValidateAsync_PhaseOfTheCase_SelectsTheActivePatterns(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string schema = """
            <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron">
              <sch:phase id="clean"><sch:active pattern="ok"/></sch:phase>
              <sch:phase id="dirty"><sch:active pattern="bad"/></sch:phase>
              <sch:pattern id="ok"><sch:rule context="/"><sch:assert test="true()"/></sch:rule></sch:pattern>
              <sch:pattern id="bad"><sch:rule context="/"><sch:assert test="false()"/></sch:rule></sch:pattern>
            </sch:schema>
            """;

        var clean = await Run(Case(SchematronOutcome.Valid, [schema], phase: "clean"), cancellationToken);
        var dirty = await Run(Case(SchematronOutcome.Invalid, [schema], phase: "dirty"), cancellationToken);

        await Assert.That(clean.Outcome).IsEqualTo(SchematronOutcome.Valid);
        await Assert.That(dirty.Outcome).IsEqualTo(SchematronOutcome.Invalid);
    }

    [Test]
    public async Task ValidateAsync_IncludeOfAnotherDocumentOfTheCase_IsResolved(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string schema = """
            <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron">
              <sch:pattern><sch:rule context="/"><sch:include href="include.sch#assert"/></sch:rule></sch:pattern>
            </sch:schema>
            """;
        const string include = """
            <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron">
              <sch:pattern><sch:rule abstract="true"><sch:assert xml:id="assert" test="false()"/></sch:rule></sch:pattern>
            </sch:schema>
            """;

        var observation = await Run(
            Case(SchematronOutcome.Invalid, [schema], extra: new SchematronDocument("include.sch", include)),
            cancellationToken
        );

        await Assert.That(observation).IsEqualTo(new SchematronObservation(SchematronOutcome.Invalid));
    }

    [Test]
    public async Task ValidateAsync_SubordinateDocumentOfTheCase_IsValidated(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string schema = """
            <sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron">
              <sch:pattern documents="/element/@secondary">
                <sch:rule context="/"><sch:report test="root"/></sch:rule>
              </sch:pattern>
            </sch:schema>
            """;

        var observation = await Run(
            Case(
                SchematronOutcome.Invalid,
                [schema],
                instance: """<element secondary="document-02.xml" xmlns=""/>""",
                extra: new SchematronDocument("document-02.xml", """<root xmlns=""/>""")
            ),
            cancellationToken
        );

        await Assert.That(observation).IsEqualTo(new SchematronObservation(SchematronOutcome.Invalid));
    }

    [Test]
    public async Task ValidateAsync_EverySchemaOfTheCaseMustGiveTheExpectedOutcome(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var schematronCase = Case(SchematronOutcome.Invalid, [Schema("false()"), Schema("true()")]);

        var observation = await Run(schematronCase, cancellationToken);

        await Assert.That(observation.Outcome).IsEqualTo(SchematronOutcome.Valid);
        await Assert.That(observation.Reason).Contains("Schema 2 of 2");
    }

    [Test]
    public async Task ValidateAsync_Cancelled_Throws()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert
            .That(async () => await Run(Case(SchematronOutcome.Valid, [Schema("true()")]), source.Token))
            .Throws<OperationCanceledException>();
    }
}
