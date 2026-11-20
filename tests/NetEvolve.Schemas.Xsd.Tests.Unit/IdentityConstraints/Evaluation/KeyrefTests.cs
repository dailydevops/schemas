namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System.Collections.Generic;

public class KeyrefTests
{
    // The key takes the attribute a, the keyref takes the attribute b of the same elements.
    private static ConstraintSet KeyAndKeyref(bool unique = false) =>
        new ConstraintSet().On(
            "r",
            unique ? IdcFixture.Unique("k", "e", "@a") : IdcFixture.Key("k", "e", "@a"),
            IdcFixture.Keyref("kr", "t:k", "e", "@b")
        );

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Keyref_ValueOfTheReferencedTable_Resolves(bool unique)
    {
        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<e a=\"1\"/>\n<e a=\"2\" b=\"1\"/>\n<e a=\"3\" b=\"2\"/>\n</r>",
            KeyAndKeyref(unique).Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Keyref_ReferenceBeforeItsKey_Resolves()
    {
        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<e a=\"1\" b=\"2\"/>\n<e a=\"2\"/>\n</r>",
            KeyAndKeyref().Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Keyref_UnresolvedValue_IsReportedAtItsElementWhenTheScopeEnds()
    {
        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<e a=\"1\" b=\"9\"/>\n<e a=\"1\"/>\n</r>",
            KeyAndKeyref().Listener()
        );

        // The duplicate key at line 3 is found while the elements stream by; the unresolved keyref waits for the end of r.
        await Assert.That(issues.Count).IsEqualTo(2);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.2.2", 3);
        await IdcAssert.Issue(issues[1], "cvc-identity-constraint.4.3", 2);
    }

    [Test]
    public async Task Keyref_SeveralUnresolvedValues_AreReportedInDocumentOrder()
    {
        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<e a=\"1\" b=\"8\"/>\n<e a=\"2\" b=\"9\"/>\n</r>",
            KeyAndKeyref().Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(2);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.3", 2);
        await IdcAssert.Issue(issues[1], "cvc-identity-constraint.4.3", 3);
    }

    [Test]
    public async Task Keyref_WithoutAnyKeyValue_IsUnresolved()
    {
        var set = new ConstraintSet().On(
            "r",
            IdcFixture.Key("k", "s", "@a"),
            IdcFixture.Keyref("kr", "t:k", "e", "@b")
        );

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r>\n<e b=\"1\"/>\n</r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.3", 2);
    }

    [Test]
    public async Task Keyref_MissingField_LeavesTheElementOutWithoutAnIssue()
    {
        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<e a=\"1\"/>\n<e a=\"2\"/>\n</r>",
            KeyAndKeyref().Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Keyref_SequencesOfSeveralFields_MustMatchAsAWhole()
    {
        var set = new ConstraintSet().On(
            "r",
            IdcFixture.Key("k", "e", "@a", "@b"),
            IdcFixture.Keyref("kr", "t:k", "s/e", "@a", "@b")
        );

        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<e a=\"1\" b=\"2\"/>\n<s><e a=\"1\" b=\"2\"/></s>\n<s><e a=\"2\" b=\"1\"/></s>\n</r>",
            set.Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.3", 4);
    }

    [Test]
    public async Task Keyref_UnresolvedReference_IsIgnoredBecauseTheSchemaIsInError()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Keyref("kr", "t:missing", "e", "@b"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r><e b=\"1\"/></r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Keyref_ReferencesTheKeyOfADescendantElement_ThroughTheTablesThatBubbleUp()
    {
        var set = new ConstraintSet()
            .On("s", IdcFixture.Key("k", "e", "@a"))
            .On("r", IdcFixture.Keyref("kr", "t:k", "e", "@b"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<s><e a=\"1\"/></s>\n<s><e a=\"2\"/></s>\n<e b=\"2\"/>\n<e b=\"3\"/>\n</r>",
            set.Listener()
        );

        // The tables of both s elements are merged for the keyref of r; the key values of different s are not compared.
        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.3", 5);
    }

    [Test]
    public async Task Key_ValuesOfDifferentDescendantScopes_DoNotCollide()
    {
        var set = new ConstraintSet().On("s", IdcFixture.Key("k", "e", "@a"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<s><e a=\"1\"/></s>\n<s><e a=\"1\"/></s>\n</r>",
            set.Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Keyref_ADescendantScopeThatDeclaresTheKeyItself_HidesTheTablesBelowIt()
    {
        // The keyref and the key sit on s. The inner s has its own key table, so the keyref of the outer s cannot see it.
        var set = new ConstraintSet().On(
            "s",
            IdcFixture.Key("k", "e", "@a"),
            IdcFixture.Keyref("kr", "t:k", "e", "@b")
        );

        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<s>\n<s><e a=\"1\"/></s>\n<e a=\"2\" b=\"1\"/>\n</s>\n</r>",
            set.Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.3", 4);
    }

    [Test]
    public async Task Keyref_ReferencesOnlyTheKeyItNames()
    {
        var set = new ConstraintSet()
            .On("s", IdcFixture.Key("ks", "e", "@a"))
            .On("r", IdcFixture.Key("k", "e", "@a"), IdcFixture.Keyref("kr", "t:k", "e", "@b"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<s><e a=\"7\"/></s>\n<e a=\"1\" b=\"7\"/>\n</r>",
            set.Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.3", 3);
    }

    [Test]
    public async Task Keyref_ValueThatTwoDescendantScopesHold_IsNotResolvedByEither()
    {
        // Structures 3.11.5: entries of descendant tables with the same key-sequence and different nodes are not included.
        var set = new ConstraintSet()
            .On("s", IdcFixture.Key("k", "e", "@a"))
            .On("r", IdcFixture.Keyref("kr", "t:k", "e", "@b"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<s><e a=\"1\"/><e a=\"2\"/></s>\n<s><e a=\"1\"/></s>\n<e b=\"1\"/>\n<e b=\"2\"/>\n</r>",
            set.Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.3", 4);
    }
}
