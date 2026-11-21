namespace NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The validator compiles the schema documents of a schema test of XSD 1.0 and leaves every other case unobserved.</summary>
public class SchemaXstsValidatorTests
{
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    private static string Schema(string body) => "<xs:schema xmlns:xs=\"" + Xs + "\">" + body + "</xs:schema>";

    private static XstsSelectedCase Case(string version, bool expectValid, XstsKind kind, params string[] documents) =>
        new(
            version + "/g/n",
            version,
            expectValid,
            new XstsCase("set", "g", "n", kind, documents, null, "accepted", [], [])
        );

    [Test]
    public async Task CompileSchema_AValidSchema_IsValid(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = new TempDirectory();
        _ = suite.Write("a.xsd", Schema("<xs:element name=\"e\" type=\"xs:int\"/>"));

        var outcome = await new SchemaXstsValidator(suite.Path).CompileSchemaAsync(
            Case(XstsVersions.V10, true, XstsKind.Schema, "a.xsd"),
            cancellationToken
        );

        await Assert.That(outcome).IsEqualTo(XstsOutcome.Valid);
    }

    [Test]
    public async Task CompileSchema_ASchemaWithAnError_IsInvalid(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = new TempDirectory();
        _ = suite.Write("a.xsd", Schema("<xs:element name=\"e\" type=\"xs:missing\"/>"));

        var outcome = await new SchemaXstsValidator(suite.Path).CompileSchemaAsync(
            Case(XstsVersions.V10, false, XstsKind.Schema, "a.xsd"),
            cancellationToken
        );

        await Assert.That(outcome).IsEqualTo(XstsOutcome.Invalid);
    }

    [Test]
    public async Task CompileSchema_SeveralDocuments_AreOneSchemaAndAnIncludeIsFollowed(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = new TempDirectory();
        _ = suite.Write(
            "sub/a.xsd",
            Schema("<xs:include schemaLocation=\"inc.xsd\"/><xs:element name=\"e\" type=\"c\"/>")
        );
        _ = suite.Write(
            "sub/inc.xsd",
            Schema("<xs:simpleType name=\"c\"><xs:restriction base=\"xs:string\"/></xs:simpleType>")
        );
        _ = suite.Write("b.xsd", Schema("<xs:element name=\"f\" type=\"xs:int\"/>"));

        var outcome = await new SchemaXstsValidator(suite.Path).CompileSchemaAsync(
            Case(XstsVersions.V10, true, XstsKind.Schema, "sub/a.xsd", "b.xsd"),
            cancellationToken
        );

        await Assert.That(outcome).IsEqualTo(XstsOutcome.Valid);
    }

    [Test]
    public async Task CompileSchema_ALocationOutsideTheSuite_IsNotFollowed(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = new TempDirectory();
        _ = suite.Write("a.xsd", Schema("<xs:include schemaLocation=\"../outside.xsd\"/>"));

        var outcome = await new SchemaXstsValidator(suite.Path).CompileSchemaAsync(
            Case(XstsVersions.V10, false, XstsKind.Schema, "a.xsd"),
            cancellationToken
        );

        await Assert.That(outcome).IsEqualTo(XstsOutcome.Invalid);
    }

    [Test]
    public async Task CompileSchema_AnXsd11Case_IsNotObserved(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = new TempDirectory();
        _ = suite.Write("a.xsd", Schema(string.Empty));

        var outcome = await new SchemaXstsValidator(suite.Path).CompileSchemaAsync(
            Case(XstsVersions.V11, true, XstsKind.Schema, "a.xsd"),
            cancellationToken
        );

        await Assert.That(outcome).IsEqualTo(XstsOutcome.NotImplemented);
    }

    [Test]
    public async Task CompileSchema_AMissingDocument_IsNotObserved(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = new TempDirectory();

        var outcome = await new SchemaXstsValidator(suite.Path).CompileSchemaAsync(
            Case(XstsVersions.V10, true, XstsKind.Schema, "nope.xsd"),
            cancellationToken
        );

        await Assert.That(outcome).IsEqualTo(XstsOutcome.NotImplemented);
    }

    [Test]
    public async Task ValidateInstance_IsNotObserved_BecauseTheInstanceValidatorIsAnotherWorkPackage(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = new TempDirectory();

        var outcome = await new SchemaXstsValidator(suite.Path).ValidateInstanceAsync(
            Case(XstsVersions.V10, true, XstsKind.Instance, "a.xsd"),
            cancellationToken
        );

        await Assert.That(outcome).IsEqualTo(XstsOutcome.NotImplemented);
    }

    [Test]
    public async Task Run_ThroughTheRunner_ComparesTheOutcomeWithTheExpectation(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = new TempDirectory();
        _ = suite.Write("good.xsd", Schema("<xs:element name=\"e\" type=\"xs:int\"/>"));
        _ = suite.Write("bad.xsd", Schema("<xs:element name=\"e\" type=\"xs:missing\"/>"));
        var cases = new[]
        {
            Case(XstsVersions.V10, true, XstsKind.Schema, "good.xsd") with
            {
                Id = "1.0/g/good",
            },
            Case(XstsVersions.V10, false, XstsKind.Schema, "bad.xsd") with
            {
                Id = "1.0/g/bad",
            },
            Case(XstsVersions.V10, true, XstsKind.Schema, "bad.xsd") with
            {
                Id = "1.0/g/wrong",
            },
            Case(XstsVersions.V10, true, XstsKind.Instance, "good.xsd") with
            {
                Id = "1.0/g/instance",
            },
        };

        var results = await XstsRunner.RunAsync(
            new XstsSelection(cases, []),
            new SchemaXstsValidator(suite.Path),
            cancellationToken
        );

        await Assert
            .That(results.Select(r => (r.CaseId, r.Outcome)))
            .IsEquivalentTo([
                ("1.0/g/good", CaseOutcome.Pass),
                ("1.0/g/bad", CaseOutcome.Pass),
                ("1.0/g/wrong", CaseOutcome.Fail),
                ("1.0/g/instance", CaseOutcome.Fail),
            ]);
    }

    [Test]
    public async Task Constructor_AndCompileSchema_GuardTheirArguments(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(() => new SchemaXstsValidator(" ")).Throws<ArgumentException>();
        using var suite = new TempDirectory();
        await Assert
            .That(async () => await new SchemaXstsValidator(suite.Path).CompileSchemaAsync(null!, cancellationToken))
            .Throws<ArgumentNullException>();
    }
}
