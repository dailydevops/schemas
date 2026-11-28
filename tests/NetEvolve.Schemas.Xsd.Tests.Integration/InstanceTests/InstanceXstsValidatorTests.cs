namespace NetEvolve.Schemas.Xsd.Tests.Integration.InstanceTests;

using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>The validator builds the schema of the group of an instance test with the public API and validates the instance.</summary>
public class InstanceXstsValidatorTests
{
    private const string Xs = "http://www.w3.org/2001/XMLSchema";

    private static string Schema(string body) => "<xs:schema xmlns:xs=\"" + Xs + "\">" + body + "</xs:schema>";

    private static XstsSelectedCase Case(
        string version,
        bool expectValid,
        string? instance,
        string group = "g",
        params string[] documents
    ) =>
        new(
            version + "/" + group + "/n",
            version,
            expectValid,
            new XstsCase("set", group, "n", XstsKind.Instance, documents, instance, "accepted", [], [])
        );

    private static TempDirectory Suite()
    {
        var suite = new TempDirectory();
        _ = suite.Write("a.xsd", Schema("<xs:element name=\"e\" type=\"xs:int\"/>"));
        _ = suite.Write("ok.xml", "<e>1</e>");
        _ = suite.Write("bad.xml", "<e>x</e>");
        _ = suite.Write("undeclared.xml", "<z/>");
        return suite;
    }

    [Test]
    public async Task ValidateInstance_AValidInstance_IsValid(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = Suite();

        var outcome = await new InstanceXstsValidator(suite.Path).ValidateInstanceAsync(
            Case(XstsVersions.V10, true, "ok.xml", "g", "a.xsd"),
            cancellationToken
        );

        await Assert.That(outcome).IsEqualTo(XstsOutcome.Valid);
    }

    [Test]
    public async Task ValidateInstance_InvalidAndUndeclaredInstances_AreInvalid(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = Suite();
        var validator = new InstanceXstsValidator(suite.Path);

        var bad = await validator.ValidateInstanceAsync(
            Case(XstsVersions.V10, false, "bad.xml", "g", "a.xsd"),
            cancellationToken
        );
        var undeclared = await validator.ValidateInstanceAsync(
            Case(XstsVersions.V10, false, "undeclared.xml", "g", "a.xsd"),
            cancellationToken
        );

        await Assert.That(bad).IsEqualTo(XstsOutcome.Invalid);
        await Assert.That(undeclared).IsEqualTo(XstsOutcome.Invalid);
    }

    [Test]
    public async Task ValidateInstance_ADoctypeWithAnEntity_IsRead(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = Suite();
        _ = suite.Write("dtd.xml", "<!DOCTYPE e [<!ENTITY n \"7\">]><e>&n;</e>");

        var outcome = await new InstanceXstsValidator(suite.Path).ValidateInstanceAsync(
            Case(XstsVersions.V10, true, "dtd.xml", "g", "a.xsd"),
            cancellationToken
        );

        await Assert.That(outcome).IsEqualTo(XstsOutcome.Valid);
    }

    [Test]
    public async Task ValidateInstance_TheSchemaOfAGroup_IsBuiltOnce(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = Suite();
        var validator = new InstanceXstsValidator(suite.Path);
        var first = await validator.ValidateInstanceAsync(
            Case(XstsVersions.V10, true, "ok.xml", "g", "a.xsd"),
            cancellationToken
        );
        File.Delete(Path.Combine(suite.Path, "a.xsd"));

        var second = await validator.ValidateInstanceAsync(
            Case(XstsVersions.V10, false, "bad.xml", "g", "a.xsd"),
            cancellationToken
        );

        await Assert.That(first).IsEqualTo(XstsOutcome.Valid);
        await Assert.That(second).IsEqualTo(XstsOutcome.Invalid);
    }

    [Test]
    public async Task ValidateInstance_CasesThatCannotBeObserved_AreNotImplemented(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = Suite();
        _ = suite.Write("broken.xsd", Schema("<xs:element name=\"e\" type=\"xs:missing\"/>"));
        var validator = new InstanceXstsValidator(suite.Path);

        var version11 = await validator.ValidateInstanceAsync(
            Case(XstsVersions.V11, true, "ok.xml", "g11", "a.xsd"),
            cancellationToken
        );
        var brokenSchema = await validator.ValidateInstanceAsync(
            Case(XstsVersions.V10, true, "ok.xml", "broken", "broken.xsd"),
            cancellationToken
        );
        var missingSchema = await validator.ValidateInstanceAsync(
            Case(XstsVersions.V10, true, "ok.xml", "missing", "nope.xsd"),
            cancellationToken
        );
        var noInstance = await validator.ValidateInstanceAsync(
            Case(XstsVersions.V10, true, null, "g", "a.xsd"),
            cancellationToken
        );
        var missingInstance = await validator.ValidateInstanceAsync(
            Case(XstsVersions.V10, true, "nope.xml", "g", "a.xsd"),
            cancellationToken
        );

        await Assert.That(version11).IsEqualTo(XstsOutcome.NotImplemented);
        await Assert.That(brokenSchema).IsEqualTo(XstsOutcome.NotImplemented);
        await Assert.That(missingSchema).IsEqualTo(XstsOutcome.NotImplemented);
        await Assert.That(noInstance).IsEqualTo(XstsOutcome.NotImplemented);
        await Assert.That(missingInstance).IsEqualTo(XstsOutcome.NotImplemented);
    }

    [Test]
    public async Task ValidateInstance_AGroupWithoutSchemaDocument_FollowsTheSchemaLocationHint(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = Suite();
        _ = suite.Write(
            "n.xsd",
            "<xs:schema xmlns:xs=\""
                + Xs
                + "\" targetNamespace=\"urn:n\" xmlns=\"urn:n\"><xs:element name=\"e\" type=\"xs:int\"/></xs:schema>"
        );
        _ = suite.Write(
            "hint-ok.xml",
            "<e xmlns=\"urn:n\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"urn:n n.xsd\">1</e>"
        );
        _ = suite.Write(
            "hint-bad.xml",
            "<e xmlns=\"urn:n\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"urn:n n.xsd\">x</e>"
        );
        var validator = new InstanceXstsValidator(suite.Path);

        var valid = await validator.ValidateInstanceAsync(
            Case(XstsVersions.V10, true, "hint-ok.xml", "hinted"),
            cancellationToken
        );
        var invalid = await validator.ValidateInstanceAsync(
            Case(XstsVersions.V10, false, "hint-bad.xml", "hinted"),
            cancellationToken
        );

        await Assert.That(valid).IsEqualTo(XstsOutcome.Valid);
        await Assert.That(invalid).IsEqualTo(XstsOutcome.Invalid);
    }

    [Test]
    public async Task CompileSchema_IsTheSchemaValidatorOfTheSchemaTests(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = Suite();
        var xstsCase = Case(XstsVersions.V10, true, null, "g", "a.xsd") with
        {
            Case = new XstsCase("set", "g", "n", XstsKind.Schema, ["a.xsd"], null, "accepted", [], []),
        };

        var outcome = await new InstanceXstsValidator(suite.Path).CompileSchemaAsync(xstsCase, cancellationToken);

        await Assert.That(outcome).IsEqualTo(XstsOutcome.Valid);
    }

    [Test]
    public async Task Constructor_AndValidateInstance_GuardTheirArguments(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var suite = Suite();

        await Assert.That(() => new InstanceXstsValidator(" ")).Throws<ArgumentException>();
        await Assert
            .That(async () =>
                await new InstanceXstsValidator(suite.Path).ValidateInstanceAsync(null!, cancellationToken)
            )
            .Throws<ArgumentNullException>();
    }
}
