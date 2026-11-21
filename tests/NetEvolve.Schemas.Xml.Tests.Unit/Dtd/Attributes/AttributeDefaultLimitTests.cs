namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Attributes;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// The limits that a defaulted attribute takes part in, and the well-formedness errors that the expansion of a default value
/// raises. A defaulted attribute counts as an attribute and a node and has a value length limit like a specified one; it is
/// not charged to the characters of the start tag, because it is not part of the document. An error is reported at the start
/// tag of the element that the default was applied to.
/// </summary>
public class AttributeDefaultLimitTests
{
    private static string Doc(string declarations, string element) => $"<!DOCTYPE a [{declarations}]>{element}";

    [Test]
    public async Task DefaultedAttribute_CountsTowardsTheAttributesPerElement()
    {
        var limits = XmlReaderLimits.Default.WithMaxAttributesPerElement(2);
        const string declarations = "<!ATTLIST a d1 CDATA \"1\" d2 CDATA \"2\" d3 CDATA \"3\">";

        var fits = AttributeHarness.Read(Doc(declarations, "<a d1=\"x\"/>"), limits);
        var exceeds = AttributeHarness.Read(Doc(declarations, "<a/>"), limits);

        await Assert.That(fits.Code).IsNull();
        await Assert.That(exceeds.Code).IsEqualTo("XML1103");
    }

    [Test]
    public async Task DefaultedNamespaceDeclaration_CountsTowardsTheDeclarationsPerElement()
    {
        var limits = XmlReaderLimits.Default.WithMaxNamespaceDeclarationsPerElement(1);

        var run = AttributeHarness.Read(Doc("<!ATTLIST a xmlns:p CDATA \"u\" xmlns:q CDATA \"v\">", "<a/>"), limits);

        await Assert.That(run.Code).IsEqualTo("XML1104");
    }

    [Test]
    public async Task DefaultedAttribute_CountsTowardsTheNodeCount()
    {
        const string declarations = "<!ATTLIST a d CDATA \"v\">";

        var fits = AttributeHarness.Read(Doc(declarations, "<a/>"), XmlReaderLimits.Default.WithMaxNodeCount(2));
        var exceeds = AttributeHarness.Read(Doc(declarations, "<a/>"), XmlReaderLimits.Default.WithMaxNodeCount(1));

        await Assert.That(fits.Code).IsNull();
        await Assert.That(exceeds.Code).IsEqualTo("XML1108");
    }

    [Test]
    public async Task DefaultValue_IsBoundedByTheAttributeValueLength()
    {
        var limits = XmlReaderLimits.Default.WithMaxAttributeValueLength(3);

        var fits = AttributeHarness.Read(Doc("<!ATTLIST a d CDATA \"abc\">", "<a/>"), limits);
        var exceeds = AttributeHarness.Read(Doc("<!ATTLIST a d CDATA \"abcd\">", "<a/>"), limits);
        var expanded = AttributeHarness.Read(Doc("<!ENTITY e \"abcd\"><!ATTLIST a d CDATA \"&e;\">", "<a/>"), limits);

        await Assert.That(fits.Code).IsNull();
        await Assert.That(exceeds.Code).IsEqualTo("XML1112");
        await Assert.That(expanded.Code).IsEqualTo("XML1112");
    }

    [Test]
    public async Task DefaultValue_IsNotChargedToTheStartTagCharacters()
    {
        var limits = XmlReaderLimits.Default.WithMaxStartTagCharacters(8);

        var run = AttributeHarness.Read(Doc("<!ATTLIST a d CDATA \"a-long-default-value\">", "<a/>"), limits);

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|@d=a-long-default-value{Cdata!}|/a");
    }

    [Test]
    public async Task DefaultValue_ExpansionIsChargedToTheExpansionBudget()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(3);

        var run = AttributeHarness.Read(Doc("<!ENTITY e \"abcd\"><!ATTLIST a d CDATA \"&e;\">", "<a/>"), limits);

        await Assert.That(run.Code).IsEqualTo("XML1109");
    }

    [Test]
    public async Task DefaultValue_ChargesTheBudgetForEveryElementItIsAppliedTo()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(4);
        const string declarations = "<!ENTITY e \"ab\"><!ATTLIST b d CDATA \"&e;\">";

        var two = AttributeHarness.Read(Doc(declarations, "<a><b/><b/></a>"), limits);
        var three = AttributeHarness.Read(Doc(declarations, "<a><b/><b/><b/></a>"), limits);

        await Assert.That(two.Code).IsNull();
        await Assert.That(three.Code).IsEqualTo("XML1109");
    }

    [Test]
    public async Task DefaultValue_ReferencingAnEntityThatContainsLessThan_RaisesXml0013AtTheStartTag()
    {
        const string xml = "<!DOCTYPE a [<!ENTITY e \"<b/>\"><!ATTLIST a d CDATA \"&e;\">]>\n<a/>";

        var run = AttributeHarness.Read(xml);

        await Assert.That(run.Code).IsEqualTo("XML0013");
        await Assert
            .That(run.ErrorPosition)
            .IsEqualTo(new XmlPosition(2, 1, xml.IndexOf("<a/>", StringComparison.Ordinal)));
    }

    [Test]
    public async Task DefaultValue_ReferencingAnUndeclaredEntity_RaisesXml0008()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a d CDATA \"&u;\">", "<a/>"));

        await Assert.That(run.Code).IsEqualTo("XML0008");
    }

    [Test]
    public async Task DefaultValue_ReferencingAnExternalEntity_RaisesXml0022()
    {
        var run = AttributeHarness.Read(Doc("<!ENTITY e SYSTEM \"x.ent\"><!ATTLIST a d CDATA \"&e;\">", "<a/>"));

        await Assert.That(run.Code).IsEqualTo("XML0022");
    }

    [Test]
    public async Task DefaultValue_ReferencingAnUnparsedEntity_RaisesXml0021()
    {
        var run = AttributeHarness.Read(
            Doc("<!NOTATION n SYSTEM \"u\"><!ENTITY e SYSTEM \"x\" NDATA n><!ATTLIST a d CDATA \"&e;\">", "<a/>")
        );

        await Assert.That(run.Code).IsEqualTo("XML0021");
    }

    [Test]
    public async Task DefaultValue_ReferencingARecursiveEntity_RaisesXml0020()
    {
        var run = AttributeHarness.Read(
            Doc("<!ENTITY e \"&f;\"><!ENTITY f \"&e;\"><!ATTLIST a d CDATA \"&e;\">", "<a/>")
        );

        await Assert.That(run.Code).IsEqualTo("XML0020");
    }
}
