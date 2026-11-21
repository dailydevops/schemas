namespace NetEvolve.Schemas.Schematron.Tests.Unit.Editions;

using System;
using NetEvolve.Schemas.Schematron.Editions;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>The edition detection of design #106 Q11: 2025 selects 2025, an absent or 2016 value selects 2016, anything else is an error.</summary>
public class SchematronEditionDetectorTests
{
    private static readonly DocumentLocation Where = new("file:///schemas/edition.sch", 7, 3);

    [Test]
    public async Task Detect_2025_SelectsIso2025() =>
        await Assert.That(SchematronEditionDetector.Detect("2025", Where)).IsEqualTo(SchematronEdition.Iso2025);

    [Test]
    public async Task Detect_AbsentAttribute_SelectsIso2016() =>
        await Assert.That(SchematronEditionDetector.Detect(null, Where)).IsEqualTo(SchematronEdition.Iso2016);

    [Test]
    public async Task Detect_2016_SelectsIso2016() =>
        await Assert.That(SchematronEditionDetector.Detect("2016", Where)).IsEqualTo(SchematronEdition.Iso2016);

    [Test]
    [Arguments("2030")]
    [Arguments("2015")]
    [Arguments("")]
    [Arguments(" 2025")]
    [Arguments("2025 ")]
    [Arguments("2025.0")]
    [Arguments("Iso2025")]
    public async Task Detect_UnknownValue_ThrowsCompilationExceptionWithCodeAndLocation(string text)
    {
        var exception = Assert.Throws<SchemaCompilationException>(() => SchematronEditionDetector.Detect(text, Where));

        await Assert.That(exception.Issues.Count).IsEqualTo(1);
        var issue = exception.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(SchematronDiagnosticCodes.EditionUnknown);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Location).IsEqualTo(Where);
        await Assert.That(issue.Message).IsEqualTo(SchematronEditionDetector.UnknownMessage(text));
    }

    [Test]
    [Arguments("2025", SchematronEdition.Iso2025)]
    [Arguments("2016", SchematronEdition.Iso2016)]
    [Arguments(null, SchematronEdition.Iso2016)]
    public async Task TryDetect_KnownValue_ReturnsTrueWithTheEdition(string? text, SchematronEdition expected)
    {
        var known = SchematronEditionDetector.TryDetect(text, out var edition);

        await Assert.That(known).IsTrue();
        await Assert.That(edition).IsEqualTo(expected);
    }

    [Test]
    [Arguments("2030")]
    [Arguments("")]
    [Arguments("2025 ")]
    public async Task TryDetect_UnknownValue_ReturnsFalseWithTheEdition2016Fallback(string text)
    {
        var known = SchematronEditionDetector.TryDetect(text, out var edition);

        await Assert.That(known).IsFalse();
        await Assert.That(edition).IsEqualTo(SchematronEdition.Iso2016);
    }

    [Test]
    public async Task UnknownMessage_NamesTheAttributeAndTheValue()
    {
        var message = SchematronEditionDetector.UnknownMessage("2030");

        await Assert.That(message).Contains("schematronEdition");
        await Assert.That(message).Contains("'2030'");
    }

    [Test]
    public async Task UnknownMessage_NullValue_ThrowsArgumentNullException() =>
        await Assert.That(() => SchematronEditionDetector.UnknownMessage(null!)).Throws<ArgumentNullException>();
}
