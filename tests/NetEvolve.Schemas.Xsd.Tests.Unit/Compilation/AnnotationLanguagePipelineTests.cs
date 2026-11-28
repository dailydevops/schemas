namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Versioning.Constructs;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>The <c>xml:lang</c> value of <c>documentation</c> and <c>appinfo</c> is an <c>xs:language</c> in XSD 1.0 only (#1179).</summary>
public class AnnotationLanguagePipelineTests
{
    private static string Annotated(string element, string lang) =>
        Schema("<xs:annotation><xs:" + element + " xml:lang=\"" + lang + "\"/></xs:annotation>");

    [Test]
    [Arguments("documentation", "")]
    [Arguments("documentation", " ")]
    [Arguments("appinfo", "")]
    [Arguments("documentation", "a-")]
    [Arguments("documentation", "toolongsub")]
    [Arguments("documentation", "en_US")]
    public async Task Xsd10_NotALanguage_IsReported(string element, string lang)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(lang);

        var compilation = Compile(Annotated(element, lang), XsdVersion.V10);

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SrcAnnotation]);
    }

    [Test]
    [Arguments("documentation", "")]
    [Arguments("documentation", " ")]
    [Arguments("appinfo", "en")]
    [Arguments("documentation", "en-US")]
    [Arguments("documentation", " english ")]
    public async Task Xsd11_AnyValue_IsAccepted(string element, string lang)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(lang);

        var compilation = Compile(Annotated(element, lang), XsdVersion.V11);

        await Assert.That(compilation.IsValid).IsTrue();
    }

    [Test]
    [Arguments("en")]
    [Arguments("en-US")]
    [Arguments(" english ")]
    [Arguments("x-1234abcd")]
    public async Task Xsd10_Language_IsAccepted(string lang)
    {
        ArgumentNullException.ThrowIfNull(lang);

        var compilation = Compile(Annotated("documentation", lang), XsdVersion.V10);

        await Assert.That(compilation.IsValid).IsTrue();
    }

    [Test]
    public async Task CheckLanguages_NullArguments_Throw()
    {
        var reporter = SchemaFixture.NewReporter(out _);
        var document = SchemaFixture.Parse(Schema(string.Empty)).Document;

        await Assert
            .That(() => Strict10Constructs.CheckLanguages(null!, XsdVersion.V10, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => Strict10Constructs.CheckLanguages(document, XsdVersion.V10, null!))
            .Throws<ArgumentNullException>();
    }
}
