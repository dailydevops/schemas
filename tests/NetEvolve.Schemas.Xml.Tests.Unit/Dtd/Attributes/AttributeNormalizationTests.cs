namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Attributes;

using System;
using System.Threading;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>XML 1.0 section 3.3.3: normalization of attribute values, CDATA rules and the additional rules of the tokenized types.</summary>
public class AttributeNormalizationTests
{
    private static string Doc(string declarations, string element) => $"<!DOCTYPE a [{declarations}]>{element}";

    [Test]
    [Arguments("<a x=\"  a   b  \"/>", "<a|@x=  a   b  {-}|/a")]
    [Arguments("<a x=\"a\tb\nc\r\nd\re\"/>", "<a|@x=a b c d e{-}|/a")]
    [Arguments("<a x=\"a&#10;b&#9;c&#13;d&#32;e\"/>", "<a|@x=a\nb\tc\rd e{-}|/a")]
    public async Task Undeclared_FollowsTheCdataRules(string element, string expected)
    {
        var run = AttributeHarness.Read(Doc("", element));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<a x=\"  a   b  \"/>", "<a|@x=  a   b  {Cdata}|/a")]
    [Arguments("<a x=\"a\tb\nc\r\nd\re\"/>", "<a|@x=a b c d e{Cdata}|/a")]
    [Arguments("<a x=\"a&#10;b&#9;c&#13;d&#32;e\"/>", "<a|@x=a\nb\tc\rd e{Cdata}|/a")]
    public async Task Cdata_FollowsTheCdataRules(string element, string expected)
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a x CDATA #IMPLIED>", element));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    [Test]
    public async Task Cdata_EntityReferenceKeepsTheWhiteSpaceOfTheReplacementText()
    {
        var run = AttributeHarness.Read(Doc("<!ENTITY e \" p  q \"><!ATTLIST a x CDATA #IMPLIED>", "<a x=\"[&e;]\"/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|@x=[ p  q ]{Cdata}|/a");
    }

    // Each tokenized type trims and collapses the space characters, and reports the change.
    [Test]
    [Arguments("ID", "Id")]
    [Arguments("IDREF", "IdRef")]
    [Arguments("IDREFS", "IdRefs")]
    [Arguments("ENTITY", "Entity")]
    [Arguments("ENTITIES", "Entities")]
    [Arguments("NMTOKEN", "NmToken")]
    [Arguments("NMTOKENS", "NmTokens")]
    [Arguments("NOTATION (n)", "Notation")]
    [Arguments("(a|b)", "Enumeration")]
    public async Task TokenizedType_TrimsAndCollapsesSpaces(string type, string name)
    {
        var run = AttributeHarness.Read(Doc($"<!ATTLIST a x {type} #IMPLIED>", "<a x=\"  a   b  \"/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo($"<a|@x=a b{{{name}~}}|/a");
    }

    [Test]
    [Arguments("a b", "<a|@x=a b{NmTokens}|/a")]
    [Arguments("a", "<a|@x=a{NmTokens}|/a")]
    [Arguments(" a", "<a|@x=a{NmTokens~}|/a")]
    [Arguments("a ", "<a|@x=a{NmTokens~}|/a")]
    [Arguments("a  b", "<a|@x=a b{NmTokens~}|/a")]
    [Arguments("   ", "<a|@x={NmTokens~}|/a")]
    [Arguments("", "<a|@x={NmTokens}|/a")]
    [Arguments("\ta\n\nb\r\n", "<a|@x=a b{NmTokens~}|/a")]
    public async Task TokenizedType_ReportsAChangeOnlyWhenTheValueChanged(string value, string expected)
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a x NMTOKENS #IMPLIED>", $"<a x=\"{value}\"/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    // Section 3.3.3 discards and collapses only the space character #x20, whichever way it was written.
    [Test]
    [Arguments("&#32;a&#32;&#32;b&#32;", "<a|@x=a b{NmTokens~}|/a")]
    [Arguments("a&#9;b", "<a|@x=a\tb{NmTokens}|/a")]
    [Arguments("&#9;a", "<a|@x=\ta{NmTokens}|/a")]
    [Arguments("a&#10;&#10;b", "<a|@x=a\n\nb{NmTokens}|/a")]
    [Arguments("a &#9; b", "<a|@x=a \t b{NmTokens}|/a")]
    public async Task TokenizedType_CharacterReferencesToWhiteSpace(string value, string expected)
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a x NMTOKENS #IMPLIED>", $"<a x=\"{value}\"/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo(expected);
    }

    [Test]
    [Arguments("&e;", "p q")]
    [Arguments("a&e;b", "a p q b")]
    [Arguments("&e;&e;", "p q p q")]
    public async Task TokenizedType_EntityReferenceIsNormalizedAfterTheExpansion(string value, string expected)
    {
        var run = AttributeHarness.Read(
            Doc("<!ENTITY e \" p  q \"><!ATTLIST a x NMTOKENS #IMPLIED>", $"<a x=\"{value}\"/>")
        );

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo($"<a|@x={expected}{{NmTokens~}}|/a");
    }

    [Test]
    public async Task TokenizedType_NormalizesEveryAttributeOfTheElementIndependently()
    {
        var run = AttributeHarness.Read(
            Doc(
                "<!ATTLIST a c CDATA #IMPLIED t NMTOKENS #IMPLIED u CDATA #IMPLIED>",
                "<a c=\" 1  2 \" t=\" 1  2 \" u=\" 1  2 \" v=\" 1  2 \"/>"
            )
        );

        await Assert
            .That(run.Events)
            .IsEqualTo("<a|@c= 1  2 {Cdata}|@t=1 2{NmTokens~}|@u= 1  2 {Cdata}|@v= 1  2 {-}|/a");
    }

    [Test]
    public async Task TokenizedType_PrefixedAttributeIsMatchedByItsNameAsWritten()
    {
        var run = AttributeHarness.Read(
            Doc("<!ATTLIST p:a p:x NMTOKENS #IMPLIED>", "<p:a xmlns:p=\"u\" p:x=\" 1  2 \"/>")
        );

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<{u}p:a|#p=u{-}|@{u}p:x=1 2{NmTokens~}|/{u}p:a");
    }

    [Test]
    public async Task TokenizedType_ValueIsNormalizedWhateverTheChunking(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string xml = "<!DOCTYPE a [<!ATTLIST a x NMTOKENS #IMPLIED>]><a x=\"  a &#32; b  \"/>";

        var sequence = AttributeHarness.ReadSequence(xml);
        var stream = await AttributeHarness.ReadStreamAsync(xml, cancellationToken);
        var whole = AttributeHarness.Read(xml);

        await Assert.That(whole.Events).IsEqualTo("<a|@x=a b{NmTokens~}|/a");
        await Assert.That(sequence).IsEqualTo(whole.Events);
        await Assert.That(stream).IsEqualTo(whole.Events);
    }

    [Test]
    public async Task TokenizedType_AttributeValueLimitIsCheckedBeforeTheCollapse()
    {
        // Limitation: the length limit counts the value as written, so the collapse can only make a value shorter.
        var limits = XmlReaderLimits.Default.WithMaxAttributeValueLength(4);

        var within = AttributeHarness.Read(Doc("<!ATTLIST a x NMTOKENS #IMPLIED>", "<a x=\"a  b\"/>"), limits);
        var beyond = AttributeHarness.Read(Doc("<!ATTLIST a x NMTOKENS #IMPLIED>", "<a x=\"a   b\"/>"), limits);

        await Assert.That(within.Events).IsEqualTo("<a|@x=a b{NmTokens~}|/a");
        await Assert.That(beyond.Code).IsEqualTo("XML1112");
    }
}
