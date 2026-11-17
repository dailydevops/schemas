namespace NetEvolve.Schemas.Tests.LegacyCompiler.Xsd;

using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>
/// Compiled with C# 13 (no extension member syntax): the XSD entry point is reached through the named static class.
/// The unit tests of the Xsd package assert that the extension member returns the same builder type and defaults.
/// </summary>
public class XsdLegacyTests
{
    [Test]
    public async Task ForXsd_NamedClass_ReturnsTheBuilderWithDefaults()
    {
        var builder = XsdSchemaValidatorExtensions.ForXsd();

        await Assert.That(builder).IsTypeOf<XsdValidatorBuilder>();
        await Assert.That(builder.Version).IsEqualTo(XsdVersion.V11);
        await Assert.That(builder.Limits).IsSameReferenceAs(ValidationLimits.Default);
        await Assert.That(builder.Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
    }

    [Test]
    public async Task ForXsd_NamedClass_SelectsStrictVersion10()
    {
        var builder = XsdSchemaValidatorExtensions.ForXsd().WithVersion(XsdVersion.V10);

        await Assert.That(builder.Version).IsEqualTo(XsdVersion.V10);
    }
}
