namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Attributes;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepHarness;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Attributes.AttributeStepModel;

/// <summary>Guards, reuse of one step for several documents, a stopped run, the events that are not start tags, and the allocation of a valid document.</summary>
public class AttributeStepLifecycleTests
{
    private static readonly XmlPosition Origin = new(1, 1, 0);

    [Test]
    public async Task BeginDocument_WithoutContext_Throws()
    {
        var step = new DtdAttributeStep();

        await Assert.That(() => step.BeginDocument(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(-1)]
    [Arguments(3)]
    public async Task Text_WithAnUndefinedKind_Throws(int kind)
    {
        var step = new DtdAttributeStep();

        await Assert
            .That(() => step.Text("x".AsSpan(), (XmlTextKind)kind, Origin))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Events_BeforeBeginDocument_AreIgnored()
    {
        var step = new DtdAttributeStep();

        step.StartElement(Element("e", "n", "x", null));
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
        var run = Run("<r>text<?pi?><!--c--></r>", new AttributeStepModel().Element("r").Build());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task StartTag_WithoutAttributes_OfAnElementWithoutRequiredAttributes_ReportsNothing()
    {
        var run = Run("<r/>", new AttributeStepModel().Element("r").Attlist("r", Cdata("a")).Build());

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task Step_ReusedForASecondDocument_ForgetsTheDeclarationsOfTheFirst()
    {
        var step = new DtdAttributeStep();
        var first = new AttributeStepModel().Element("r").Attlist("r", Required("a")).Build();
        var second = new AttributeStepModel().Element("r").Attlist("r", Cdata("b")).Build();

        var firstIssues = new ValidationIssueCollector(ValidationLimits.Default);
        step.BeginDocument(Context(firstIssues, first));
        step.StartElement(new ValidationElement("r".AsMemory(), Origin));
        step.EndDocument();

        var secondIssues = new ValidationIssueCollector(ValidationLimits.Default);
        step.BeginDocument(Context(secondIssues, second));
        step.StartElement(new ValidationElement("r".AsMemory(), Origin));
        step.EndDocument();

        // "a" is required in the first document only.
        await Assert.That(firstIssues.Count).IsEqualTo(1);
        await Assert.That(secondIssues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task StoppedRun_IgnoresLaterEvents()
    {
        var model = new AttributeStepModel().Element("r").Attlist("r", Required("a")).Build();
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var context = Context(issues, model);
        var step = new DtdAttributeStep();
        step.BeginDocument(context);
        context.Stop(DtdLimitCodes.XML1118, default);

        step.StartElement(Element("r", "x", "1", null));

        await Assert.That(issues.IsStopped).IsTrue();
        await Assert.That(issues.Count).IsEqualTo(1);
    }

    private const int WarmupRuns = 5;
    private const int MeasuredRuns = 5;

    [Test]
    public async Task ValidStartTags_AllocateNothingPerElement()
    {
        var model = new AttributeStepModel()
            .Element("e")
            .Attlist(
                "e",
                Required("id"),
                NmToken("n"),
                NmTokens("ns"),
                Enumeration("en", "yes", "no"),
                Cdata("f", DtdDefaultKind.Fixed, "fixed")
            )
            .Build();
        var element = Valid();
        var issues = new ValidationIssueCollector(ValidationLimits.Default);
        var step = new DtdAttributeStep();
        step.BeginDocument(Context(issues, model));

        var allocated = AllocationAssert.Measure(
            () =>
            {
                for (var i = 0; i < 20_000; i++)
                {
                    step.StartElement(in element);
                    step.EndElement(in element);
                }
            },
            WarmupRuns,
            MeasuredRuns
        );

        // The names are looked up in tables that the declarations built once; a string or a list per element would be
        // hundreds of kilobytes over 20,000 elements.
        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(allocated).IsLessThan(4 * 1024);
    }

    private static ValidationElement Valid()
    {
        var attributes = new[]
        {
            Info("id", "i1", DtdAttributeType.Cdata),
            Info("n", "tok", DtdAttributeType.NmToken),
            Info("ns", "a b c", DtdAttributeType.NmTokens),
            Info("en", "no", DtdAttributeType.Enumeration),
            Info("f", "fixed", DtdAttributeType.Cdata),
        };

        return new ValidationElement("e".AsMemory(), Origin, attributes);
    }

    private static ValidationAttributeInfo Info(string name, string value, DtdAttributeType type) =>
        new(name.AsMemory(), value.AsMemory(), type, isSpecified: true, Origin);
}
