namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Attributes;

using System;

/// <summary>Namespaces in XML 1.0: a namespace declaration that comes from the default of an attribute definition takes part in namespace processing.</summary>
public class AttributeNamespaceTests
{
    private static string Doc(string declarations, string element) => $"<!DOCTYPE a [{declarations}]>{element}";

    [Test]
    public async Task DefaultedPrefixDeclaration_BindsThePrefixForTheElementAndItsDescendants()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a xmlns:p CDATA \"urn:p\">", "<a><p:b><p:c/></p:b></a>"));

        await Assert.That(run.Code).IsNull();
        await Assert
            .That(run.Events)
            .IsEqualTo("<a|#p=urn:p{Cdata!}|<{urn:p}p:b|<{urn:p}p:c|/{urn:p}p:c|/{urn:p}p:b|/a");
    }

    [Test]
    public async Task DefaultedPrefixDeclaration_BindsNothingOutsideTheElementThatCarriesIt()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST b xmlns:p CDATA \"urn:p\">", "<a><b><p:c/></b><p:d/></a>"));

        await Assert.That(run.Code).IsEqualTo("XML3001");
        await Assert.That(run.Events).IsEqualTo("<a|<b|#p=urn:p{Cdata!}|<{urn:p}p:c|/{urn:p}p:c|/b");
    }

    [Test]
    [Arguments("xmlns CDATA \"urn:d\"")]
    [Arguments("xmlns CDATA #FIXED \"urn:d\"")]
    public async Task DefaultedDefaultNamespace_PutsTheElementAndItsDescendantsInTheNamespace(string definition)
    {
        var run = AttributeHarness.Read(Doc($"<!ATTLIST a {definition}>", "<a><b/></a>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<{urn:d}a|#=urn:d{Cdata!}|<{urn:d}b|/{urn:d}b|/{urn:d}a");
    }

    [Test]
    public async Task SpecifiedNamespaceDeclaration_IsNotOverriddenByTheDefault()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a xmlns CDATA \"urn:d\">", "<a xmlns=\"urn:s\"/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<{urn:s}a|#=urn:s{Cdata}|/{urn:s}a");
    }

    [Test]
    public async Task DefaultedAttribute_UsesAPrefixThatADefaultedDeclarationBindsOnTheSameElement()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a xmlns:p CDATA \"u\" p:x CDATA \"1\">", "<a/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|#p=u{Cdata!}|@{u}p:x=1{Cdata!}|/a");
    }

    [Test]
    public async Task SpecifiedAttribute_UsesAPrefixThatADefaultedDeclarationBinds()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a xmlns:p CDATA \"u\">", "<a p:x=\"1\"/>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|#p=u{Cdata!}|@{u}p:x=1{-}|/a");
    }

    [Test]
    public async Task DefaultedPrefixDeclaration_OfAChildRebindsThePrefix()
    {
        var run = AttributeHarness.Read(
            Doc("<!ATTLIST b xmlns:p CDATA \"u2\">", "<a xmlns:p=\"u1\"><b><p:c/></b><p:d/></a>")
        );

        await Assert.That(run.Code).IsNull();
        await Assert
            .That(run.Events)
            .IsEqualTo("<a|#p=u1{-}|<b|#p=u2{Cdata!}|<{u2}p:c|/{u2}p:c|/b|<{u1}p:d|/{u1}p:d|/a");
    }

    [Test]
    public async Task DefaultedDeclaration_OfTheReservedXmlPrefix_RaisesXml3002()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a xmlns:xml CDATA \"urn:wrong\">", "<a/>"));

        await Assert.That(run.Code).IsEqualTo("XML3002");
    }

    [Test]
    public async Task DefaultedDeclaration_OfAPrefixWithAnEmptyUri_RaisesXml3005()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST a xmlns:p CDATA \"\">", "<a/>"));

        await Assert.That(run.Code).IsEqualTo("XML3005");
    }

    [Test]
    public async Task DefaultedDeclaration_OfTheDefaultNamespaceWithAnEmptyUri_UndeclaresIt()
    {
        var run = AttributeHarness.Read(Doc("<!ATTLIST b xmlns CDATA \"\">", "<a xmlns=\"urn:d\"><b/></a>"));

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<{urn:d}a|#=urn:d{-}|<b|#={Cdata!}|/b|/{urn:d}a");
    }

    [Test]
    public async Task DefaultedAttribute_ThatDuplicatesASpecifiedOneByExpandedName_RaisesXml3003()
    {
        var run = AttributeHarness.Read(
            Doc("<!ATTLIST a xmlns:q CDATA \"u\" q:x CDATA \"1\">", "<a xmlns:p=\"u\" p:x=\"2\"/>")
        );

        await Assert.That(run.Code).IsEqualTo("XML3003");
    }
}
