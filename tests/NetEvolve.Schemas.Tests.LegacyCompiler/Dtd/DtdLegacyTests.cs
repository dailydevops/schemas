namespace NetEvolve.Schemas.Tests.LegacyCompiler.Dtd;

using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>
/// Compiled with C# 13 (no extension member syntax): the DTD entry point is reached through the named static class.
/// The unit tests of the Xml package assert that the extension member returns the same builder type and defaults.
/// </summary>
public class DtdLegacyTests
{
    [Test]
    public async Task ForDtd_NamedClass_ReturnsTheBuilderWithDenyByDefault()
    {
        var builder = DtdSchemaValidatorExtensions.ForDtd();

        await Assert.That(builder).IsTypeOf<DtdValidatorBuilder>();
        await Assert.That(builder.Limits).IsSameReferenceAs(ValidationLimits.Default);
        await Assert.That(builder.Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
        await Assert.That(builder.AllowExternalSubset).IsFalse();
        await Assert.That(builder.NonDeterministicContentModels).IsEqualTo(DtdNonDeterminism.Warn);
    }

    [Test]
    public async Task ForDtd_NamedClass_BuildsAValidatorThatReportsAValidityError()
    {
        var result = ValidateWithTheNamedClass();

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5004.Code);
        await Assert.That(result.Issues[0].Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task ForDtd_NamedClass_BuildsAValidatorThatAcceptsAValidDocument()
    {
        var result = Check("<!DOCTYPE note [<!ELEMENT note (#PCDATA)>]><note>text</note>");

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task ForDtd_NamedClass_DefaultBuilderRefusesAnExternalSubset()
    {
        var result = Check("<!DOCTYPE note SYSTEM \"note.dtd\"><note/>");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Issues[0].Code).IsEqualTo(XmlErrorCodes.XML2002.Code);
    }

    // A synchronous method: the analyzers ask an async test to await BuildAsync, which the sample does not show.
    private static ValidationResult ValidateWithTheNamedClass()
    {
        // <sample>
        var validator = DtdSchemaValidatorExtensions.ForDtd().Build();

        var result = validator.Validate("<!DOCTYPE note [<!ELEMENT note EMPTY>]>\n<note>text</note>");
        // </sample>

        return result;
    }

    private static ValidationResult Check(string xml) => DtdSchemaValidatorExtensions.ForDtd().Build().Validate(xml);
}
