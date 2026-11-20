namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;

using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

public class IdTests
{
    private static async Task AssertIssue(IReadOnlyList<ValidationIssue> issues, string clause, int line)
    {
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("cvc-id"));
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Constraint).IsEqualTo(clause);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(issues[0].Location.Column).IsGreaterThan(0);
        await Assert.That(issues[0].Location.Source).IsEqualTo(ValidationHarness.Source);
    }

    [Test]
    public async Task Id_TwoAttributesWithTheSameValue_ReportOneDuplicateAtTheSecondOccurrence()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r>\n<e id=\"a\"/>\n<e id=\"b\"/>\n<e id=\"a\"/>\n</r>");

        await AssertIssue(issues, "cvc-id.2", 4);
    }

    [Test]
    public async Task Id_TwoElementsWithTheSameValue_ReportOneDuplicateAtTheSecondElement()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r>\n<i>a</i>\n<i>b</i>\n<i>a</i>\n</r>");

        await AssertIssue(issues, "cvc-id.2", 4);
    }

    [Test]
    public async Task Id_AnAttributeAndAnElementWithTheSameValue_AreDuplicates()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r>\n<e id=\"a\"/>\n<i>a</i>\n</r>");

        await AssertIssue(issues, "cvc-id.2", 3);
    }

    [Test]
    public async Task Id_ARepeatedValue_IsReportedAtEveryFurtherOccurrence()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r>\n<e id=\"a\"/>\n<e id=\"a\"/>\n<e id=\"a\"/>\n</r>");

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
        await Assert.That(issues[1].Location.Line).IsEqualTo(4);
    }

    [Test]
    public async Task Id_ValuesThatDifferInCase_AreDistinct()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r><e id=\"a\"/><e id=\"A\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Id_SurroundingWhiteSpace_IsCollapsedBeforeTheComparison()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r>\n<e id=\"a\"/>\n<e id=\"  a\t\"/>\n</r>");

        await AssertIssue(issues, "cvc-id.2", 3);
    }

    [Test]
    public async Task Id_OnADerivedType_IsChecked()
    {
        var schema = new SchemaFixture();
        var derived = IdFixture.Restriction(IdFixture.Restriction(SchemaFixture.Simple("ID"), schema), schema);

        var issues = IdFixture.Run(IdFixture.Schema(id: derived), "<r>\n<e id=\"a\"/>\n<i>a</i>\n</r>");

        await AssertIssue(issues, "cvc-id.2", 3);
    }

    [Test]
    public async Task Id_OnAUnionMemberThatAcceptsTheValue_IsChecked_AndNotWhenAnEarlierMemberAccepts()
    {
        var schema = new SchemaFixture();
        var union = IdFixture.UnionOf(schema, SchemaFixture.Simple("letters"), SchemaFixture.Simple("ID"));

        // "ab" is accepted by the first member (letters), "a1" only by the ID member.
        var letters = IdFixture.Run(IdFixture.Schema(id: union), "<r><e id=\"ab\"/><e id=\"ab\"/></r>");
        var ids = IdFixture.Run(IdFixture.Schema(id: union), "<r>\n<e id=\"a1\"/>\n<e id=\"a1\"/>\n</r>");

        await Assert.That(letters.Count).IsEqualTo(0);
        await AssertIssue(ids, "cvc-id.2", 3);
    }

    [Test]
    public async Task Id_ThatTheDatatypeRejects_IsNotRegistered_SoARepeatIsNotAlsoADuplicate()
    {
        var listener = IdFixture.Listener(new RejectingAssessor());

        var issues = ValidationHarness.Run(
            IdFixture.Schema(),
            "<r><e id=\"1\"/><e id=\"1\"/></r>",
            new RejectingAssessor(),
            listener
        );

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Code).IsNotEqualTo(ValidationHarness.Code("cvc-id"));
        await Assert.That(issues[1].Code).IsNotEqualTo(ValidationHarness.Code("cvc-id"));
    }

    [Test]
    public async Task Id_WithADefaultedValue_IsNotRegistered()
    {
        var schema = IdFixture.Schema(
            idConstraint: new AttributeValueConstraint(AttributeValueConstraintKind.Default, "d")
        );

        var issues = IdFixture.Run(schema, "<r><e/><e/><e id=\"d\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Id_ManyDistinctValues_AreAllAccepted_AndEveryRepeatIsFound()
    {
        const int Count = 20_000;
        var builder = new StringBuilder("<r>");
        for (var i = 0; i < Count; i++)
        {
            _ = builder.Append("<e id=\"v").Append(i).Append("\"/>");
        }

        var clean = IdFixture.Run(IdFixture.Schema(), builder.ToString() + "</r>");
        var repeated = IdFixture.Run(IdFixture.Schema(), builder.ToString() + "<e id=\"v7\"/></r>");

        await Assert.That(clean.Count).IsEqualTo(0);
        await Assert.That(repeated.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Listener_ReusedForASecondRun_StartsWithEmptyTables()
    {
        var listener = IdFixture.Listener(new ScriptedAssessor());
        var schema = IdFixture.Schema();

        var first = ValidationHarness.Run(schema, "<r><e id=\"a\"/><e ref=\"x\"/></r>", null, listener);
        var second = ValidationHarness.Run(schema, "<r><e id=\"a\"/><e ref=\"a\"/></r>", null, listener);

        await Assert.That(first.Count).IsEqualTo(1);
        await Assert.That(second.Count).IsEqualTo(0);
    }
}
