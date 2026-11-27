namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System.Collections.Generic;

/// <summary>
/// A nilled element has no value (Structures 3.11.4, both versions): as a field element it contributes no value, so the
/// selected element is not in the qualified node set; as a selected element it takes part with its attributes.
/// </summary>
public class NilledElementTests
{
    private const string Open = "<r xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">";

    private static IReadOnlyList<ValidationIssue> Run(ConstraintSet set, string body) =>
        IdcFixture.Run(IdcFixture.Schema(nillable: true), Open + body + "</r>", set.Listener());

    [Test]
    public async Task Unique_NilledFieldElement_LeavesTheSelectedElementOut()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", "e", "k"));

        var issues = Run(set, "<e><k xsi:nil=\"true\"/></e><e><k xsi:nil=\"true\"/></e><e><k>1</k></e><e><k>1</k></e>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-identity-constraint.4.1");
    }

    [Test]
    public async Task Key_NilledFieldElement_ReportsTheMissingValueAtTheSelectedElement()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Key("k", "e", "k"));

        var issues = Run(set, "\n<e><k>1</k></e>\n<e><k xsi:nil=\"true\"/></e>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.2.1", 3);
    }

    [Test]
    public async Task Keyref_NilledFieldElement_LeavesTheSelectedElementOut()
    {
        var set = new ConstraintSet().On(
            "r",
            IdcFixture.Key("kk", "e", "@a"),
            IdcFixture.Keyref("kr", "t:kk", "e", "k")
        );

        var issues = Run(set, "<e a=\"1\"><k xsi:nil=\"true\"/></e><e a=\"2\"><k>9</k></e>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("cvc-identity-constraint.4.3");
    }

    [Test]
    public async Task NilledSelectedElement_TakesPartWithItsAttributes()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Key("k", "e", "@a"));

        var issues = Run(set, "\n<e a=\"1\" xsi:nil=\"true\"/>\n<e a=\"1\" xsi:nil=\"true\"/>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.2.2", 3);
    }

    [Test]
    public async Task Key_NilledSelectedElement_HasNoElementFieldValue()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Key("k", "e", "k"));

        var issues = Run(set, "\n<e xsi:nil=\"true\"/>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.2.1", 2);
    }
}
