namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Declarations;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entities;

/// <summary>Namespaces in XML 1.0, production [17] and the NCName rule: entity and notation names hold no colon.</summary>
public class NamespaceNameTests
{
    [Test]
    [Arguments("<!DOCTYPE foo [<!ENTITY a:b \"bogus\">]><foo/>")]
    [Arguments("<!DOCTYPE foo [<!NOTATION a:b SYSTEM \"notation\">]><foo/>")]
    [Arguments("<!DOCTYPE foo [<!ENTITY % a:b \"bogus\">]><foo/>")]
    [Arguments("<!DOCTYPE foo [<!ENTITY a SYSTEM \"s\" NDATA a:b>]><foo/>")]
    [Arguments("<!DOCTYPE foo [<!ENTITY :b \"bogus\">]><foo/>")]
    [Arguments("<!DOCTYPE foo [<!NOTATION a: SYSTEM \"notation\">]><foo/>")]
    public async Task ColonInADeclaredName_RaisesXml3004(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = EntityHarness.Read(xml);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML3004");
    }

    [Test]
    [Arguments("<!DOCTYPE foo [<!ENTITY a \"x\">]><foo>&a;</foo>")]
    [Arguments("<!DOCTYPE foo [<!ENTITY % a \"x\">]><foo/>")]
    [Arguments("<!DOCTYPE foo [<!NOTATION n SYSTEM \"s\"><!ENTITY a SYSTEM \"s\" NDATA n>]><foo/>")]
    public async Task NameWithoutAColon_IsAccepted(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var run = EntityHarness.Read(xml);

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Completed);
    }
}
