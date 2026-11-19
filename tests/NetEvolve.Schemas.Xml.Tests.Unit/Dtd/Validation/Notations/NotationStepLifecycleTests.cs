namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Notations;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations.NotationHarness;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations.NotationModel;

/// <summary>Guards, reuse of one step for several documents, a stopped run, and the events that are not attribute values.</summary>
public class NotationStepLifecycleTests
{
    private static readonly XmlPosition Origin = new(1, 1, 0);

    [Test]
    public async Task BeginDocument_WithoutContext_Throws()
    {
        var step = new DtdNotationStep();

        await Assert.That(() => step.BeginDocument(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(-1)]
    [Arguments(3)]
    public async Task Text_WithAnUndefinedKind_Throws(int kind)
    {
        var step = new DtdNotationStep();

        await Assert
            .That(() => step.Text("x".AsSpan(), (XmlTextKind)kind, new XmlPosition(1, 1, 0)))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Events_BeforeBeginDocument_AreIgnored()
    {
        var step = new DtdNotationStep();

        step.StartElement(Element("e", "n", "x", DtdAttributeType.Notation));
        step.StartElement(Element("e", "n", "x", DtdAttributeType.Entity));
        step.EntityReference(new ValidationEntityReference("x".AsMemory(), Origin, DtdEntityOutcome.DeclaredInternal));
        step.Text("x".AsSpan(), XmlTextKind.Text, Origin);
        step.Comment(Origin);
        step.ProcessingInstruction(Origin);
        step.EndElement(new ValidationElement("e".AsMemory(), Origin));
        step.EndDocument();

        await Assert.That(step).IsNotNull();
    }

    [Test]
    public async Task EventsThatAreNotStartTags_ReportNothing()
    {
        var run = Run("<r>text<?pi?><!--c--></r>", new NotationModel().Element("r").Build());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task Step_ReusedForASecondDocument_ForgetsTheDeclarationsOfTheFirst()
    {
        var step = new DtdNotationStep();
        var first = new NotationModel()
            .Element("r")
            .Attlist("r", EntityAttribute("e"), NotationAttribute("n", "gif"))
            .Notation("gif")
            .Entity(Unparsed("logo", "gif"))
            .Build();
        var second = new NotationModel()
            .Element("r")
            .Attlist("r", EntityAttribute("e"))
            .Entity(Unparsed("photo", "gif"))
            .Notation("gif")
            .Build();

        var firstIssues = new ValidationIssueCollector(ValidationLimits.Default);
        step.BeginDocument(Context(firstIssues, first));
        step.StartElement(Element("r", "e", "logo", DtdAttributeType.Entity));
        step.StartElement(Element("r", "n", "gif", DtdAttributeType.Notation));

        var secondIssues = new ValidationIssueCollector(ValidationLimits.Default);
        step.BeginDocument(Context(secondIssues, second));
        step.StartElement(Element("r", "e", "logo", DtdAttributeType.Entity));
        step.StartElement(Element("r", "e", "photo", DtdAttributeType.Entity));
        step.StartElement(Element("r", "n", "gif", DtdAttributeType.Notation));

        // "logo" was declared in the first document only; the NOTATION attribute of the first document is gone.
        await Assert.That(firstIssues.Count).IsEqualTo(0);
        await Assert.That(secondIssues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task StoppedRun_IgnoresLaterEvents()
    {
        var model = new NotationModel().Element("r").Attlist("r", EntityAttribute("e")).Build();
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = Context(issues, model);
        var step = new DtdNotationStep();
        step.BeginDocument(context);
        context.Stop(DtdLimitCodes.XML1118, default);

        step.StartElement(Element("r", "e", "ghost", DtdAttributeType.Entity));

        await Assert.That(issues.IsStopped).IsTrue();
        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Run_StopsReportingDeclarationChecks_WhenTheContextIsStopped()
    {
        var model = new NotationModel().Entity(Unparsed("a", "x")).Entity(Unparsed("b", "x")).Build();
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = Context(issues, model);
        context.Stop(DtdLimitCodes.XML1118, default);

        new DtdNotationStep().BeginDocument(context);

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task AttributeOfATypeOtherThanEntityEntitiesOrNotation_IsNeverChecked()
    {
        var model = new NotationModel().Element("r").Build();
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var step = new DtdNotationStep();
        step.BeginDocument(Context(issues, model));

        step.StartElement(Element("r", "x", "ghost", DtdAttributeType.IdRef));
        step.StartElement(Element("r", "x", "ghost", DtdAttributeType.Enumeration));
        step.StartElement(Element("r", "x", "ghost", DtdAttributeType.NmTokens));

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task NotationAttribute_ThatTheViewDoesNotDefine_IsNotChecked()
    {
        // A consumer view can report a declared type for an attribute that its own list does not contain; there is no list to match.
        var model = new NotationModel().Element("r").Build();
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var step = new DtdNotationStep();
        step.BeginDocument(Context(issues, model));

        step.StartElement(Element("r", "n", "anything", DtdAttributeType.Notation));

        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ValidDocument_AllocatesNothingPerElement()
    {
        var model = new NotationModel()
            .Element("r")
            .Attlist("r", EntityAttribute("e"), EntitiesAttribute("es"), NotationAttribute("n", "gif", "png"))
            .Notation("gif")
            .Notation("png")
            .Entity(Unparsed("logo", "gif"))
            .Entity(Unparsed("photo", "gif"))
            .Build();
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var step = new DtdNotationStep();
        step.BeginDocument(Context(issues, model));
        var attributes = new[]
        {
            new ValidationAttributeInfo("e".AsMemory(), "logo".AsMemory(), DtdAttributeType.Entity, true, Origin),
            new ValidationAttributeInfo(
                "es".AsMemory(),
                "logo photo logo".AsMemory(),
                DtdAttributeType.Entities,
                true,
                Origin
            ),
            new ValidationAttributeInfo("n".AsMemory(), "png".AsMemory(), DtdAttributeType.Notation, true, Origin),
        };
        var element = new ValidationElement("r".AsMemory(), Origin, attributes);
        step.StartElement(in element);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            step.StartElement(in element);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(allocated).IsLessThan(4096);
    }
}
