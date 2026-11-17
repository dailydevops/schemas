namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Declarations;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Productions 82 and 83: NotationDecl and PublicID.</summary>
public class NotationDeclarationTests
{
    [Test]
    [Arguments("<!NOTATION n SYSTEM \"s\">", "N:n - s")]
    [Arguments("<!NOTATION n PUBLIC \"p\" \"s\">", "N:n p s")]
    [Arguments("<!NOTATION n PUBLIC \"p\">", "N:n p -")]
    [Arguments("<!NOTATION  n\r\nSYSTEM\t's' >", "N:n - s")]
    [Arguments("<!NOTATION a SYSTEM \"1\"><!NOTATION b SYSTEM \"2\">", "N:a - 1|N:b - 2")]
    public async Task ValidDeclaration_ProducesTheModel(string subset, string expected)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (model, error) = DtdHarness.Parse(subset);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<!NOTATION n>")]
    [Arguments("<!NOTATION n PUBLIC>")]
    [Arguments("<!NOTATION SYSTEM \"s\">")]
    [Arguments("<!NOTATION n \"s\">")]
    [Arguments("<!NOTATION n SYSTEM>")]
    [Arguments("<!NOTATION n PUBLIC \"p<\">")]
    [Arguments("<!NOTATION n SYSTEM \"s\" x>")]
    [Arguments("<!NOTATIONn SYSTEM \"s\">")]
    public async Task InvalidDeclaration_RaisesXml0011(string subset)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (_, error) = DtdHarness.Parse(subset);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task SecondDeclarationOfTheSameNotation_IsIgnored()
    {
        var (model, error) = DtdHarness.Parse("<!NOTATION n SYSTEM \"one\"><!NOTATION n SYSTEM \"two\">");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("N:n - one");
    }
}
