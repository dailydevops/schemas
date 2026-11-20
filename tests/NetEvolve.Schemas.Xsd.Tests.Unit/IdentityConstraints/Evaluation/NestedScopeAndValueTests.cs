namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

public class NestedScopeAndValueTests
{
    [Test]
    public async Task NestedScopes_OfTheSameConstraint_KeepSeparateTables()
    {
        var set = new ConstraintSet().On("s", IdcFixture.Unique("u", "e", "@a"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<s>\n<e a=\"1\"/>\n<s>\n<e a=\"1\"/>\n<e a=\"1\"/>\n</s>\n</s>\n</r>",
            set.Listener()
        );

        // The outer s sees one element with a=1; only the inner s sees two.
        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 6);
    }

    [Test]
    public async Task NestedScopes_WithADescendantSelector_LetTheOuterScopeSeeTheInnerElementsToo()
    {
        var set = new ConstraintSet().On("s", IdcFixture.Unique("u", ".//e", "@a"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(),
            "<r>\n<s>\n<e a=\"1\"/>\n<s>\n<e a=\"1\"/>\n</s>\n</s>\n</r>",
            set.Listener()
        );

        // The outer table holds both elements, the inner one only its own.
        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 5);
    }

    [Test]
    public async Task SiblingScopes_OfTheSameConstraint_KeepSeparateTables()
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
    public async Task Decimal_OneAndOnePointZero_AreEqual()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", "e", "@a"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(SchemaFixture.Simple("decimal")),
            "<r>\n<e a=\"1\"/>\n<e a=\"1.0\"/>\n<e a=\"+01.00\"/>\n</r>",
            set.Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(2);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
        await IdcAssert.Issue(issues[1], "cvc-identity-constraint.4.1", 4);
    }

    [Test]
    public async Task String_OneAndOnePointZero_AreDifferent()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", "e", "@a"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r><e a=\"1\"/><e a=\"1.0\"/></r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Field_UnionOfAnAttributeAndAnElement_ComparesTheValuesAlike()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", "e", "@a|k"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r>\n<e a=\"1\"/>\n<e><k>1</k></e>\n</r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task Version10_ComparesLikeVersion11_ForDecimals()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", "e", "@a"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(SchemaFixture.Simple("decimal")),
            "<r><e a=\"2\"/><e a=\"2.0\"/></r>",
            set.Listener(XsdVersion.V10)
        );

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task DefaultedAttribute_CountsAsAField()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Unique("u", "e", "@c"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(attributeC: new AttributeValueConstraint(AttributeValueConstraintKind.Default, "d")),
            "<r>\n<e/>\n<e/>\n</r>",
            set.Listener()
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task InvalidFieldValue_LeavesTheElementOutAndAddsNoIssueOfItsOwn()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Key("k", "e", "@a"));

        var issues = IdcFixture.Run(
            IdcFixture.Schema(SchemaFixture.Simple("int")),
            "<r>\n<e a=\"x\"/>\n<e a=\"x\"/>\n</r>",
            set.Listener()
        );

        // Only the two datatype issues of the validator: neither a missing field nor a duplicate key on top of them.
        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Code).IsNotEqualTo(IdcFixture.Code("cvc-identity-constraint"));
        await Assert.That(issues[1].Code).IsNotEqualTo(IdcFixture.Code("cvc-identity-constraint"));
    }

    [Test]
    public async Task FieldElementWhoseContentWasRejected_IsLeftOutWithoutAnIssueOfItsOwn()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Key("k", "e", "k"));

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r>\n<e><k><x/></k></e>\n</r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsNotEqualTo(IdcFixture.Code("cvc-identity-constraint"));
    }

    [Test]
    public async Task Listener_RunTwice_StartsTheSecondRunWithEmptyTables()
    {
        var listener = new ConstraintSet().On("r", IdcFixture.Key("k", "e", "@a")).Listener();
        var schema = IdcFixture.Schema();

        var first = IdcFixture.Run(schema, "<r><e a=\"1\"/></r>", listener);
        var second = IdcFixture.Run(schema, "<r><e a=\"1\"/></r>", listener);

        await Assert.That(first.Count).IsEqualTo(0);
        await Assert.That(second.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Declaration_WithoutConstraints_CostsNothing()
    {
        var listener = new ConstraintSet().Listener();

        var issues = IdcFixture.Run(IdcFixture.Schema(), "<r><e a=\"1\"/><e a=\"1\"/></r>", listener);

        await Assert.That(issues.Count).IsEqualTo(0);
    }
}
