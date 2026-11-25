namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

public class ListenerEdgeTests
{
    [Test]
    public async Task Hooks_BeforeTheRunBegan_ReportAndStoreNothing()
    {
        var listener = new ConstraintSet().On("r", IdcFixture.Key("k", "e", "@a")).Listener();

        listener.StartElement(default);
        listener.Attribute(default);
        listener.SimpleValue(default);
        listener.EndElement(default);
        listener.EndRun();

        await Assert.That(listener.Interest).IsEqualTo(ListenerInterest.All | ListenerInterest.XsiAttribute);
    }

    [Test]
    public async Task Scopes_NestedDeeperThanTheInitialStack_StillKeepSeparateTables()
    {
        var set = new ConstraintSet().On("s", IdcFixture.Unique("u", "e", "@a"));
        var xml = new StringBuilder("<r>");
        for (var i = 0; i < 40; i++)
        {
            _ = xml.Append("<s><e a=\"1\"/>");
        }

        for (var i = 0; i < 40; i++)
        {
            _ = xml.Append("</s>");
        }

        var issues = IdcFixture.Run(IdcFixture.Schema(), xml.Append("</r>").ToString(), set.Listener());

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fields_ManyOnOneElement_AllGetTheirValue()
    {
        var fields = new string[20];
        System.Array.Fill(fields, ".");
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", ".//k", fields));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r>\n<e><k>1</k></e>\n<e><k>1</k></e>\n</r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task InvalidFieldElementValue_LeavesTheSelectedElementOut()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Key("k", "e", "k"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(SchemaFixture.Simple("int")),
            "<r>\n<e><k>x</k></e>\n<e><k>x</k></e>\n</r>",
            set.Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Code).IsNotEqualTo(IdcFixture.Code("cvc-identity-constraint"));
        await Assert.That(issues[1].Code).IsNotEqualTo(IdcFixture.Code("cvc-identity-constraint"));
    }

    [Test]
    public async Task UndeclaredElements_HaveNoDeclarationAndOpenNoScope()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Key("k", "e", "@a"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<x><e a=\"1\"/><e a=\"1\"/></x>", set.Listener());

        // Only the validator's issue about the undeclared root.
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsNotEqualTo(IdcFixture.Code("cvc-identity-constraint"));
    }

    [Test]
    public async Task Keyref_ToAKeyOfADescendantThatNeverAppeared_IsUnresolved()
    {
        var set = new ConstraintSet()
            .On("s", IdcFixture.Key("k", "e", "@a"))
            .On("r", IdcFixture.Keyref("kr", "t:k", "e", "@b"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r>\n<e b=\"1\"/>\n</r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.3", 2);
    }

    [Test]
    public async Task Field_ElementWithSimpleContentInAComplexType_TakesTheValue()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", "e", "q"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r>\n<e><q>1</q></e>\n<e><q>1</q></e>\n</r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task Field_ElementOfABuiltInType_TakesTheValue()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", "e", "m"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r>\n<e><m>1</m></e>\n<e><m>1</m></e>\n</r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task Message_OfALongValue_IsCutAfterSixtyFourCharacters()
    {
        var value = new string('x', 100);
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", "e", "@a"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r><e a=\"" + value + "\"/><e a=\"" + value + "\"/></r>",
            set.Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Message).Contains(new string('x', 64) + "...'");
        await Assert.That(issues[0].Message).DoesNotContain(new string('x', 65));
    }

    [Test]
    public async Task Message_OfSeveralFields_ListsEveryValue()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", "e", "@a", "@b"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r><e a=\"1\" b=\"2\"/><e a=\"1\" b=\"2\"/></r>",
            set.Listener()
        );

        await Assert.That(issues[0].Message).Contains("['1', '2']");
    }

    [Test]
    public async Task ManyConstraintsOnOneElement_AreAllEvaluated()
    {
        var set = new ConstraintSet().On(
            "r",
            IdcFixture.Unique("u1", "e", "@a"),
            IdcFixture.Unique("u2", "e", "@b"),
            IdcFixture.Key("k", "e", "@a", "@b")
        );

        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<e a=\"1\" b=\"2\"/>\n<e a=\"1\" b=\"3\"/>\n<e a=\"4\" b=\"2\"/>\n</r>",
            set.Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(2);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
        await IdcAssert.Issue(issues[1], "cvc-identity-constraint.4.1", 4);
    }

    [Test]
    public async Task Listener_ReusedForAnotherDocument_ReusesTheCompiledPaths()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", "e", "@a"));
        var listener = set.Listener();
        var schema = IdcFixture.Schema();

        var first = IdcFixture.Run(schema, "<r><e a=\"1\"/><e a=\"1\"/></r>", listener);
        var second = IdcFixture.Run(schema, "<r><e a=\"1\"/><e a=\"1\"/></r>", listener);

        await Assert.That(first.Count).IsEqualTo(1);
        await Assert.That(second.Count).IsEqualTo(1);
    }
}
