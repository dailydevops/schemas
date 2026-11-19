namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Identity;

using System;
using System.Globalization;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Identity;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Identity.IdentityHarness;

/// <summary>Guards, reuse of one step for several documents, the bounds of the ID table, and the allocation behavior of the table.</summary>
public class IdentityStepLifecycleTests
{
    [Test]
    public async Task BeginDocument_WithoutContext_Throws()
    {
        var step = new DtdIdentityStep();

        await Assert.That(() => step.BeginDocument(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(-1)]
    [Arguments(3)]
    public async Task Text_WithAnUndefinedKind_Throws(int kind)
    {
        var step = new DtdIdentityStep();

        await Assert
            .That(() => step.Text("x".AsSpan(), (XmlTextKind)kind, new XmlPosition(1, 1, 0)))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Events_BeforeBeginDocument_AreIgnored()
    {
        var step = new DtdIdentityStep();
        var position = new XmlPosition(1, 1, 0);
        var attribute = new ValidationAttributeInfo(
            "id".AsMemory(),
            "x".AsMemory(),
            DtdAttributeType.Id,
            isSpecified: true,
            position
        );

        step.StartElement(new ValidationElement("e".AsMemory(), position, new[] { attribute }));
        step.EntityReference(
            new ValidationEntityReference("x".AsMemory(), position, DtdEntityOutcome.DeclaredInternal)
        );
        step.Text("x".AsSpan(), XmlTextKind.Text, position);
        step.Comment(position);
        step.ProcessingInstruction(position);
        step.EndElement(new ValidationElement("e".AsMemory(), position));
        step.EndDocument();

        await Assert.That(step).IsNotNull();
    }

    [Test]
    public async Task Text_BetweenTags_IsNotIdentityContent_AndReportsNothing()
    {
        var run = Run("<r id=\"a\">text &amp; more<a ref=\"a\"/></r>");

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task Step_ReusedForASecondDocument_StartsWithAnEmptyTableAndNoPendingReferences()
    {
        var step = new DtdIdentityStep();
        var declarations = Declare(Standard("e"));

        // The first document ends without EndDocument, as after a fatal error: its forward reference stays pending.
        var first = Collector();
        step.BeginDocument(Context(first, declarations));
        step.StartElement(Element("e", "id", "x", 1));
        step.StartElement(Element("e", "ref", "ghost", 2));

        var second = Collector();
        step.BeginDocument(Context(second, declarations));
        step.StartElement(Element("e", "id", "x", 1));
        step.EndDocument();

        await Assert.That(first.Count).IsEqualTo(0);
        await Assert.That(second.Count).IsEqualTo(0);
    }

    [Test]
    public async Task EndDocument_CalledTwice_ReportsEachUnmatchedReferenceOnce()
    {
        var step = new DtdIdentityStep();
        var issues = Collector();
        step.BeginDocument(Context(issues, Declare(Standard("e"))));
        step.StartElement(Element("e", "ref", "ghost", 1));

        step.EndDocument();
        step.EndDocument();

        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Limit_MaxDistinctNames_StopsTheRunAtTheFirstIdBeyondTheBound()
    {
        var limits = XmlReaderLimits.Default.WithMaxDistinctNames(2);
        var run = Run("<r id=\"a\"><a id=\"b\"/><b ~id=\"c\"/><a id=\"a\"/><a ref=\"zz\"/></r>", limits);

        await Assert.That(run.Collector.IsStopped).IsTrue();
        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(XmlErrorCodes.XML1111.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
    }

    [Test]
    public async Task Limit_MaxDistinctNames_ARepeatOfAKnownIdAtTheBound_IsStillADuplicate()
    {
        var limits = XmlReaderLimits.Default.WithMaxDistinctNames(2);
        var run = Run("<r id=\"a\"><a id=\"b\"/><b ~id=\"a\"/></r>", limits);

        await Assert.That(run.Collector.IsStopped).IsFalse();
        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5009.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
    }

    [Test]
    public async Task Limit_MaxNodeCount_BoundsTheForwardReferencesThatAreWaitingForTheirId()
    {
        var limits = XmlReaderLimits.Default.WithMaxNodeCount(2);
        var run = Run("<r><a ref=\"p\"/><a ref=\"q\"/><a ~ref=\"s\"/><a id=\"p\"/></r>", limits);

        await Assert.That(run.Collector.IsStopped).IsTrue();
        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(XmlErrorCodes.XML1108.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
    }

    [Test]
    public async Task Limit_MaxNodeCount_ReferencesToKnownIdsAreNotWaiting_AndDoNotCount()
    {
        var limits = XmlReaderLimits.Default.WithMaxNodeCount(1);
        var run = Run("<r id=\"p\"><a ref=\"p\"/><a ref=\"p\"/><a ref=\"p\"/><a ref=\"zz\"/></r>", limits);

        await Assert.That(run.Collector.IsStopped).IsFalse();
        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5012.Code);
    }

    // Unmeasured runs before the measured ones (a one-time allocation hook fires on the first measured run), and the
    // measured runs of which the smallest counts: one-time allocations land in one run, a per-value one in all.
    private const int WarmupRuns = 5;
    private const int MeasuredRuns = 5;

    // Keeps the deliberate allocations of the tests observable to the allocation counter.
    private static object? s_retained;

    // Every element carries an ID and a reference to an earlier one.
    private static ValidationElement[] Elements(int count)
    {
        var elements = new ValidationElement[count];
        for (var n = 0; n < count; n++)
        {
            elements[n] = Element(
                "e",
                "id",
                "id" + n.ToString(CultureInfo.InvariantCulture),
                n + 1,
                n > 0 ? "id" + (n / 2).ToString(CultureInfo.InvariantCulture) : null
            );
        }

        return elements;
    }

    // What a run of the reused step allocates (smallest of the measured runs); `disturb` receives the zero-based run index
    // and runs inside the measured window of every run.
    private static long Allocated(Action<int>? disturb = null)
    {
        var declarations = Declare(Standard("e"));
        var elements = Elements(20_000);
        var step = new DtdIdentityStep();
        var run = 0;

        return AllocationAssert.Measure(
            () =>
            {
                var issues = Collector();
                var context = Context(issues, declarations);
                step.BeginDocument(context);
                foreach (var element in elements)
                {
                    step.StartElement(in element);
                    step.EndElement(in element);
                }

                step.EndDocument();
                disturb?.Invoke(run++);
                if (issues.Count != 0)
                {
                    throw new InvalidOperationException("The measured document must be valid.");
                }
            },
            WarmupRuns,
            MeasuredRuns
        );
    }

    [Test]
    public async Task Table_AfterTheFirstDocument_StoresIdsAndReferencesWithoutAllocatingPerValue()
    {
        // The values are built before the measurement, so what the second run allocates is the work of the step itself:
        // with a string or an entry object per ID it would be several hundred kilobytes; the table keeps the names in
        // reused buffers and allocates (almost) nothing.
        var allocated = Allocated();

        await Assert.That(allocated).IsLessThan(4 * 1024);
    }

    [Test]
    public async Task Table_AllocationMeasurement_ToleratesAOneTimeAllocation()
    {
        // A one-time allocation (a lazily created table, an allocation context refill, work of another test in the
        // process) lands in the first measured run only; the measurement must not read it as a cost of the step.
        var allocated = Allocated(run => s_retained = run == WarmupRuns ? new byte[8000] : null);

        await Assert.That(allocated).IsLessThan(4 * 1024);
    }

    [Test]
    public async Task Table_AllocationMeasurement_StillSeesAnAllocationPerValue()
    {
        // The mutation proof: an allocation of 24 bytes per value in every run must still exceed the limit.
        var allocated = Allocated(_ => s_retained = new byte[20_000 * 24]);

        await Assert.That(allocated).IsGreaterThanOrEqualTo(4 * 1024);
    }

    private static ValidationElement Element(
        string name,
        string attribute,
        string value,
        int line,
        string? reference = null
    )
    {
        var position = new XmlPosition(line, 4, line * 10);
        var info = new ValidationAttributeInfo(
            attribute.AsMemory(),
            value.AsMemory(),
            string.Equals(attribute, "id", StringComparison.Ordinal) ? DtdAttributeType.Id : DtdAttributeType.IdRef,
            isSpecified: true,
            position
        );
        ValidationAttributeInfo[] attributes = reference is null
            ? [info]
            :
            [
                info,
                new ValidationAttributeInfo(
                    "ref".AsMemory(),
                    reference.AsMemory(),
                    DtdAttributeType.IdRef,
                    isSpecified: true,
                    position
                ),
            ];

        return new ValidationElement(name.AsMemory(), new XmlPosition(line, 1, line * 10), attributes);
    }
}
