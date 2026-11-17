namespace NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;

using System;
using System.Text;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Core;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Text.NameTable;

public class XmlParserAllocationTests
{
    private static string Document(int elements)
    {
        var builder = new StringBuilder("<r xmlns:p=\"urn:p\">");
        for (var i = 0; i < elements; i++)
        {
            builder.Append(
                "<item id=\"1\" p:v=\"x&amp;y\" xmlns:q=\"urn:q\">text &lt; more<!--c--><?pi d?><q:e/></item>\n"
            );
        }

        return builder.Append("</r>").ToString();
    }

    private static int Parse(string xml)
    {
        using var parser = new XmlParser(XmlReaderLimits.Default);
        var sink = new CountingStructSink(new SpanInterningNameTable(1000));
        parser.Feed(xml.AsMemory(), true);

        return parser.Pump(ref sink) == XmlReadStatus.Completed ? sink.Count : -1;
    }

    [Test]
    public async Task Pump_RepeatedNames_AllocateNothingPerElementAfterWarmUp()
    {
        var small = Document(20);
        var large = Document(2000);

        // The fixed part (parser, buffers, table) is the same for both, so only a per-element allocation shows up.
        var smallBytes = AllocationAssert.Measure(() => Parse(small), warmups: 30, repeats: 10);
        var largeBytes = AllocationAssert.Measure(() => Parse(large), warmups: 30, repeats: 10);

        await Assert.That(Parse(large)).IsGreaterThan(Parse(small));
        await Assert.That(largeBytes - smallBytes).IsLessThanOrEqualTo(0L);
    }
}
