namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.ContentModels;

using System;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Validation.ContentModels;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.ContentModels.ContentModelHarness;

/// <summary>The Element Valid report: code, severity, position of the first offending item, and what counts as character data.</summary>
public class ContentModelReportTests
{
    [Test]
    public async Task Issue_IsAnElementValidError_WithTheCodeConstraintAndSystemId()
    {
        var run = Run("<r>~<b/></r>", null, WithLeaves(Children("r", "(a)")));

        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5004.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("Element Valid");
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(SystemId, 1, 4));
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5004.Message);
        await Assert.That(issue.Message).Contains("'b'");
    }

    [Test]
    public async Task Issue_PositionOfALaterLine_CarriesLineAndColumn()
    {
        var run = Run("<r>\n  <a/>\n  ~<c/>\n</r>", null, WithLeaves(Children("r", "(a,b)")));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Location).IsEqualTo(new DocumentLocation(SystemId, 3, 3));
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
    }

    [Test]
    public async Task OnlyTheFirstOffendingChildOfAnElement_IsReported()
    {
        var run = Run("<r>~<b/><b/><b/></r>", null, WithLeaves(Children("r", "(a)")));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
    }

    [Test]
    public async Task EachElementIsCheckedOnItsOwn_AnOffendingChildDoesNotHideItsParentsLaterError()
    {
        // The inner element is wrong at its own end tag; the outer one is wrong at its second child.
        var run = Run(
            "<r><s><a/>~</s><s><a/><b/></s></r>",
            null,
            Children("r", "(s)"),
            Children("s", "(a,b)"),
            Empty("a"),
            Empty("b")
        );

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[1].Location).IsEqualTo(new DocumentLocation(SystemId, 1, 15));
    }

    [Test]
    public async Task Element_WithoutADeclaration_ReportsXml5004()
    {
        var run = Run("~<z/>", null);

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5004.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[0].Message).Contains("'z'");
    }

    [Test]
    public async Task UndeclaredChild_OfAChildrenModel_ReportsTheParentMismatchAndTheMissingDeclaration()
    {
        var run = Run("<r>~<z/></r>", null, Children("r", "(a)"), Empty("a"));

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[1].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[0].Message).IsNotEqualTo(run.Issues[1].Message);
    }

    [Test]
    public async Task ChildrenOfAnUndeclaredElement_AreNotMatchedAgainstAnything()
    {
        var run = Run("~<z><a/>text</z>", null, Empty("a"));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
    }

    [Test]
    public async Task ElementContent_CommentsAndProcessingInstructions_AreAllowed()
    {
        var run = Run("<r><!-- c --><a/><?pi data?><!--d--></r>", null, WithLeaves(Children("r", "(a)")));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    [Arguments("<r>~x<a/></r>")]
    [Arguments("<r><a/>  ~x</r>")]
    [Arguments("<r><a/>\n ~y z</r>")]
    [Arguments("<r><a/><![CDATA[~ ]]></r>")]
    [Arguments("<r><a/><![CDATA[~x]]></r>")]
    [Arguments("<r><a/>~&#32;</r>")]
    [Arguments("<r><a/>~&#13;</r>")]
    [Arguments("<r><a/>~&amp;</r>")]
    [Arguments("<r><a/>~&#65;</r>")]
    public async Task ElementContent_CharacterData_IsReportedAtTheFirstCharacterThatIsNotWhiteSpace(string markedScript)
    {
        var run = Run(markedScript, null, WithLeaves(Children("r", "(a)")));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5004.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
    }

    [Test]
    [Arguments("<r> <a/></r>")]
    [Arguments("<r>\t<a/>\n</r>")]
    [Arguments("<r>\n\n  <a/>  \n</r>")]
    public async Task ElementContent_WhiteSpace_IsAllowed(string script)
    {
        var run = Run(script, null, WithLeaves(Children("r", "(a)")));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task ElementContent_LineFeedWrittenAsACharacterReference_IsAccepted_LimitationOfTheEventStream()
    {
        // The reader reports a line break and a character reference to a line feed as the same event (kind Reference,
        // text "\n"), so the step cannot tell them apart and accepts both. Every other reference text is character data.
        var run = Run("<r><a/>&#10;</r>", null, WithLeaves(Children("r", "(a)")));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task ElementContent_EntityReference_IsNotCharacterData()
    {
        var run = Run("<r><a/>&e;</r>", null, WithLeaves(Children("r", "(a)")));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task ElementContent_ReplacementEventsOfAnEntity_AreValidatedAsContentOfTheElement()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = new DtdValidationContext(
            Declarations("r", WithLeaves(Children("r", "(a)"))),
            DtdValidationOptions.Default,
            issues,
            isStandalone: false,
            SystemId,
            default
        );
        var step = new DtdContentModelStep();
        var p = new XmlPosition(1, 1, 0);
        var inside = new XmlPosition(1, 9, 8);

        step.BeginDocument(context);
        step.StartElement(new ValidationElement("r".AsMemory(), p));
        step.EntityReference(new ValidationEntityReference("e".AsMemory(), p, DtdEntityOutcome.DeclaredInternal));
        step.StartElement(new ValidationElement("a".AsMemory(), p));
        step.EndElement(new ValidationElement("a".AsMemory(), p));
        step.Text("tail".AsSpan(), XmlTextKind.Text, in inside);
        step.EndElement(new ValidationElement("r".AsMemory(), p));
        step.EndDocument();

        var result = issues.ToResult();
        await Assert.That(result.Issues.Count).IsEqualTo(1);
        await Assert.That(result.Issues[0].Location).IsEqualTo(new DocumentLocation(SystemId, 1, 9));
    }

    [Test]
    public async Task CommentsAndProcessingInstructions_OutsideAnyElement_AreIgnored()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = new DtdValidationContext(
            Declarations("r", WithLeaves(Children("r", "(a)"))),
            DtdValidationOptions.Default,
            issues,
            isStandalone: false,
            SystemId,
            default
        );
        var step = new DtdContentModelStep();
        var p = new XmlPosition(1, 1, 0);

        step.BeginDocument(context);
        step.Comment(in p);
        step.ProcessingInstruction(in p);
        step.StartElement(new ValidationElement("r".AsMemory(), p));
        step.StartElement(new ValidationElement("a".AsMemory(), p));
        step.EndElement(new ValidationElement("a".AsMemory(), p));
        step.EndElement(new ValidationElement("r".AsMemory(), p));
        step.Comment(in p);
        step.EndDocument();

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task BeginDocument_StartsAFreshRun_NoStateLeaksFromTheDocumentBefore()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = new DtdValidationContext(
            Declarations("r", WithLeaves(Children("r", "(a,b)"))),
            DtdValidationOptions.Default,
            issues,
            isStandalone: false,
            SystemId,
            default
        );
        var step = new DtdContentModelStep();
        var p = new XmlPosition(1, 1, 0);

        // The first document ends in the middle of the content of r (a fatal error ended it): the step is reused.
        step.BeginDocument(context);
        step.StartElement(new ValidationElement("r".AsMemory(), p));
        step.StartElement(new ValidationElement("a".AsMemory(), p));
        step.EndElement(new ValidationElement("a".AsMemory(), p));

        step.BeginDocument(context);
        step.StartElement(new ValidationElement("r".AsMemory(), p));
        step.StartElement(new ValidationElement("a".AsMemory(), p));
        step.EndElement(new ValidationElement("a".AsMemory(), p));
        step.StartElement(new ValidationElement("b".AsMemory(), p));
        step.EndElement(new ValidationElement("b".AsMemory(), p));
        step.EndElement(new ValidationElement("r".AsMemory(), p));
        step.EndDocument();

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Step_AfterTheContextWasStopped_ReportsNothing()
    {
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = new DtdValidationContext(
            Declarations("r", WithLeaves(Children("r", "(a)"))),
            DtdValidationOptions.Default,
            issues,
            isStandalone: false,
            SystemId,
            default
        );
        var step = new DtdContentModelStep();
        var p = new XmlPosition(1, 1, 0);

        step.BeginDocument(context);
        context.Stop(DtdLimitCodes.XML1117, default);
        step.StartElement(new ValidationElement("r".AsMemory(), p));
        step.StartElement(new ValidationElement("z".AsMemory(), p));

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Step_RejectsAMissingContext()
    {
        var step = new DtdContentModelStep();

        await Assert.That(() => step.BeginDocument(null!)).Throws<ArgumentNullException>();
    }
}
