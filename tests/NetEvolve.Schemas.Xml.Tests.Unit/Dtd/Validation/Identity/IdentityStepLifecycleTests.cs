namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Identity;

using System;
using System.Globalization;
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

    [Test]
    public async Task Table_AfterTheFirstDocument_StoresIdsAndReferencesWithoutAllocatingPerValue()
    {
        // Every element carries an ID and a reference to an earlier one. The values are built before the measurement, so
        // what the second run allocates is the work of the step itself: with a string or an entry object per ID it would be
        // several hundred kilobytes; the table keeps the names in reused buffers and allocates (almost) nothing.
        const int count = 20_000;
        var declarations = Declare(Standard("e"));
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

        var step = new DtdIdentityStep();
        _ = RunDocument(step, declarations, elements);
        var allocated = RunDocument(step, declarations, elements);

        await Assert.That(allocated).IsLessThan(4 * 1024);
    }

    // No await between the two reads: the counter is per thread.
    private static long RunDocument(DtdIdentityStep step, DtdDeclarations declarations, ValidationElement[] elements)
    {
        var issues = Collector();
        var context = Context(issues, declarations);
        var before = GC.GetAllocatedBytesForCurrentThread();
        step.BeginDocument(context);
        foreach (var element in elements)
        {
            step.StartElement(in element);
            step.EndElement(in element);
        }

        step.EndDocument();
        var after = GC.GetAllocatedBytesForCurrentThread();
        if (issues.Count != 0)
        {
            throw new InvalidOperationException("The measured document must be valid.");
        }

        return after - before;
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
