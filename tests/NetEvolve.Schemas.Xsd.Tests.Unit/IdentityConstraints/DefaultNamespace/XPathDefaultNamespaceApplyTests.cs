namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.DefaultNamespace;

using System;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.DefaultNamespace;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using static NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.DefaultNamespace.DefaultNamespaceFixture;

public class XPathDefaultNamespaceApplyTests
{
    private static SchemaSyntaxNode Apply(string xpath, string? value, string schemaAttributes = "")
    {
        var constraint = Constraint(Schema(schemaAttributes, Unique(xpath, value, "@a", null)));
        XPathDefaultNamespace.Apply(constraint, Scope, XsdVersion.V11);
        return constraint.GetChildNode(0);
    }

    [Test]
    [Arguments("a", "xdn:a")]
    [Arguments("a/b", "xdn:a/xdn:b")]
    [Arguments(".//a", ".//xdn:a")]
    [Arguments("./a/./b", "./xdn:a/./xdn:b")]
    [Arguments("a|b/c", "xdn:a|xdn:b/xdn:c")]
    [Arguments("child::a", "child::xdn:a")]
    [Arguments("a/@b", "xdn:a/@b")]
    [Arguments("a/attribute::b", "xdn:a/attribute::b")]
    [Arguments(" a / b ", " xdn:a / xdn:b ")]
    public async Task Apply_UnprefixedElementNameTests_GetThePrefixOfTheNamespace(string xpath, string expected)
    {
        var selector = Apply(xpath, "urn:q");

        await Assert.That(selector.FindAttribute("xpath")).IsEqualTo(expected);
        await Assert.That(selector.TryResolvePrefix("xdn", out var uri)).IsTrue();
        await Assert.That(uri).IsEqualTo("urn:q");
    }

    [Test]
    [Arguments("*")]
    [Arguments("t:a")]
    [Arguments("t:*")]
    [Arguments("*:a")]
    [Arguments("@a")]
    [Arguments("@*")]
    [Arguments("xml:a")]
    [Arguments(".")]
    [Arguments(".//.")]
    public async Task Apply_OtherSteps_AreLeftAsWritten(string xpath)
    {
        var selector = Apply(xpath, "urn:q", " xmlns:t=\"urn:t\"");

        await Assert.That(selector.FindAttribute("xpath")).IsEqualTo(xpath);
        await Assert.That(selector.TryResolvePrefix("xdn", out _)).IsFalse();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("##local")]
    public async Task Apply_NoNamespace_LeavesThePathAlone(string? value)
    {
        var selector = Apply("a/b", value);

        await Assert.That(selector.FindAttribute("xpath")).IsEqualTo("a/b");
        await Assert.That(selector.TryResolvePrefix("xdn", out _)).IsFalse();
    }

    [Test]
    public async Task Apply_NamespaceOfTheSchema_QualifiesSelectorAndFieldWithTheirOwnPrefix()
    {
        var constraint = Constraint(Schema(Attr("urn:s"), Unique("a", null, "b", "urn:f")));

        XPathDefaultNamespace.Apply(constraint, Scope, XsdVersion.V11);

        var selector = constraint.GetChildNode(0);
        var field = constraint.GetChildNode(1);
        await Assert.That(selector.FindAttribute("xpath")).IsEqualTo("xdn:a");
        await Assert.That(field.FindAttribute("xpath")).IsEqualTo("xdn:b");
        await Assert.That(selector.TryResolvePrefix("xdn", out var selectorUri)).IsTrue();
        await Assert.That(selectorUri).IsEqualTo("urn:s");
        await Assert.That(field.TryResolvePrefix("xdn", out var fieldUri)).IsTrue();
        await Assert.That(fieldUri).IsEqualTo("urn:f");
    }

    [Test]
    public async Task Apply_PrefixIsDeclaredOnlyOnTheQualifiedPath()
    {
        var constraint = Constraint(Schema(string.Empty, Unique("a", "urn:q", "b", null)));

        XPathDefaultNamespace.Apply(constraint, Scope, XsdVersion.V11);

        await Assert.That(constraint.GetChildNode(1).TryResolvePrefix("xdn", out _)).IsFalse();
        await Assert.That(constraint.GetChildNode(1).FindAttribute("xpath")).IsEqualTo("b");
    }

    [Test]
    public async Task Apply_Twice_QualifiesOnce()
    {
        var constraint = Constraint(Schema(string.Empty, Unique("a/b", "urn:q", "@a", null)));
        XPathDefaultNamespace.Apply(constraint, Scope, XsdVersion.V11);
        var once = constraint.GetChildNode(0).FindAttribute("xpath");

        XPathDefaultNamespace.Apply(constraint, Scope, XsdVersion.V11);

        await Assert.That(constraint.GetChildNode(0).FindAttribute("xpath")).IsEqualTo(once);
        await Assert.That(constraint.GetChildNode(0).TryResolvePrefix("xdn1", out _)).IsFalse();
    }

    [Test]
    public async Task Apply_PrefixDeclaredInTheSchema_PicksAnotherPrefix()
    {
        var selector = Apply("a", "urn:q", " xmlns:xdn=\"urn:other\"");

        await Assert.That(selector.FindAttribute("xpath")).IsEqualTo("xdn1:a");
        await Assert.That(selector.TryResolvePrefix("xdn", out var other)).IsTrue();
        await Assert.That(other).IsEqualTo("urn:other");
        await Assert.That(selector.TryResolvePrefix("xdn1", out var chosen)).IsTrue();
        await Assert.That(chosen).IsEqualTo("urn:q");
    }

    [Test]
    public async Task Apply_PrefixAlreadyUsedInThePath_PicksAnotherPrefix()
    {
        // The prefix xdn is undeclared in the path: the definition reports it, so the synthesized prefix must not bind it.
        var selector = Apply("xdn:a/b", "urn:q");

        await Assert.That(selector.FindAttribute("xpath")).IsEqualTo("xdn:a/xdn1:b");
        await Assert.That(selector.TryResolvePrefix("xdn", out _)).IsFalse();
    }

    [Test]
    public async Task Apply_SeveralPrefixesTaken_CountsUp()
    {
        var selector = Apply("a", "urn:q", " xmlns:xdn=\"urn:1\" xmlns:xdn1=\"urn:2\"");

        await Assert.That(selector.FindAttribute("xpath")).IsEqualTo("xdn2:a");
    }

    [Test]
    public async Task Apply_PathThatDoesNotParse_IsLeftForTheDefinitionToReport()
    {
        var selector = Apply("a[", "urn:q");

        await Assert.That(selector.FindAttribute("xpath")).IsEqualTo("a[");
    }

    [Test]
    public async Task Apply_ElementsWithoutXPath_AreSkipped()
    {
        var schema = Schema(
            string.Empty,
            "<xs:unique name=\"u\"><xs:annotation/><xs:selector xpathDefaultNamespace=\"urn:q\"/><xs:field xpath=\"a\" xpathDefaultNamespace=\"urn:q\"/></xs:unique>"
        );
        var constraint = Constraint(schema);

        XPathDefaultNamespace.Apply(constraint, Scope, XsdVersion.V11);

        await Assert.That(constraint.GetChildNode(1).FindAttribute("xpath")).IsNull();
        await Assert.That(constraint.GetChildNode(2).FindAttribute("xpath")).IsEqualTo("xdn:a");
    }

    [Test]
    public async Task Apply_Strict10_LeavesEverythingAsWritten()
    {
        var constraint = Constraint(Schema(Attr("urn:s"), Unique("a", "urn:q", "b", "urn:f")));

        XPathDefaultNamespace.Apply(constraint, Scope, XsdVersion.V10);

        await Assert.That(constraint.GetChildNode(0).FindAttribute("xpath")).IsEqualTo("a");
        await Assert.That(constraint.GetChildNode(1).FindAttribute("xpath")).IsEqualTo("b");
    }

    [Test]
    public async Task Create_AfterApply_HasNoIssueAndBindsThePrefixOfTheSelector()
    {
        var constraint = Constraint(Schema(string.Empty, Unique("a|.//b", "urn:q", "@x", null)));
        XPathDefaultNamespace.Apply(constraint, Scope, XsdVersion.V11);
        var collector = new ValidationIssueCollector(ValidationLimits.Default);

        var definition = IdentityConstraintDefinition.Create(
            constraint,
            Scope,
            new FakeComponentRegistry([]),
            new XsdIssueReporter(collector)
        );

        await Assert.That(collector.Count).IsEqualTo(0);
        await Assert.That(definition).IsNotNull();
        await Assert.That(definition!.Selector).IsEqualTo("xdn:a|.//xdn:b");
        await Assert.That(definition.SelectorBindings).Count().IsEqualTo(1);
        await Assert.That(definition.SelectorBindings[0].Key).IsEqualTo("xdn");
        await Assert.That(definition.SelectorBindings[0].Value).IsEqualTo("urn:q");
        await Assert.That(definition.FieldBindings[0]).IsEmpty();
    }

    [Test]
    public async Task Apply_Guards_Throw()
    {
        var constraint = Constraint(Schema(string.Empty, Unique("a", null, "b", null)));

        await Assert
            .That(() => XPathDefaultNamespace.Apply(null!, Scope, XsdVersion.V11))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => XPathDefaultNamespace.Apply(constraint, Scope, (XsdVersion)(-1)))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => XPathDefaultNamespace.Apply(constraint, Scope, (XsdVersion)99))
            .Throws<ArgumentOutOfRangeException>();
    }
}
