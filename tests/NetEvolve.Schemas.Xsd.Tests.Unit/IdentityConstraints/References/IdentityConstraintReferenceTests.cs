namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.References;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.IdentityConstraints.References;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

public class IdentityConstraintReferenceTests
{
    private readonly ValidationIssueCollector _collector = new(ValidationLimits.Default);
    private readonly FakeComponentRegistry _registry = new([]);

    private XsdIssueReporter Reporter => new(_collector);

    private IdentityConstraintReference? Create(string constraint, XsdVersion version = XsdVersion.V11) =>
        IdentityConstraintReference.Create(
            ReferenceFixture.Constraint(ReferenceFixture.Schema("urn:t", [constraint])),
            ReferenceFixture.Scope,
            version,
            Reporter
        );

    private IdentityConstraintDefinition Register(string namespaceName, string kind)
    {
        var schema = ReferenceFixture.Schema(namespaceName, [ReferenceFixture.Definition(kind, "k")]);
        return IdentityConstraintDefinition.Create(
            ReferenceFixture.Constraint(schema),
            new SchemaNamespaceScope(namespaceName, false),
            _registry,
            Reporter
        )!;
    }

    private async Task AssertSingleIssue(string ruleId, string? clause, int line, int column)
    {
        var issues = _collector.ToResult().Issues;
        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ReferenceFixture.CodeOf(ruleId));
        await Assert.That(issues[0].Constraint).IsEqualTo(clause ?? ruleId);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(issues[0].Location.Column).IsEqualTo(column);
    }

    [Test]
    [Arguments("unique", "Unique")]
    [Arguments("key", "Key")]
    [Arguments("keyref", "Keyref")]
    public async Task Create_Ref_RecordsKindAndExpandedName(string element, string kind)
    {
        var reference = Create($"<xs:{element} ref=\"o:k\"/>");

        await Assert.That(reference).IsNotNull();
        await Assert.That(reference!.ConstraintKind.ToString()).IsEqualTo(kind);
        await Assert.That(reference.Name).IsEqualTo(new XPathQName("urn:o", "k"));
        await Assert.That(reference.Target).IsNull();
        await Assert.That(_collector.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Create_RefWithIdAndAnnotation_IsAccepted()
    {
        var reference = Create(
            "<xs:key id=\"i\" ref=\"t:k\"><xs:annotation><xs:documentation>d</xs:documentation></xs:annotation></xs:key>"
        );

        await Assert.That(reference).IsNotNull();
        await Assert.That(_collector.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("unique")]
    [Arguments("key")]
    [Arguments("keyref")]
    public async Task Resolve_SameKind_BindsTheRegisteredInstance(string element)
    {
        var target = Register("urn:t", element);
        var reference = Create($"<xs:{element} ref=\"t:k\"/>")!;

        var resolved = reference.Resolve(_registry, Reporter);

        await Assert.That(resolved).IsTrue();
        await Assert.That(reference.Target).IsSameReferenceAs(target);
        await Assert.That(_collector.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Resolve_ConstraintOfAnImportedNamespace_BindsTheRegisteredInstance()
    {
        var target = Register("urn:o", "key");
        var reference = Create("<xs:key ref=\"o:k\"/>")!;

        await Assert.That(reference.Resolve(_registry, Reporter)).IsTrue();
        await Assert.That(reference.Target).IsSameReferenceAs(target);
    }

    [Test]
    [Arguments("key", "unique")]
    [Arguments("unique", "key")]
    [Arguments("keyref", "key")]
    [Arguments("key", "keyref")]
    public async Task Resolve_ConstraintOfAnotherKind_IsSchemaErrorWithRuleCodeAndLocation(
        string target,
        string referrer
    )
    {
        _ = Register("urn:t", target);
        var reference = Create($"<xs:{referrer} ref=\"t:k\"/>")!;

        var resolved = reference.Resolve(_registry, Reporter);

        await Assert.That(resolved).IsFalse();
        await Assert.That(reference.Target).IsNull();
        await AssertSingleIssue("src-identity-constraint", "src-identity-constraint.5", 3, 1);
    }

    [Test]
    public async Task Resolve_Unresolved_IsSchemaErrorWithRuleCodeAndLocation()
    {
        var reference = Create("<xs:unique ref=\"t:missing\"/>")!;

        var resolved = reference.Resolve(_registry, Reporter);

        await Assert.That(resolved).IsFalse();
        await Assert.That(reference.Target).IsNull();
        await AssertSingleIssue("src-resolve", null, 3, 1);
    }

    [Test]
    [Arguments("<xs:key ref=\"t:k\" name=\"n\"/>", "src-identity-constraint.1")]
    [Arguments("<xs:key ref=\"t:k\"><xs:selector xpath=\"a\"/></xs:key>", "src-identity-constraint.4")]
    [Arguments("<xs:key ref=\"t:k\"><xs:field xpath=\"@x\"/></xs:key>", "src-identity-constraint.4")]
    [Arguments(
        "<xs:key ref=\"t:k\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@x\"/></xs:key>",
        "src-identity-constraint.4"
    )]
    [Arguments("<xs:keyref ref=\"t:k\" refer=\"t:k\"/>", "src-identity-constraint.4")]
    public async Task Create_RefTogetherWithNameSelectorFieldOrRefer_IsSchemaError(string constraint, string clause)
    {
        var reference = Create(constraint);

        await Assert.That(reference).IsNull();
        await AssertSingleIssue("src-identity-constraint", clause, 3, 1);
    }

    [Test]
    public async Task Create_RefWithUndeclaredPrefix_IsSchemaError()
    {
        var reference = Create("<xs:key ref=\"q:k\"/>");

        await Assert.That(reference).IsNull();
        await AssertSingleIssue("src-resolve", null, 3, 1);
    }

    [Test]
    public async Task Create_SameFixture_IsLocatedSchemaErrorInStrict10AndCompilesInVersion11()
    {
        const string fixture = "<xs:key ref=\"t:k\"/>";

        var strict = Create(fixture, XsdVersion.V10);

        await Assert.That(strict).IsNull();
        await AssertSingleIssue("strict10-identity-constraint-ref", null, 3, 1);

        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var lenient = IdentityConstraintReference.Create(
            ReferenceFixture.Constraint(ReferenceFixture.Schema("urn:t", [fixture])),
            ReferenceFixture.Scope,
            XsdVersion.V11,
            new XsdIssueReporter(collector)
        );

        await Assert.That(lenient).IsNotNull();
        await Assert.That(collector.Count).IsEqualTo(0);
    }
}
