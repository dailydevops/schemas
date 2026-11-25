namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

public class UniqueAndKeyTests
{
    private static IReadOnlyList<ValidationIssue> Run(string constraint, string xml, string element = "r")
    {
        var set = new ConstraintSet().On(element, constraint);
        return IdcFixture.Run(IdcFixture.Schema(), xml, set.Listener());
    }

    [Test]
    public async Task Unique_DuplicateValue_ReportsAtTheSecondElement()
    {
        var issues = Run(IdcFixture.Unique("u", "e", "@a"), "<r>\n<e a=\"1\"/>\n<e a=\"2\"/>\n<e a=\"1\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 4);
    }

    [Test]
    public async Task Unique_ARepeatedValue_IsReportedAtEveryFurtherOccurrence()
    {
        var issues = Run(IdcFixture.Unique("u", "e", "@a"), "<r>\n<e a=\"1\"/>\n<e a=\"1\"/>\n<e a=\"1\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(2);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
        await IdcAssert.Issue(issues[1], "cvc-identity-constraint.4.1", 4);
    }

    [Test]
    public async Task Unique_DistinctValues_ReportNothing()
    {
        var issues = Run(IdcFixture.Unique("u", "e", "@a"), "<r><e a=\"1\"/><e a=\"2\"/><e a=\"A\"/><e a=\"a\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Unique_ElementsWithoutTheField_AreLeftOut()
    {
        var issues = Run(IdcFixture.Unique("u", "e", "@a"), "<r><e/><e/><e a=\"1\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Unique_ValuesOfTypeString_DifferInCaseAndWhiteSpace()
    {
        var issues = Run(IdcFixture.Unique("u", "e", "@a"), "<r><e a=\"x\"/><e a=\"X\"/><e a=\"x \"/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Key_DuplicateValue_ReportsAtTheSecondElement()
    {
        var issues = Run(IdcFixture.Key("k", "e", "@a"), "<r>\n<e a=\"1\"/>\n<e a=\"1\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.2.2", 3);
    }

    [Test]
    public async Task Key_MissingField_ReportsAtTheSelectedElement()
    {
        var issues = Run(IdcFixture.Key("k", "e", "@a"), "<r>\n<e a=\"1\"/>\n<e/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.2.1", 3);
    }

    [Test]
    public async Task Key_OneOfSeveralFieldsMissing_ReportsOncePerElement()
    {
        var issues = Run(IdcFixture.Key("k", "e", "@a", "@b"), "<r>\n<e/>\n<e a=\"1\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(2);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.2.1", 2);
        await IdcAssert.Issue(issues[1], "cvc-identity-constraint.4.2.1", 3);
    }

    [Test]
    public async Task Key_SequencesOfSeveralFields_AreDuplicatesOnlyWhenEveryFieldIsEqual()
    {
        var issues = Run(
            IdcFixture.Key("k", "e", "@a", "@b"),
            "<r>\n<e a=\"1\" b=\"x\"/>\n<e a=\"1\" b=\"y\"/>\n<e a=\"2\" b=\"x\"/>\n<e a=\"1\" b=\"x\"/>\n</r>"
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.2.2", 5);
    }

    [Test]
    public async Task Key_FieldOrderMatters_BecauseTheSequenceIsOrdered()
    {
        var issues = Run(IdcFixture.Key("k", "e", "@a", "@b"), "<r><e a=\"1\" b=\"2\"/><e a=\"2\" b=\"1\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Key_NoSelectedElement_ReportsNothing()
    {
        var issues = Run(IdcFixture.Key("k", "e", "@a"), "<r/>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Field_Element_TakesTheValueOfTheElement()
    {
        var issues = Run(
            IdcFixture.Unique("u", "e", "k"),
            "<r>\n<e><k>1</k></e>\n<e><k>2</k></e>\n<e><k>1</k></e>\n</r>"
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 4);
    }

    [Test]
    public async Task Field_AttributeOfAChild_IsFoundThroughTheStepsBeforeIt()
    {
        var issues = Run(IdcFixture.Unique("u", "s", "e/@a"), "<r>\n<s><e a=\"1\"/></s>\n<s><e a=\"1\"/></s>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task Field_Self_IsTheValueOfTheSelectedElementItself()
    {
        var issues = Run(
            IdcFixture.Unique("u", ".//k", "."),
            "<r>\n<e><k>1</k></e>\n<e><k>2</k><m>1</m></e>\n<e><k>1</k></e>\n</r>"
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 4);
    }

    [Test]
    public async Task Field_Descendants_FindsTheValueAtAnyDepth()
    {
        var issues = Run(
            IdcFixture.Unique("u", "s", ".//@a"),
            "<r>\n<s><s><e a=\"1\"/></s></s>\n<s><e a=\"1\"/></s>\n</r>"
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.4.1", 3);
    }

    [Test]
    public async Task Field_TwoElementsMatch_IsAnErrorAtTheSecondOne()
    {
        var issues = Run(IdcFixture.Unique("u", "e", "k"), "<r>\n<e>\n<k>1</k>\n<k>2</k>\n</e>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.3", 4);
    }

    [Test]
    public async Task Field_TwoAttributesMatch_IsAnErrorAtTheElement()
    {
        var issues = Run(IdcFixture.Key("k", "e", "@a|@b"), "<r>\n<e a=\"1\" b=\"2\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.3", 2);
    }

    [Test]
    public async Task Field_ThreeMatches_AreReportedOnce()
    {
        var issues = Run(IdcFixture.Unique("u", "e", "k"), "<r>\n<e><k>1</k><k>2</k><k>3</k></e>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Field_AfterAnError_TheElementTakesNoPartInTheTable()
    {
        var issues = Run(IdcFixture.Unique("u", "e", "k"), "<r>\n<e><k>1</k><k>2</k></e>\n<e><k>1</k></e>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.3", 2);
    }

    [Test]
    public async Task Field_AttributeMatchedBySkippedWildcard_IsAnErrorAtTheElement()
    {
        var set = new ConstraintSet().On("r", IdcFixture.Key("k", "e", "@x"));
        var schema = IdcFixture.Schema(attributeWildcard: SchemaFixture.Wild("##any", XsdProcessContents.Skip));

        var issues = IdcFixture.Run(schema, "<r>\n<e x=\"1\"/>\n</r>", set.Listener());

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.3", 2);
    }

    [Test]
    public async Task Field_UnionOfPathsThatSelectTheSameAttribute_CountsItOnce()
    {
        var issues = Run(IdcFixture.Key("k", "e", "@a|@*"), "<r><e a=\"1\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Field_ElementWithoutSimpleContent_IsAnErrorAtThatElement()
    {
        var issues = Run(IdcFixture.Unique("u", "s", "e"), "<r>\n<s>\n<e/>\n</s>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(1);
        await IdcAssert.Issue(issues[0], "cvc-identity-constraint.3", 3);
    }

    [Test]
    public async Task Selector_SameElementByTwoPaths_IsSelectedOnce()
    {
        var issues = Run(IdcFixture.Unique("u", "e|.//e", "@a"), "<r><e a=\"1\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }
}
