namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Xslt.Library.Core;
using NetEvolve.Schemas.XPath.Api;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core.CoreFixture;

/// <summary>
/// <c>type-available()</c> (XSLT 3.0, 20.1.2): true for a built-in type of the XML Schema namespace, false for any other name,
/// and false for <c>xs:dateTimeStamp</c> unless the static context is XPath 3.1; XTDE1428 for a name that is not a lexical QName or
/// whose prefix is not bound. The Schematron static context is XPath 2.0, so dateTimeStamp is not available there.
/// </summary>
public class TypeAvailableTests
{
    [Test]
    [Arguments("xs:string", true)]
    [Arguments("xs:integer", true)]
    [Arguments("xs:dateTime", true)]
    [Arguments("xs:anyAtomicType", true)]
    [Arguments("xs:untypedAtomic", true)]
    [Arguments("xs:dateTimeStamp", false)]
    [Arguments("xs:no-such-type", false)]
    [Arguments("f:string", false)]
    [Arguments("string", false)]
    public async Task TypeAvailable_AnswersFromTheBuiltInTypeTable(string name, bool expected)
    {
        ArgumentNullException.ThrowIfNull(name);

        await Assert
            .That(Run(Scope("xslt3"), "type-available", S(name)))
            .IsEqualTo(expected ? "boolean:true" : "boolean:false");
    }

    [Test]
    public async Task TypeAvailable_ForDateTimeStamp_IsTrueWhereTheStaticContextIsXPath31()
    {
        var statics = Statics.ToBuilder().WithVersion(XPathVersion.V31).Build();
        var scope = new XsltFunctionScope(new XPathQueryBinding("xslt3", true), statics);

        await Assert.That(Run(scope, "type-available", S("xs:dateTimeStamp"))).IsEqualTo("boolean:true");
    }

    [Test]
    [Arguments("")]
    [Arguments("1x")]
    [Arguments("a:b:c")]
    [Arguments("zz:string")]
    [Arguments(" xs:string")]
    public async Task TypeAvailable_ForAnInvalidNameOrAnUnboundPrefix_IsXtde1428(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        await Assert.That(Run(Scope("xslt3"), "type-available", S(name))).IsEqualTo("error:XTDE1428");
    }

    [Test]
    public async Task TypeAvailable_IsNotFoundUnderAnXpathBinding() =>
        await Assert.That(Run(Scope("xpath2"), "type-available", S("xs:string"))).IsEqualTo("notfound");
}
