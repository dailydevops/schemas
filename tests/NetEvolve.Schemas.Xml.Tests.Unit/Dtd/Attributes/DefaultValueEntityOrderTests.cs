namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Attributes;

using System;

/// <summary>
/// The well-formedness constraint Entity Declared (XML 1.0 section 4.1): a general entity that a default value refers to
/// must be declared before the attribute-list declaration, where no other declaration can exist.
/// </summary>
public class DefaultValueEntityOrderTests
{
    [Test]
    [Arguments("<!DOCTYPE doc [<!ELEMENT doc (#PCDATA)><!ATTLIST doc a CDATA \"&e;\"><!ENTITY e \"v\">]><doc/>")]
    [Arguments("<!DOCTYPE doc [<!ATTLIST doc a CDATA \"x&e;y\"><!ENTITY e \"v\">]><doc/>")]
    [Arguments("<!DOCTYPE doc [<!ATTLIST doc a CDATA \"&amp;&#65;&e;\"><!ENTITY e \"v\">]><doc/>")]
    public async Task EntityDeclaredAfterTheAttributeList_RaisesXml0008(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        var run = AttributeHarness.Read(xml);

        await Assert.That(run.Code).IsEqualTo("XML0008");
    }

    [Test]
    [Arguments("<!DOCTYPE doc [<!ENTITY e \"v\"><!ATTLIST doc a CDATA \"&e;\">]><doc/>")]
    [Arguments("<!DOCTYPE doc [<!ATTLIST doc a CDATA \"&amp;&#65;&lt;\">]><doc/>")]
    // A parameter-entity reference before the declaration can supply the entity: the constraint is only a validity one.
    [Arguments("<!DOCTYPE doc [<!ENTITY % p \"<!ENTITY e 'v'>\">%p;<!ATTLIST doc a CDATA \"&e;\">]><doc/>")]
    public async Task EntityDeclaredBefore_OrNotReferenced_IsAccepted(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);

        var run = AttributeHarness.Read(xml);

        await Assert.That(run.Code).IsNull();
    }
}
