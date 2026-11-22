namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Pipeline;

using NetEvolve.Schemas;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts.Fakes;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Pipeline.PipelineHarness;

/// <summary>The issues of a run of the validating reader mode with the steps of every validity constraint.</summary>
public class PipelineIssueTests
{
    private const string TwoErrorSubset =
        "\n<!ELEMENT r (a*)>\n<!ELEMENT a EMPTY>\n<!ATTLIST a k (x|y) #IMPLIED m CDATA #REQUIRED>\n";

    private const string TwoErrors = "<r>\n<a k=\"z\" m=\"1\"/>\n<a k=\"x\"/>\n</r>";

    [Test]
    public async Task Document_WithTwoDifferentValidityErrors_ReportsBothWithCodesAndPositions()
    {
        var result = Validate(Doc(TwoErrorSubset, TwoErrors));

        var issues = result.Issues.Issues;
        await Assert.That(result.IsWellFormed).IsTrue();
        await Assert.That(result.Issues.IsValid).IsFalse();
        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5019.Code);
        await Assert.That(issues[0].Constraint).IsEqualTo("Enumeration");
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Location).IsEqualTo(new DocumentLocation(DocumentUri, 7, 4));
        await Assert.That(issues[1].Code).IsEqualTo(DtdValidityCodes.XML5020.Code);
        await Assert.That(issues[1].Constraint).IsEqualTo("Required Attribute");
        await Assert.That(issues[1].Location).IsEqualTo(new DocumentLocation(DocumentUri, 8, 1));
    }

    [Test]
    public async Task Issue_ValidityError_DoesNotStopTheParse_AndTheEndOfTheStepsFires()
    {
        var step = new ScriptedDtdValidationStep().On("StartElement:a", c =>
            c.Report(DtdValidityCodes.XML5004, IssueSeverity.Error, c.Locate(new XmlPosition(2, 1, 0)))
        );

        var (result, sink) = Run(Doc(TwoErrorSubset, TwoErrors), step);

        await Assert.That(result.IsWellFormed).IsTrue();
        await Assert.That(result.Diagnostic).IsNull();
        await Assert.That(result.Issues.Issues.Count).IsEqualTo(2);
        await Assert.That(step.Events[^1]).IsEqualTo("End");
        await Assert.That(sink.Events[^1]).IsEqualTo("EndDocument");
    }

    [Test]
    public async Task Issue_ValidityErrors_AreInEventOrder_AcrossTheStepsOfOneEvent()
    {
        const string Subset = "<!ELEMENT r (a)><!ELEMENT a EMPTY><!ATTLIST r k (x|y) #IMPLIED>";

        var result = Validate(Doc(Subset, "<r k=\"z\"><b/></r>"));

        // The attribute error of r comes with the start tag of r; then b is not allowed in r and not declared.
        await Assert
            .That(result.Issues.Issues.Select(i => i.Code))
            .IsEquivalentTo([
                DtdValidityCodes.XML5019.Code,
                DtdValidityCodes.XML5004.Code,
                DtdValidityCodes.XML5004.Code,
            ]);
    }

    [Test]
    public async Task Document_WithoutDoctype_ReportsTheMissingDeclarationAsAValidityIssue()
    {
        var result = Validate("<r/>");

        var issue = result.Issues.Issues[0];
        await Assert.That(result.IsWellFormed).IsTrue();
        await Assert.That(result.Issues.IsValid).IsFalse();
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5910.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo(DtdValidityCodes.XML5910.Name);
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(DocumentUri, 1, 1));
        await Assert.That(result.Issues.Issues.Count(i => i.Code == DtdValidityCodes.XML5910.Code)).IsEqualTo(1);
        await Assert.That(result.Issues.Issues.Any(i => i.Code == DtdValidityCodes.XML5001.Code)).IsFalse();
    }

    [Test]
    public async Task Document_WithoutDoctype_AlsoReportsEveryElementAsUndeclared_BecauseEveryStepSeesTheEmptyModel()
    {
        // Decision on #327: the steps run unchanged against the empty model of design #122, D2, so Element Valid
        // reports each element of a document without a declaration; the missing declaration is reported once.
        var result = Validate("<r><a/></r>");

        await Assert
            .That(result.Issues.Issues.Select(i => i.Code))
            .IsEquivalentTo([
                DtdValidityCodes.XML5910.Code,
                DtdValidityCodes.XML5004.Code,
                DtdValidityCodes.XML5004.Code,
            ]);
    }

    [Test]
    public async Task Document_ThatIsValid_ReportsNoIssue()
    {
        const string Subset =
            "<!ELEMENT r (a+)><!ELEMENT a (#PCDATA)><!ATTLIST a id ID #REQUIRED k (x|y) \"x\"><!ATTLIST r ref IDREF #IMPLIED>";

        var result = Validate(Doc(Subset, "<r ref=\"i1\"><a id=\"i1\">t</a><a id=\"i2\" k=\"y\"/></r>"));

        await Assert.That(result.IsWellFormed).IsTrue();
        await Assert.That(result.Issues.Issues).IsEmpty();
        await Assert.That(result.Issues.IsValid).IsTrue();
    }

    [Test]
    public async Task Issue_FatalWellFormednessError_IsTheLastIssue_AndKeepsTheEarlierValidityErrors()
    {
        var result = Validate(Doc("<!ELEMENT r EMPTY>", "<r>x<a></r>"));

        var issues = result.Issues.Issues;
        await Assert.That(result.IsWellFormed).IsFalse();
        await Assert.That(result.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(result.Diagnostic!.Code).IsSameReferenceAs(XmlErrorCodes.XML0005);
        await Assert.That(result.Issues.IsValid).IsFalse();
        await Assert.That(issues.Count).IsEqualTo(3);
        await Assert.That(issues[0].Code).IsEqualTo(DtdValidityCodes.XML5004.Code);
        await Assert.That(issues[2].Code).IsEqualTo(XmlErrorCodes.XML0005.Code);
        await Assert.That(issues[2].Constraint).IsEqualTo(XmlErrorCodes.XML0005.Constraint);
        await Assert.That(issues[2].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[2].Location.Source).IsEqualTo(DocumentUri);
        await Assert.That(issues[2].Location.Line).IsEqualTo(result.Diagnostic.Position.Line);
        await Assert.That(result.Issues.IsTruncated).IsTrue();
    }

    [Test]
    public async Task Issue_FatalStatusOfTheReader_IsDroppedWhenAStepStoppedTheRunBefore()
    {
        var stopping = new ScriptedDtdValidationStep().On("Begin", c => c.Stop(DtdLimitCodes.XML1118, default));

        var (result, _) = Run(Doc("<!ELEMENT r ANY>", "<r><a></r>"), stopping);

        await Assert.That(result.IsWellFormed).IsFalse();
        await Assert.That(result.Issues.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues.Issues[0].Code).IsEqualTo(DtdLimitCodes.XML1118.Code);
    }

    [Test]
    public async Task Issue_Truncation_FollowsTheLimitsOfTheOptions()
    {
        var options = DtdValidationOptions.Default.WithLimits(ValidationLimits.Default.WithMaxIssues(1));

        var result = Validate(Doc(TwoErrorSubset, TwoErrors), options);

        await Assert.That(result.Issues.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues.IsTruncated).IsTrue();
        await Assert.That(result.Issues.IsValid).IsFalse();
    }

    [Test]
    public async Task Issue_Warning_KeepsTheDocumentValid()
    {
        const string Subset = "<!ELEMENT r ((a,b)|(a,c))><!ELEMENT a EMPTY><!ELEMENT b EMPTY><!ELEMENT c EMPTY>";

        var result = Validate(Doc(Subset, "<r><a/><b/></r>"));

        await Assert.That(result.Issues.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5911.Code);
        await Assert.That(result.Issues.Issues[0].Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(result.Issues.IsValid).IsTrue();
    }

    [Test]
    public async Task Issue_NonDeterministicContentModel_RejectModeReportsAnError()
    {
        const string Subset = "<!ELEMENT r ((a,b)|(a,c))><!ELEMENT a EMPTY><!ELEMENT b EMPTY><!ELEMENT c EMPTY>";
        var options = DtdValidationOptions.Default.WithNonDeterministicContentModels(DtdNonDeterminism.Reject);

        var result = Validate(Doc(Subset, "<r><a/><b/></r>"), options);

        await Assert.That(result.Issues.Issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(result.Issues.IsValid).IsFalse();
    }
}
