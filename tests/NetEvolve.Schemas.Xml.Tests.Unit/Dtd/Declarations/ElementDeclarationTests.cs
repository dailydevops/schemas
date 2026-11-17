namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Declarations;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>Productions 45 to 51: elementdecl, contentspec, children, Mixed.</summary>
public class ElementDeclarationTests
{
    [Test]
    [Arguments("<!ELEMENT a EMPTY>", "E:a:EMPTY")]
    [Arguments("<!ELEMENT a ANY>", "E:a:ANY")]
    [Arguments("<!ELEMENT a (#PCDATA)>", "E:a:(#PCDATA)")]
    [Arguments("<!ELEMENT a (#PCDATA)*>", "E:a:(#PCDATA)*")]
    [Arguments("<!ELEMENT a ( #PCDATA | b | c )*>", "E:a:(#PCDATA|b|c)*")]
    [Arguments("<!ELEMENT a (b)>", "E:a:(b)")]
    [Arguments("<!ELEMENT a (b,c?,d*,e+)>", "E:a:(b,c?,d*,e+)")]
    [Arguments("<!ELEMENT a (b|c)+>", "E:a:(b|c)+")]
    [Arguments("<!ELEMENT a ((b,c)|d)*>", "E:a:((b,c)|d)*")]
    [Arguments("<!ELEMENT  a\r\n( b , c )\t>", "E:a:(b,c)")]
    [Arguments("<!ELEMENT a EMPTY><!ELEMENT b ANY>", "E:a:EMPTY|E:b:ANY")]
    public async Task ValidDeclaration_ProducesTheModel(string subset, string expected)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (model, error) = DtdHarness.Parse(subset);

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<!ELEMENT a>")]
    [Arguments("<!ELEMENTa EMPTY>")]
    [Arguments("<!ELEMENT 1a EMPTY>")]
    [Arguments("<!ELEMENT a EMPTY")]
    [Arguments("<!ELEMENT a empty>")]
    [Arguments("<!ELEMENT a ()>")]
    [Arguments("<!ELEMENT a (b,)>")]
    [Arguments("<!ELEMENT a (b,c>")]
    [Arguments("<!ELEMENT a (b|c,d)>")]
    [Arguments("<!ELEMENT a (b)??>")]
    [Arguments("<!ELEMENT a (#PCDATA|b)>")]
    [Arguments("<!ELEMENT a (#PCDATA)+>")]
    [Arguments("<!ELEMENT a (#PCDATA|(b))*>")]
    [Arguments("<!ELEMENT a EMPTY extra>")]
    public async Task InvalidDeclaration_RaisesXml0011(string subset)
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (_, error) = DtdHarness.Parse(subset);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task SecondDeclarationOfTheSameElement_IsIgnored()
    {
        var (model, error) = DtdHarness.Parse("<!ELEMENT a EMPTY><!ELEMENT a ANY>");

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY");
    }

    [Test]
    public async Task DeeplyNestedContentModel_RaisesTheDepthLimit()
    {
        var limits = XmlReaderLimits.Default.WithMaxElementDepth(8);
        var subset = "<!ELEMENT a " + new string('(', 9) + "b" + new string(')', 9) + ">";

        var (_, error) = DtdHarness.Parse(subset, limits: limits);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1102);
    }
}
