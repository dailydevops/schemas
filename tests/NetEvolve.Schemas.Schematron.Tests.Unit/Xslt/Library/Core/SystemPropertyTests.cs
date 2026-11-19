namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Xslt.Library.Core;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Library.Core.CoreFixture;

/// <summary>
/// <c>system-property()</c> (XSLT 2.0, 16.6.5): a string per property of the XSLT namespace, the zero-length string for every other
/// name, and XTDE1390 for a name that is not a lexical QName or whose prefix is not bound. The values are fixed constants of the
/// package (design #150 EP-13: no host data); decision D2 of issue #285 pins them.
/// </summary>
public class SystemPropertyTests
{
    private static readonly string ProductVersion = typeof(XsltFunctionScope).Assembly.GetName().Version!.ToString();

    [Test]
    [Arguments("xsl:version", "2.0")]
    [Arguments("xsl:vendor", "NetEvolve")]
    [Arguments("xsl:vendor-url", "https://github.com/dailydevops/schemas")]
    [Arguments("xsl:product-name", "NetEvolve.Schemas.Schematron")]
    [Arguments("xsl:is-schema-aware", "no")]
    [Arguments("xsl:supports-serialization", "no")]
    [Arguments("xsl:supports-backwards-compatibility", "no")]
    [Arguments("xsl:supports-namespace-axis", "no")]
    public async Task SystemProperty_ForAStandardPropertyName_ReturnsTheDocumentedValue(string name, string expected)
    {
        await Assert.That(Run(Scope(), "system-property", S(name))).IsEqualTo("string:" + expected);
    }

    [Test]
    public async Task SystemProperty_ProductVersion_IsTheVersionOfTheAssembly()
    {
        await Assert
            .That(Run(Scope(), "system-property", S("xsl:product-version")))
            .IsEqualTo("string:" + ProductVersion);
    }

    [Test]
    public async Task SystemProperty_ForTheSameNamespaceUnderAnotherPrefix_ReturnsTheValue()
    {
        await Assert.That(Run(Scope(), "system-property", S("x:version"))).IsEqualTo("string:2.0");
    }

    [Test]
    public async Task SystemProperty_UnderEveryXsltBinding_ReturnsVersionTwoPointZero()
    {
        foreach (var binding in new[] { "xslt", "xslt1", "xslt2", "xslt3" })
        {
            await Assert.That(Run(Scope(binding), "system-property", S("xsl:version"))).IsEqualTo("string:2.0");
        }
    }

    [Test]
    [Arguments("xsl:unknown")]
    [Arguments("xsl:supports-streaming")]
    [Arguments("version")]
    [Arguments("f:version")]
    [Arguments("xsl:Version")]
    public async Task SystemProperty_ForAnUnknownProperty_IsTheZeroLengthString(string name)
    {
        await Assert.That(Run(Scope(), "system-property", S(name))).IsEqualTo("string:");
    }

    [Test]
    [Arguments("")]
    [Arguments("1x")]
    [Arguments("a:b:c")]
    [Arguments(" xsl:version")]
    [Arguments("zz:version")]
    [Arguments("xsl:")]
    public async Task SystemProperty_ForAnInvalidNameOrAnUnboundPrefix_IsXtde1390(string name)
    {
        await Assert.That(Run(Scope(), "system-property", S(name))).IsEqualTo("error:XTDE1390");
    }

    [Test]
    public async Task SystemProperty_IsNotFoundUnderAnXpathBinding()
    {
        await Assert.That(Run(Scope("xpath2"), "system-property", S("xsl:version"))).IsEqualTo("notfound");
    }
}
