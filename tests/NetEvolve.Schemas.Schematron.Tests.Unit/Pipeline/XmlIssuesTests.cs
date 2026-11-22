namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using NetEvolve.Schemas.Schematron.Pipeline;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;

/// <summary>
/// The join to the Xml package (SCH-55, #311): the limits of the validator become the limits of the reader, and a failed read becomes
/// an issue at the position of the reader, with the core code when the condition is a limit.
/// </summary>
public class XmlIssuesTests
{
    [Test]
    public async Task Options_FollowTheLimitsOfTheValidator()
    {
        var limits = ValidationLimits
            .Default.WithMaxInputSize(1_000)
            .WithMaxDepth(7)
            .WithMaxAttributesPerElement(5)
            .WithMaxEntityExpansion(900)
            .WithMaxEntityDepth(3);

        var reader = XmlIssues.Options(limits).Limits;

        await Assert.That(reader.MaxDocumentCharacters).IsEqualTo(1_000L);
        await Assert.That(reader.MaxElementDepth).IsEqualTo(7);
        await Assert.That(reader.MaxAttributesPerElement).IsEqualTo(5);
        await Assert.That(reader.MaxEntityExpansionCharacters).IsEqualTo(900L);
        await Assert.That(reader.MaxEntityDepth).IsEqualTo(3);
    }

    [Test]
    public async Task Options_NullLimits_Throw() =>
        await Assert.That(() => XmlIssues.Options(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Issue_OfAWellFormednessError_HasTheXmlCodePositionAndConstraint()
    {
        var failure = Fail("<a>\n  <b></a>");

        var issue = XmlIssues.Issue(failure, "file:///x.xml");

        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Code.Number).IsBetween(1000, 1999);
        await Assert.That(issue.Location.Source).IsEqualTo("file:///x.xml");
        await Assert.That(issue.Location.Line).IsEqualTo(2);
        await Assert.That(issue.Constraint).IsNotNull();
    }

    [Test]
    public async Task Issue_OfALimit_HasTheCoreCode()
    {
        var failure = Fail("<a><b><c/></b></a>", XmlIssues.Options(ValidationLimits.Default.WithMaxDepth(2)));

        var issue = XmlIssues.Issue(failure, null);

        await Assert.That(issue.Code).IsEqualTo(CoreDiagnosticCodes.DepthLimitExceeded);
        await Assert.That(issue.Location.Source).IsNull();
    }

    [Test]
    public async Task Issue_WithoutADiagnostic_IsAModuleInvalidIssueWithoutAPosition()
    {
        var issue = XmlIssues.Issue(new XmlParseException("boom"), "file:///x.xml");

        await Assert.That(issue.Code).IsEqualTo(SchematronDiagnosticCodes.ModuleInvalid);
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation("file:///x.xml", 0, 0));
    }

    [Test]
    public async Task Issue_NullFailure_Throws() =>
        await Assert.That(() => XmlIssues.Issue(null!, null)).Throws<ArgumentNullException>();

    private static XmlParseException Fail(string xml, XmlReaderOptions? options = null)
    {
        try
        {
            _ = XmlDocumentLoader.Load(xml.AsSpan(), options);
        }
        catch (XmlParseException failure)
        {
            return failure;
        }

        throw new InvalidOperationException("The document is well formed.");
    }
}
