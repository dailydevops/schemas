namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Document;

using System;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Document;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Document.DocumentHarness;

/// <summary>Validity constraint Root Element Type (XML 1.0, section 2.8): the root element type matches the name of the document type declaration.</summary>
public class RootElementTypeTests
{
    [Test]
    public async Task Root_WithAnotherNameThanTheDocumentType_ReportsXml5001AtTheStartTag()
    {
        var session = new Session(Declare("doc", [Element("other")])).Start("other", At(3, 1));

        await Assert.That(session.Result.Count).IsEqualTo(1);
        var issue = session.Result[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5001.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Root Element Type");
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(SystemId, 3, 1));
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5001.Message);
        await Assert.That(issue.Message).Contains("'other'");
        await Assert.That(issue.Message).Contains("'doc'");
    }

    [Test]
    public async Task Root_WithTheNameOfTheDocumentType_ReportsNothing()
    {
        var session = new Session(Declare("doc", [Element("doc")])).Start("doc", At(3, 1)).End("doc").Finish();

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Root_NameComparison_IsCaseSensitive()
    {
        var session = new Session(Declare("doc", [Element("DOC")])).Start("DOC", At(3, 1));

        await Assert.That(session.Result.Count).IsEqualTo(1);
        await Assert.That(session.Result[0].Code).IsEqualTo(DtdValidityCodes.XML5001.Code);
    }

    [Test]
    public async Task Child_WithAnotherNameThanTheDocumentType_IsNotARootElement()
    {
        var session = new Session(Declare("doc", [Element("doc"), Element("child")]))
            .Start("doc", At(3, 1))
            .Start("child", At(3, 6))
            .End("child")
            .End("doc");

        await Assert.That(session.Result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Root_IsReportedOnce_NotOncePerEnclosedElement()
    {
        var session = new Session(Declare("doc", [Element("wrong")]))
            .Start("wrong", At(3, 1))
            .Start("wrong", At(3, 8))
            .End("wrong")
            .End("wrong");

        await Assert.That(session.Result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Step_ReusedForASecondDocument_ChecksTheRootOfThatDocument()
    {
        var step = new DtdDocumentStep();
        var first = new ValidationIssueCollector(ValidationLimits.Default);
        var second = new ValidationIssueCollector(ValidationLimits.Default);

        step.BeginDocument(Context(Declare("doc"), first));
        step.StartElement(new ValidationElement("doc".AsMemory(), At(1, 1)));
        step.EndElement(new ValidationElement("doc".AsMemory(), At(1, 6)));
        step.EndDocument();
        step.BeginDocument(Context(Declare("doc"), second));
        step.StartElement(new ValidationElement("other".AsMemory(), At(1, 1)));

        await Assert.That(first.Count).IsEqualTo(0);
        await Assert.That(second.Count).IsEqualTo(1);
    }

    private static DtdValidationContext Context(IDtdDeclarations declarations, ValidationIssueCollector issues) =>
        new(declarations, DtdValidationOptions.Default, issues, isStandalone: false, SystemId, DoctypeLocation);

    [Test]
    public async Task Guards_RejectANullContext_AndAnUndefinedTextKind()
    {
        var step = new DtdDocumentStep();

        await Assert.That(() => step.BeginDocument(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => step.Text("x".AsSpan(), (XmlTextKind)99, At(1, 1)))
            .Throws<ArgumentOutOfRangeException>();
    }
}
