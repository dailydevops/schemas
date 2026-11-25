namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System.Collections.Generic;

public class SelectorTests
{
    private static IReadOnlyList<ValidationIssue> Run(string selector, string xml, string field = "@a", string ns = "")
    {
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", selector, field));
        return IdcFixture.Run(IdcFixture.Schema(ns: ns), xml, set.Listener());
    }

    // Two elements with a=1 are at the given places: a duplicate is found exactly when the selector reaches both.
    private const string Places = "<r>\n<e a=\"1\"/>\n<s>\n<e a=\"1\"/>\n<s><e a=\"1\"/></s>\n</s>\n</r>";

    [Test]
    public async Task Self_SelectsTheScopeElementItself()
    {
        var set = new ConstraintSet().On("e", IdcFixture.Key("k", ".", "@a"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r>\n<e a=\"1\"/>\n<e/>\n</r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.2.1", 3);
    }

    [Test]
    public async Task Name_SelectsDirectChildrenOnly()
    {
        var issues = Run("e", Places);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Descendants_SelectsAtAnyDepth()
    {
        var issues = Run(".//e", Places);

        await Assert.That(issues.Count).IsEqualTo(2);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 4);
        await IdcAssert.Issue(issues[1], "cvc-identity-constraint.4.1", 5);
    }

    [Test]
    public async Task Path_SelectsThroughTheNamedSteps()
    {
        var issues = Run("s/e", "<r>\n<s><e a=\"1\"/></s>\n<s><e a=\"1\"/></s>\n<e a=\"1\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task Path_WithSelfSteps_IgnoresThem()
    {
        var issues = Run("./s/./e", "<r>\n<s><e a=\"1\"/></s>\n<s><e a=\"1\"/></s>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DescendantPath_MatchesTheEndOfTheAncestorChain()
    {
        var issues = Run(".//s/e", "<r>\n<s><e a=\"1\"/></s>\n<s><s><e a=\"1\"/></s></s>\n<e a=\"1\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task DescendantSelf_SelectsTheScopeElementAndEveryDescendant()
    {
        var set = new ConstraintSet().On("s", IdcFixture.Key("k", ".//.", "@a"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r>\n<s>\n<e a=\"1\"/>\n</s>\n</r>", set.Listener());

        // The scope element s has no attribute a: a key reports it. The element e has one.
        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.2.1", 2);
    }

    [Test]
    public async Task Star_SelectsEveryChild()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Key("k", "*", "@a"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r>\n<e a=\"1\"/>\n<s/>\n</r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.2.1", 3);
    }

    [Test]
    public async Task Union_SelectsWhatEveryPathSelects()
    {
        var issues = Run("e|s/e", Places);

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 4);
    }

    [Test]
    public async Task Selector_DoesNotSelectTheScopeElementItselfByAName()
    {
        var set = new ConstraintSet().On("e", IdcFixture.Key("k", "e", "@a"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r><e/></r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task QualifiedName_SelectsTheElementsOfThatNamespace()
    {
        var issues = Run("t:e", "<r xmlns=\"urn:t\">\n<e a=\"1\"/>\n<e a=\"1\"/>\n</r>", ns: IdcFixture.Ns);

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task UnprefixedName_SelectsNamesInNoNamespaceOnly()
    {
        var issues = Run("e", "<r xmlns=\"urn:t\">\n<e a=\"1\"/>\n<e a=\"1\"/>\n</r>", ns: IdcFixture.Ns);

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task NamespaceWildcard_SelectsEveryNameOfTheNamespace()
    {
        var issues = Run("t:*", "<r xmlns=\"urn:t\">\n<e a=\"1\"/>\n<e a=\"1\"/>\n</r>", ns: IdcFixture.Ns);

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task NamespaceWildcard_DoesNotSelectAnotherNamespace()
    {
        var issues = Run("p:*", "<r>\n<e a=\"1\"/>\n<e a=\"1\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task QualifiedAttribute_NeedsTheNamespaceOfTheAttribute()
    {
        // The attributes of the fixture are in no namespace, so a field that names one in urn:t finds nothing.
        var set = new ConstraintSet().On("r", IdcFixture.Key("k", "e", "@t:a"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r>\n<e a=\"1\"/>\n</r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.2.1", 2);
    }

    [Test]
    public async Task LongAxisForms_AreEvaluatedLikeTheirAbbreviations()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", "child::s/child::e", "attribute::a"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<s><e a=\"1\"/></s>\n<s><e a=\"1\"/></s>\n</r>",
            set.Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task AttributeWildcard_AsField_TakesTheOnlyAttribute()
    {
        var issues = Run("e", "<r>\n<e a=\"1\"/>\n<e b=\"1\"/>\n</r>", "@*");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task AttributeNamespaceWildcard_AsField_DoesNotMatchAttributesInNoNamespace()
    {
        var issues = Run("e", "<r>\n<e a=\"1\"/>\n<e a=\"1\"/>\n</r>", "@t:*");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AttributeWildcard_AsField_CountsTheSchemaInstanceAttributes()
    {
        var issues = Run(
            "e",
            "<r xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\n<e a=\"1\" xsi:nil=\"true\"/>\n</r>",
            "@*"
        );

        await Assert.That(issues.Any(i => i.Constraint == "cvc-identity-constraint.3")).IsTrue();
    }
}
