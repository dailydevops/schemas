namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Xliff;

public class XliffRunTests
{
    [Test]
    public async Task ExpandInternalEntities_DeclarationWithNestedEntity_IsRemovedAndExpanded()
    {
        const string Text = """
            <!DOCTYPE schematron [
            <!-- comment -->
            <!ENTITY year "2018">
            <!ENTITY date "13 February &year;">
            ]>
            <schema title="&date; &amp; &unknown;"/>
            """;

        var result = XliffRun.ExpandInternalEntities(Text);

        await Assert.That(result).DoesNotContain("DOCTYPE");
        await Assert.That(result).Contains("title=\"13 February 2018 &amp; &unknown;\"");
    }

    [Test]
    public async Task ExpandInternalEntities_WithoutDeclaration_ReturnsTheTextUnchanged()
    {
        const string Text = "<schema title=\"&amp;\"/>";

        await Assert.That(XliffRun.ExpandInternalEntities(Text)).IsEqualTo(Text);
    }

    [Test]
    public async Task ExpandInternalEntities_Null_Throws() =>
        await Assert.That(() => XliffRun.ExpandInternalEntities(null!)).Throws<ArgumentNullException>();
}
