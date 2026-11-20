namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Ids;

using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

public class NotationTests
{
    [Test]
    public async Task Notation_Attribute_ThatNamesNoDeclaredNotation_IsReportedAtTheElement()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r>\n<e note=\"gif\"/>\n<e note=\"png\"/>\n</r>", "gif");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(ValidationHarness.Code("cvc-datatype-valid"));
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
        await Assert.That(issues[0].Location.Column).IsGreaterThan(0);
        await Assert.That(issues[0].Message).Contains("'png'");
    }

    [Test]
    public async Task Notation_Element_ThatNamesNoDeclaredNotation_IsReportedAtTheElement()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r>\n<n>gif</n>\n<n> png </n>\n</r>", "gif");

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
        await Assert.That(issues[0].Message).Contains("'png'");
    }

    [Test]
    public async Task Notation_ThatNamesADeclaredNotation_IsValid()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r><e note=\"gif\"/><n>jpeg</n></r>", "gif", "jpeg");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Notation_WithoutAnyDeclaredNotation_IsReported()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r><e note=\"gif\"/></r>");

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Notation_WithAPrefix_IsNotAssessed_BecauseThePrefixNeedsTheNamespaceContext()
    {
        var issues = IdFixture.Run(IdFixture.Schema(), "<r><e note=\"p:gif\"/><n>p:png</n></r>", "gif");

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Notation_OnADerivedType_IsChecked()
    {
        var schema = new SchemaFixture();
        var derived = IdFixture.Restriction(SchemaFixture.Simple("NOTATION"), schema);

        var issues = IdFixture.Run(IdFixture.Schema(notation: derived), "<r><e note=\"png\"/></r>", "gif");

        await Assert.That(issues.Count).IsEqualTo(1);
    }
}
