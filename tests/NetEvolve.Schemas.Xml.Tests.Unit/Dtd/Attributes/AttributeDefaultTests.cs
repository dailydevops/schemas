namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Attributes;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>XML 1.0 section 3.3.2: defaulted and fixed attributes are added to the elements that omit them.</summary>
public class AttributeDefaultTests
{
    private static string Doc(string declarations, string element) => $"<!DOCTYPE a [{declarations}]>{element}";

    [Test]
    [Arguments("#IMPLIED")]
    [Arguments("#REQUIRED")]
    public async Task ImpliedAndRequired_AddNothing(string kind)
    {
        var run = AttributeHarness.Read(Doc($"<!ATTLIST a d CDATA {kind}>", "<a/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|/a");
    }

    [Test]
    public async Task Default_IsAddedWithTheDeclaredValueAndTheNotSpecifiedFlag()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a d CDATA \"v\">", "<a/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|@d=v{Cdata!}|/a");
    }

    [Test]
    public async Task Fixed_IsAddedWithTheDeclaredValueAndTheNotSpecifiedFlag()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a d CDATA #FIXED \"f\">", "<a/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|@d=f{Cdata!}|/a");
    }

    [Test]
    [Arguments("CDATA \"v\"", "mine")]
    [Arguments("CDATA #FIXED \"f\"", "mine")]
    [Arguments("CDATA #FIXED \"f\"", "f")]
    public async Task SpecifiedAttribute_IsNeverOverridden(string definition, string value)
    {
        var run = AttributeHarness.Read(Doc($"<!ATTLIST a d {definition}>", $"<a d=\"{value}\"/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo($"<a|@d={value}{{Cdata}}|/a");
    }

    [Test]
    public async Task Defaults_FollowTheSpecifiedAttributesInDeclarationOrder()
    {
        var run = AttributeHarness.Read(
            Doc(
                "<!ATTLIST a d1 CDATA \"1\" s CDATA #IMPLIED d2 CDATA #FIXED \"2\" d3 CDATA \"3\">",
                "<a s=\"x\" d3=\"mine\"/>"
            )
        );

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|@s=x{Cdata}|@d3=mine{Cdata}|@d1=1{Cdata!}|@d2=2{Cdata!}|/a");
    }

    [Test]
    [Arguments("<a/>")]
    [Arguments("<a></a>")]
    [Arguments("<a>t</a>")]
    public async Task Default_IsAddedToEmptyAndNonEmptyElements(string element)
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a d CDATA \"v\">", element));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).StartsWith("<a|@d=v{Cdata!}|");
    }

    [Test]
    public async Task Default_IsAddedToEveryElementOfTheType()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST b d CDATA \"v\">", "<a><b/><b d=\"x\"/><c><b/></c></a>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|<b|@d=v{Cdata!}|/b|<b|@d=x{Cdata}|/b|<c|<b|@d=v{Cdata!}|/b|/c|/a");
    }

    [Test]
    public async Task Default_IsAddedToAnElementThatComesFromAnEntity()
    {
        var run = AttributeHarness.Read(Doc("<!ENTITY e \"<b/>\"><!ATTLIST b d CDATA \"v\">", "<a>&e;</a>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|<b|@d=v{Cdata!}|/b|/a");
    }

    [Test]
    public async Task Declarations_OfOtherElementsAndUndeclaredElements_AddNothing()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST other d CDATA \"v\">", "<a x=\"1\"><b/></a>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|@x=1{-}|<b|/b|/a");
    }

    [Test]
    public async Task SeveralAttlistDeclarations_OfOneElementAreMerged()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a d1 CDATA \"1\"><!ATTLIST a d2 CDATA \"2\">", "<a/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|@d1=1{Cdata!}|@d2=2{Cdata!}|/a");
    }

    // Section 3.3: when more than one definition is provided for the same attribute, the first declaration binds.
    [Test]
    [Arguments("<!ATTLIST a d CDATA \"1\"><!ATTLIST a d CDATA \"2\">", "<a|@d=1{Cdata!}|/a")]
    [Arguments("<!ATTLIST a d CDATA \"1\" d NMTOKEN \"2\">", "<a|@d=1{Cdata!}|/a")]
    [Arguments("<!ATTLIST a d NMTOKEN #IMPLIED><!ATTLIST a d CDATA \"2\">", "<a|/a")]
    public async Task FirstDeclarationOfAnAttribute_Binds(string declarations, string expected)
    {
        var run = AttributeHarness.Read(Doc(declarations, "<a/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    [Test]
    [Arguments("x&e;&#65;&amp;", "xEA&")]
    [Arguments("&e;&e;", "EE")]
    [Arguments("a\tb\nc", "a b c")]
    [Arguments("a\r\nb\rc", "a b c")]
    [Arguments("a&#10;b&#9;c", "a\nb\tc")]
    [Arguments("", "")]
    public async Task Default_ExpandsReferencesAndNormalizesWhiteSpaceAsAnAttributeValue(
        string literal,
        string expected
    )
    {
        var run = AttributeHarness.Read(Doc($"<!ENTITY e \"E\"><!ATTLIST a d CDATA \"{literal}\">", "<a/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo($"<a|@d={expected}{{Cdata!}}|/a");
    }

    [Test]
    public async Task Default_OfATokenizedType_IsNormalizedByTheType()
    {
        var run = AttributeHarness.Read(
            Doc("<!ENTITY e \" p  q \"><!ATTLIST a d NMTOKENS \" x &#32;&#32; y&e;\">", "<a/>")
        );

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|@d=x y p q{NmTokens!~}|/a");
    }

    [Test]
    public async Task Default_OfTheIdType_IsReportedWithTheIdType()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a i ID \"x\">", "<a/>"));

        await Assert.That(run.Events).IsEqualTo("<a|@i=x{Id!}|/a");
    }

    [Test]
    public async Task Default_OfAPrefixedAttribute_IsMatchedByItsNameAsWritten()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST p:a p:d CDATA \"v\">", "<p:a xmlns:p=\"u\"/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<{u}p:a|#p=u{-}|@{u}p:d=v{Cdata!}|/{u}p:a");
    }

    [Test]
    public async Task Default_IsNotExpandedWhenTheDocumentSpecifiesTheAttribute()
    {
        // The default is expanded where it is applied, so a default that cannot be expanded is no error here.
        var run = AttributeHarness.Read(Doc("<!ATTLIST a d CDATA \"&undeclared;\">", "<a d=\"x\"/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|@d=x{Cdata}|/a");
    }

    [Test]
    public async Task Default_OfAnElementThatNeverAppears_IsNotExpanded()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST other d CDATA \"&undeclared;\">", "<a/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|/a");
    }

    [Test]
    public async Task DtdProcessingOff_ReportsNoInfoAndLeavesTheEventsAsTheyWere()
    {
        const string plain = "<a x=\"1\"><b/></a>";

        var without = AttributeHarness.Read(plain, XmlReaderOptions.Default);
        var with = AttributeHarness.Read(plain);

        await Assert.That(without.Events).IsEqualTo("<a|@x=1|<b|/b|/a");
        await Assert.That(without.Sink.Infos).IsEmpty();
        await Assert.That(with.Events).IsEqualTo("<a|@x=1{-}|<b|/b|/a");
    }

    [Test]
    public async Task DtdProcessingOff_RejectsTheDocumentTypeDeclarationAndAppliesNothing()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a d CDATA \"v\">", "<a/>"), XmlReaderOptions.Default);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML2001");
        await Assert.That(run.Events).IsEmpty();
    }

    [Test]
    public async Task DocumentWithoutDocumentTypeDeclaration_ValuesAreNormalizedAsBefore()
    {
        const string xml = "<a x=\" 1  2 \" y='&amp;'><b/></a>";

        var off = AttributeHarness.Read(xml, XmlReaderOptions.Default);
        var on = AttributeHarness.Read(xml);

        await Assert.That(off.Events).IsEqualTo("<a|@x= 1  2 |@y=&|<b|/b|/a");
        await Assert.That(on.Events).IsEqualTo("<a|@x= 1  2 {-}|@y=&{-}|<b|/b|/a");
    }
}
