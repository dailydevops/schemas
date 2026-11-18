namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entities;

using System;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>The expansion budgets (design #108, AP17 and AP20): characters (XML1109) and depth (XML1110).</summary>
public class EntityLimitTests
{
    // Each level references the level below it ten times: level n expands to 10^n characters.
    private static string ExponentialDocument(int levels)
    {
        var builder = new StringBuilder("<!DOCTYPE a [<!ENTITY l0 \"xxxxxxxxxx\">");
        for (var i = 1; i < levels; i++)
        {
            builder.Append("<!ENTITY l").Append(i).Append(" \"");
            for (var j = 0; j < 10; j++)
            {
                builder.Append("&l").Append(i - 1).Append(';');
            }

            builder.Append("\">");
        }

        return builder.Append("]><a>&l").Append(levels - 1).Append(";</a>").ToString();
    }

    // One entity of `length` characters referenced `count` times: the expansion is count * length characters.
    private static string WideDocument(int length, int count, bool inAttribute = false)
    {
        var builder = new StringBuilder("<!DOCTYPE a [<!ENTITY e \"").Append('x', length).Append("\">]>");
        builder.Append(inAttribute ? "<a x=\"" : "<a>");
        for (var i = 0; i < count; i++)
        {
            builder.Append("&e;");
        }

        return builder.Append(inAttribute ? "\"/>" : "</a>").ToString();
    }

    [Test]
    public async Task Exponential_NestedExpansion_EndsInTheExpansionLimit()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(100_000);
        var run = EntityHarness.Read(ExponentialDocument(10), limits);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML1109");
    }

    [Test]
    public async Task Exponential_NestedExpansion_EndsInTheExpansionLimitWithDefaultLimits()
    {
        using var reader = XmlPipelineReader.Create(
            ExponentialDocument(10).AsSpan(),
            XmlReaderOptions.Default.WithDtdProcessing(true)
        );
        var sink = new CountingSink(new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames));

        var status = reader.Pump(ref sink);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(reader.Diagnostic?.Code.Alias).IsEqualTo("XML1109");
        await Assert.That(sink.Characters).IsLessThanOrEqualTo(XmlReaderLimits.Default.MaxEntityExpansionCharacters);
    }

    [Test]
    public async Task Quadratic_WideExpansionInContent_EndsInTheExpansionLimit()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(10_000);
        var run = EntityHarness.Read(WideDocument(200, 100), limits);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML1109");
    }

    [Test]
    public async Task Quadratic_WideExpansionInAnAttributeValue_EndsInTheExpansionLimit()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(10_000);
        var run = EntityHarness.Read(WideDocument(200, 100, inAttribute: true), limits);

        await Assert.That(run.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(run.Code).IsEqualTo("XML1109");
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task ExpansionLimit_ExactlyAtTheLimitPasses_OneCharacterMoreFails(bool inAttribute)
    {
        var xml = WideDocument(10, 3, inAttribute);
        var atLimit = EntityHarness.Read(xml, XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(30));
        var overLimit = EntityHarness.Read(xml, XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(29));

        await Assert.That(atLimit.Code).IsNull();
        await Assert.That(atLimit.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(overLimit.Code).IsEqualTo("XML1109");
    }

    [Test]
    public async Task ExpansionLimit_ChargesTheReplacementTextOfEveryNestedFrame()
    {
        // The text of c is 6 characters and holds two references to b, a frame of 3 characters each: 6 + 3 + 3 = 12.
        const string xml = "<!DOCTYPE a [<!ENTITY b \"abc\"><!ENTITY c \"&b;&b;\">]><a>&c;</a>";
        var atLimit = EntityHarness.Read(xml, XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(12));
        var overLimit = EntityHarness.Read(xml, XmlReaderLimits.Default.WithMaxEntityExpansionCharacters(11));

        await Assert.That(atLimit.Events).IsEqualTo("<a|'abcabc'|/a");
        await Assert.That(overLimit.Code).IsEqualTo("XML1109");
    }

    private static string Chain(int length)
    {
        var builder = new StringBuilder("<!DOCTYPE a [<!ENTITY e1 \"x\">");
        for (var i = 2; i <= length; i++)
        {
            builder.Append("<!ENTITY e").Append(i).Append(" \"&e").Append(i - 1).Append(";\">");
        }

        return builder.Append("]><a>&e").Append(length).Append(";</a>").ToString();
    }

    [Test]
    public async Task Depth_ChainAtTheDefaultDepthPasses_OneFrameDeeperRaisesXml1110()
    {
        var atLimit = EntityHarness.Read(Chain(XmlReaderLimits.Default.MaxEntityDepth));
        var overLimit = EntityHarness.Read(Chain(XmlReaderLimits.Default.MaxEntityDepth + 1));

        await Assert.That(atLimit.Code).IsNull();
        await Assert.That(atLimit.Events).IsEqualTo("<a|'x'|/a");
        await Assert.That(overLimit.Status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(overLimit.Code).IsEqualTo("XML1110");
    }

    [Test]
    public async Task Depth_InAnAttributeValue_RaisesXml1110PastTheLimit()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityDepth(3);
        var xml = "<!DOCTYPE a [<!ENTITY e1 \"x\"><!ENTITY e2 \"&e1;\"><!ENTITY e3 \"&e2;\"><!ENTITY e4 \"&e3;\">]>";
        var atLimit = EntityHarness.Read(xml + "<a x=\"&e3;\"/>", limits);
        var overLimit = EntityHarness.Read(xml + "<a x=\"&e4;\"/>", limits);

        await Assert.That(atLimit.Events).IsEqualTo("<a|@x=x|/a");
        await Assert.That(overLimit.Code).IsEqualTo("XML1110");
    }

    [Test]
    public async Task Depth_FramesAreReleased_SoSiblingReferencesDoNotAccumulate()
    {
        var limits = XmlReaderLimits.Default.WithMaxEntityDepth(1);
        var run = EntityHarness.Read("<!DOCTYPE a [<!ENTITY e \"x\">]><a>&e;&e;&e;</a>", limits);

        await Assert.That(run.Code).IsNull();
        await Assert.That(run.Events).IsEqualTo("<a|'xxx'|/a");
    }

    private sealed class CountingSink(XmlNameTable names) : IXmlSink
    {
        public XmlNameTable Names { get; } = names;

        public long Characters { get; private set; }

        public void StartDocument(in XmlDeclaration declaration) { }

        public void EndDocument() { }

        public void StartElement(XmlName name, XmlPosition position) { }

        public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri) { }

        public void Attribute(XmlName name, ReadOnlySpan<char> value) { }

        public void EndAttributes() { }

        public void EndElement(XmlName name) { }

        public void Text(ReadOnlySpan<char> text, XmlTextKind kind) => Characters += text.Length;

        public void Comment(ReadOnlySpan<char> text) { }

        public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) { }

        public void Doctype(in XmlDoctypeInfo doctype) { }
    }
}
