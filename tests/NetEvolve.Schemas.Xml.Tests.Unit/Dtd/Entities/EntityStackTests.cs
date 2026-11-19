namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entities;

using System;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Reader.EntityInput;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>The entity input stack (design #108, AP20): frames over replacement text, outer positions, and state restore.</summary>
public class EntityStackTests
{
    [Test]
    public async Task EntityStack_FrameConsumedIsRelativeToFrameSpan()
    {
        // The reference to f stands in the middle of the frame of e, so the frame of e must resume right after it.
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"1&f;3<c/>5\"><!ENTITY f \"2\">]><a>&e;&e;</a>");

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|'123'|<c|/c|'5123'|<c|/c|'5'|/a");
    }

    [Test]
    public async Task EntityStack_PopRestoresTokenizerState()
    {
        const string xml =
            "<!DOCTYPE a [<!ENTITY v \"va\"><!ENTITY m \"<b/>t\">]><a x=\"&v;\" y=\"&v;\">&m;<c/>&m;</a>";
        var run = EntityHarness.Read(xml);

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|@x=va|@y=va|<b|/b|'t'|<c|/c|<b|/b|'t'|/a");
        await Assert.That(run.ElementPositions.Count).IsEqualTo(4);
    }

    [Test]
    public async Task EntityStack_Position_IsOuterReference()
    {
        const string xml =
            "<!DOCTYPE a [<!ENTITY v \"va\"><!ENTITY m \"<b/>t\">]><a x=\"&v;\" y=\"&v;\">&m;<c/>&m;</a>";
        var run = EntityHarness.Read(xml);
        var first = xml.IndexOf("&m;", StringComparison.Ordinal);
        var second = xml.IndexOf("&m;", first + 1, StringComparison.Ordinal);
        var c = xml.IndexOf("<c/>", StringComparison.Ordinal);
        var a = xml.IndexOf("<a ", StringComparison.Ordinal);

        await Assert.That(run.ElementPositions[0]).IsEqualTo(new XmlPosition(1, a + 1, a));
        await Assert.That(run.ElementPositions[1]).IsEqualTo(new XmlPosition(1, first + 1, first));
        await Assert.That(run.ElementPositions[2]).IsEqualTo(new XmlPosition(1, c + 1, c));
        await Assert.That(run.ElementPositions[3]).IsEqualTo(new XmlPosition(1, second + 1, second));
    }

    [Test]
    public async Task EntityStack_ErrorInsideAFrame_CarriesTheOuterReferencePosition()
    {
        const string xml = "<!DOCTYPE a [<!ENTITY e1 \"x&e2;\"><!ENTITY e2 \"<b>\">]><a>\n  &e1;</a>";
        var run = EntityHarness.Read(xml);
        var reference = xml.IndexOf("&e1;", StringComparison.Ordinal);

        await Assert.That(run.Code).IsEqualTo("XML0001");
        await Assert.That(run.ErrorPosition).IsEqualTo(new XmlPosition(2, 3, reference));
    }

    [Test]
    public async Task EntityStack_IncompleteConstructAtFrameEnd_RaisesXml0001()
    {
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"<b att='1\">]><a>&e;'/></a>");

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML0001");
    }

    [Test]
    public async Task EntityStack_DepthLimit_RaisesXml1110()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityDepth(2);
        var run = EntityHarness.Read(
            "<!DOCTYPE a [<!ENTITY e1 \"x\"><!ENTITY e2 \"&e1;\"><!ENTITY e3 \"&e2;\">]><a>&e3;</a>",
            limits
        );

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML1110");
    }

    [Test]
    public async Task EntityStack_NodeAndTextLimits_ApplyToExpandedContent()
    {
        var limits = XmlReaderLimits.Default.WithMaxNodeCount(3);
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"<b/><b/><b/>\">]><a>&e;</a>", limits);

        await Assert.That(run.Code).IsEqualTo("XML1108");
    }

    private static ReaderRun RunSplit(string xml, int split)
    {
        var limits = XmlReaderLimits.Default;
        var handler = new DtdDoctypeHandler(limits);

        return ReaderHarness.Run(xml, split: split, doctype: handler, stack: new XmlEntityInputStack(limits, handler));
    }

    [Test]
    [Arguments(
        "<!DOCTYPE a [<!ENTITY v \"v&w;\"><!ENTITY w \"w\"><!ENTITY m \"<b x='&v;'>t&v;</b>\">]>"
            + "<a x=\"&v;\" y='[&w;]'>pre&m;&amp;&#65;<!-- c -->&v;<![CDATA[&m;]]></a>"
    )]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"<b>\">]><a>&e;</a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"&e;\">]><a x=\"&e;\"/>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"x\">]><a>&f;</a>")]
    public async Task EntityStack_SplitAtEveryOffset_GivesTheSameEventsAndDiagnostic(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var expected = RunSplit(xml, -1);

        for (var split = 0; split <= xml.Length; split++)
        {
            await Assert.That(RunSplit(xml, split)).IsEqualTo(expected);
        }
    }

    // Unmeasured runs before the measured ones; a one-time allocation hook fires on the first measured run.
    private const int WarmupRuns = 2;

    // Keeps the deliberate allocations of the tests observable to the allocation counter.
    private static object? s_retained;

    // The characters and the references of a document with `count` references; everything else is constant.
    // `disturb` receives the zero-based run index and runs inside the measured window of every run.
    private static long Allocated(int count, Action<int>? disturb = null)
    {
        var xml =
            "<!DOCTYPE a [<!ENTITY e \"<b/>text\"><!ENTITY v \"value\">]><a x=\""
            + new StringBuilder().Insert(0, "&v;", count)
            + "\">"
            + new StringBuilder().Insert(0, "&e;", count)
            + "</a>";
        var options = XmlReaderOptions.Default.WithDtdProcessing(true);
        var measured = 0L;

        for (var run = 0; run <= WarmupRuns; run++)
        {
            var sink = new CountingXmlSink(new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames));
            using var reader = XmlPipelineReader.Create(xml.AsSpan(), options);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var status = reader.Pump(ref sink);
            disturb?.Invoke(run);
            measured = GC.GetAllocatedBytesForCurrentThread() - before;

            if (status != XmlReadStatus.Completed)
            {
                throw new InvalidOperationException("The document must be read to the end.");
            }
        }

        return measured;
    }

    [Test]
    public async Task EntityStack_ExpansionAllocatesNothingPerReference()
    {
        var few = Allocated(10);
        var many = Allocated(1000);

        await Assert.That(many - few).IsLessThanOrEqualTo(0);
    }

    [Test]
    public async Task EntityStack_ExpansionAllocationMeasurement_ToleratesAOneTimeAllocation()
    {
        // A one-time allocation (tiered JIT, a lazily created static, an allocation context refill) lands in the first
        // measured run of the large document only; the measurement must not read it as a per-reference cost.
        var few = Allocated(10);
        var many = Allocated(1000, run => s_retained = run == WarmupRuns ? new byte[4096] : null);

        await Assert.That(many - few).IsLessThanOrEqualTo(0);
    }

    [Test]
    public async Task EntityStack_ExpansionAllocationMeasurement_StillSeesAnAllocationPerReference()
    {
        // The mutation proof: one 24-byte allocation per reference in every run must still make the slope positive.
        var few = Allocated(10, _ => s_retained = new byte[10 * 24]);
        var many = Allocated(1000, _ => s_retained = new byte[1000 * 24]);

        await Assert.That(many - few).IsGreaterThan(0);
    }
}
