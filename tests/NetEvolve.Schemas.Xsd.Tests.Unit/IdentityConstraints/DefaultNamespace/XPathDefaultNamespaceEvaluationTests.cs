namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.DefaultNamespace;

using NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;
using static NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.DefaultNamespace.DefaultNamespaceFixture;

/// <summary>
/// The instances are in the namespace <c>urn:t</c> (or in none), so a name step finds its element only when the
/// <c>xpathDefaultNamespace</c> in force gives it that namespace. The attributes are always in no namespace.
/// </summary>
public class XPathDefaultNamespaceEvaluationTests
{
    [Test]
    public async Task Selector_WithTheAttribute_MatchesUnprefixedNamesInThatNamespace()
    {
        var issues = Run(Schema(string.Empty, Unique("e", Tns, "@a", null)), EqualAttributes);

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task Selector_WithTheAttribute_DoesNotMatchAnotherNamespace()
    {
        var issues = Run(Schema(string.Empty, Unique("e", "urn:other", "@a", null)), EqualAttributes);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Selector_WithoutAnyAttribute_MeansNoNamespace()
    {
        var issues = Run(Schema(string.Empty, Unique("e", null, "@a", null)), EqualAttributes);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Selector_WithoutAnyAttribute_StillMatchesNoNamespaceInstances()
    {
        var issues = Run(Schema(string.Empty, Unique("e", null, "@a", null)), EqualAttributes, namespaced: false);

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Selector_DescendantAndPathSteps_AreQualifiedToo()
    {
        var issues = Run(
            Schema(string.Empty, Unique(".//s/e|s/s/e", Tns, "@a", null)),
            "<s><e a=\"1\"/></s>\n<s><s><e a=\"1\"/></s></s>"
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task Field_WithTheAttribute_MatchesUnprefixedNamesInThatNamespace()
    {
        var issues = Run(Schema(string.Empty, Unique("e", Tns, "k", Tns)), EqualChildren);

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task Field_WithoutTheAttribute_MeansNoNamespaceEvenWhenTheSelectorHasOne()
    {
        var issues = Run(Schema(string.Empty, Unique("e", Tns, "k", null)), EqualChildren);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Field_Attribute_IsNotAffected()
    {
        // The default namespace is for element names: the attribute a is in no namespace although the field has the attribute.
        var issues = Run(Schema(string.Empty, Unique("e", Tns, "@a", Tns)), EqualAttributes);

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Field_Wildcards_AreNotAffected()
    {
        var issues = Run(Schema(string.Empty, Unique("e", Tns, "@*", Tns)), EqualAttributes);

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task SchemaElement_GivesTheValueWhenSelectorAndFieldHaveNone()
    {
        var issues = Run(Schema(Attr(Tns), Unique("e", null, "k", null)), EqualChildren);

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task SchemaElement_IsOverriddenBySelectorAndField()
    {
        var issues = Run(Schema(Attr(Tns), Unique("e", "##local", "k", "urn:other")), EqualChildren);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SchemaElement_WithAnotherNamespace_MatchesNothing()
    {
        var issues = Run(Schema(Attr("urn:other"), Unique("e", null, "k", null)), EqualChildren);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TargetNamespace_MatchesTheNamespaceOfTheSchema()
    {
        var issues = Run(
            Schema(string.Empty, Unique("e", "##targetNamespace", "k", "##targetNamespace")),
            EqualChildren
        );

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TargetNamespace_DoesNotMatchNoNamespaceInstances()
    {
        var issues = Run(
            Schema(Attr("##targetNamespace"), Unique("e", null, "@a", null)),
            EqualAttributes,
            namespaced: false
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DefaultNamespace_MatchesTheDefaultNamespaceOfTheSchemaDocument()
    {
        var issues = Run(
            Schema(" xmlns=\"urn:t\"" + Attr("##defaultNamespace"), Unique("e", null, "k", null)),
            EqualChildren
        );

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DefaultNamespace_WithoutOneInScopeMeansNoNamespace()
    {
        var issues = Run(
            Schema(Attr("##defaultNamespace"), Unique("e", null, "@a", null)),
            EqualAttributes,
            namespaced: false
        );

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Local_MeansNoNamespaceAndMatchesNoNamespaceInstances()
    {
        var issues = Run(Schema(Attr(Tns), Unique("e", "##local", "@a", null)), EqualAttributes, namespaced: false);

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Local_DoesNotMatchNamespacedInstances()
    {
        var issues = Run(Schema(string.Empty, Unique("e", "##local", "@a", null)), EqualAttributes);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task PrefixedNames_KeepTheirOwnNamespace()
    {
        var issues = Run(Schema(" xmlns:q=\"urn:t\"", Unique("q:e", "urn:other", "@a", null)), EqualAttributes);

        await Assert.That(issues.Count).IsEqualTo(1);
    }
}
