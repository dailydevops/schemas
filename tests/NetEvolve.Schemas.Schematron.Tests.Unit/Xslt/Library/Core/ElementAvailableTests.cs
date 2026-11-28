namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core;

using System.Threading.Tasks;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core.CoreFixture;

/// <summary>
/// <c>element-available()</c> (XSLT 2.0, 18.1.1): true for an XSLT instruction that the instruction registry has, false for an
/// element of another namespace, an unknown or deferred XSLT element, and an XSLT element that is not an instruction; XTDE1440 for a
/// name that is not a lexical QName or whose prefix is not bound.
/// </summary>
public class ElementAvailableTests
{
    [Test]
    [Arguments("xsl:copy-of", true)]
    [Arguments("x:copy-of", true)]
    [Arguments("xsl:element", true)]
    [Arguments("xsl:comment", true)]
    [Arguments("xsl:no-such-instruction", false)]
    [Arguments("xsl:stylesheet", false)]
    [Arguments("f:copy-of", false)]
    [Arguments("copy-of", false)]
    public async Task ElementAvailable_AnswersFromTheInstructionRegistry(string name, bool expected) =>
        await Assert
            .That(Run(Scope(), "element-available", S(name)))
            .IsEqualTo(expected ? "boolean:true" : "boolean:false");

    [Test]
    [Arguments("")]
    [Arguments("1x")]
    [Arguments("a:b:c")]
    [Arguments("zz:copy-of")]
    [Arguments(" xsl:copy-of")]
    public async Task ElementAvailable_ForAnInvalidNameOrAnUnboundPrefix_IsXtde1440(string name) =>
        await Assert.That(Run(Scope(), "element-available", S(name))).IsEqualTo("error:XTDE1440");

    [Test]
    public async Task ElementAvailable_IsNotFoundUnderAnXpathBinding() =>
        await Assert.That(Run(Scope("xpath2"), "element-available", S("xsl:copy-of"))).IsEqualTo("notfound");
}
