namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Subset;

using System.Globalization;
using System.Linq;
using System.Text;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Subset;
using NetEvolve.Schemas.Xml.Tests.Unit.Dtd.External.Loader;

/// <summary>
/// Design #122 D9 rule 5: an external entity is read through the frame in chunks and never buffered completely. The
/// declaration parser keeps one window with the unit that it parses; the unit is bounded by <c>MaxDoctypeLength</c>.
/// </summary>
public class ExternalSubsetStreamingTests
{
    private const string EntityUri = "http://example.org/doc/pe.ent";

    private static string Entities(int count) =>
        string.Concat(
            Enumerable
                .Range(0, count)
                .Select(i => string.Create(CultureInfo.InvariantCulture, $"<!ENTITY e{i} 'v{i}'>\n"))
        );

    private static (string Model, XmlErrorCode? Error) Parse(
        string internalSubset,
        IResourceResolver resolver,
        XmlReaderLimits? limits = null,
        string systemId = "ext.dtd"
    ) => SubsetHarness.Parse(internalSubset, resolver, systemId, limits);

    [Test]
    public async Task ManySmallDeclarations_FarBeyondTheUnitBound_StillParse()
    {
        var limits = XmlReaderLimits.Default.WithMaxDoctypeLength(64);
        var subset = Entities(3000);

        var (model, error) = Parse(
            string.Empty,
            new ReadCountingResolver(maxRead: 7).Add(SubsetHarness.SubsetUri, Encoding.UTF8.GetBytes(subset)),
            limits
        );

        await Assert.That(subset.Length).IsGreaterThan(64 * 100);
        await Assert.That(error).IsNull();
        await Assert.That(model.Split('|').Length).IsEqualTo(3000);
    }

    [Test]
    public async Task OneUnitFarBeyondTheBound_RaisesXml1115()
    {
        var limits = XmlReaderLimits.Default.WithMaxDoctypeLength(64);
        var unit = "<!ENTITY e '" + new string('x', 400) + "'>";

        var (_, error) = Parse(
            string.Empty,
            new ReadCountingResolver(maxRead: 1).Add(SubsetHarness.SubsetUri, Encoding.UTF8.GetBytes(unit)),
            limits
        );

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML1115);
    }

    [Test]
    public async Task OneUnitWithinTheBound_Parses()
    {
        var limits = XmlReaderLimits.Default.WithMaxDoctypeLength(64);

        var (model, error) = Parse(
            string.Empty,
            new ReadCountingResolver(maxRead: 1).Add(
                SubsetHarness.SubsetUri,
                Encoding.UTF8.GetBytes("<!ENTITY e '" + new string('x', 30) + "'>")
            ),
            limits
        );

        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("GE:e=\"" + new string('x', 30) + "\"");
    }

    [Test]
    public async Task ExternalSubset_ThatFailsEarly_IsNotReadToTheEnd()
    {
        var content = "<!ELEMENT a>" + Entities(100_000);
        var resolver = new ReadCountingResolver().Add(SubsetHarness.SubsetUri, Encoding.UTF8.GetBytes(content));

        var (_, error) = Parse(string.Empty, resolver);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
        await Assert.That(content.Length).IsGreaterThan(1_000_000);
        await Assert.That(resolver.Streams[0].BytesRead).IsLessThan(20_000);
    }

    [Test]
    public async Task ExternalParameterEntity_BetweenDeclarations_IsStreamedToo()
    {
        var content = "<!ELEMENT a>" + Entities(100_000);
        var resolver = new ReadCountingResolver().Add(EntityUri, Encoding.UTF8.GetBytes(content));

        var (_, error) = Parse("<!ENTITY % ext SYSTEM \"pe.ent\"> %ext;", resolver, systemId: string.Empty);

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0011);
        await Assert.That(resolver.Streams[0].BytesRead).IsLessThan(20_000);
    }

    [Test]
    public async Task IgnoredSection_FarBeyondTheBound_IsSkippedWithoutCarryingIt()
    {
        var limits = XmlReaderLimits.Default.WithMaxDoctypeLength(64);
        var filler = string.Concat(Enumerable.Repeat("ignored <!ELEMENT x EMPTY> <![ INCLUDE [ nested ]]> ", 2000));
        var subset = "<![ IGNORE [ " + filler + " ]]><!ELEMENT a EMPTY>";

        var (model, error) = Parse(
            string.Empty,
            new ReadCountingResolver(maxRead: 5).Add(SubsetHarness.SubsetUri, Encoding.UTF8.GetBytes(subset)),
            limits
        );

        await Assert.That(subset.Length).IsGreaterThan(64 * 1000);
        await Assert.That(error).IsNull();
        await Assert.That(model).IsEqualTo("E:a:EMPTY");
    }

    [Test]
    public async Task ExternalEntityInsideADeclaration_BeyondTheBound_RaisesXml1115()
    {
        var limits = XmlReaderLimits.Default.WithMaxDoctypeLength(64);
        var big = string.Concat(Enumerable.Repeat("|x", 500));
        var small = string.Concat(Enumerable.Repeat("|x", 5));

        var (_, beyond) = Parse(
            "<!ENTITY % ext SYSTEM \"pe.ent\">",
            new ReadCountingResolver(maxRead: 1)
                .Add(EntityUri, Encoding.UTF8.GetBytes(big))
                .Add(SubsetHarness.SubsetUri, Encoding.UTF8.GetBytes("<!ELEMENT a (x%ext;)>")),
            limits
        );
        var (model, within) = Parse(
            "<!ENTITY % ext SYSTEM \"pe.ent\">",
            new ReadCountingResolver(maxRead: 1)
                .Add(EntityUri, Encoding.UTF8.GetBytes(small))
                .Add(SubsetHarness.SubsetUri, Encoding.UTF8.GetBytes("<!ELEMENT a (x%ext;)>")),
            limits
        );

        await Assert.That(beyond).IsSameReferenceAs(XmlErrorCodes.XML1115);
        await Assert.That(within).IsNull();
        await Assert.That(model).Contains("E:a:(x|x|x|x|x|x)");
    }

    [Test]
    public async Task InvalidByte_AfterValidDeclarations_RaisesTheDecodingError()
    {
        var content = Encoding.UTF8.GetBytes("<!ELEMENT a EMPTY>").Concat(new byte[] { 0xFF }).ToArray();

        var (_, error) = Parse(
            string.Empty,
            new ReadCountingResolver(maxRead: 3).Add(SubsetHarness.SubsetUri, content)
        );

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML4003);
    }

    // A character that is not an XML Char is valid in the encoding, so it is XML0002 and not a decoding error (#712); the
    // declaration parser does not check the characters of a comment, an instruction, or a literal, so the decoder does.
    [Test]
    [Arguments("<!ELEMENT a EMPTY>\u0001", 1)]
    [Arguments("<!ELEMENT a EMPTY>\u0001", 3)]
    [Arguments("<!-- c\u0001 -->", 1)]
    [Arguments("<?p c\u0001?>", 3)]
    [Arguments("<!ENTITY e 'a\u0001b'>", 1)]
    [Arguments("<!ELEMENT a\u0001 EMPTY>", 3)]
    [Arguments("<!ELEMENT a EMPTY> \uFFFE ", 1)]
    public async Task NonXmlCharacter_IsXml0002_NotAcceptedAndNotADecodingError(string text, int maxRead)
    {
        var (_, error) = Parse(
            string.Empty,
            new ReadCountingResolver(maxRead).Add(SubsetHarness.SubsetUri, Encoding.UTF8.GetBytes(text))
        );

        await Assert.That(error).IsSameReferenceAs(XmlErrorCodes.XML0002);
    }

    // Every construct of the external subset, long enough that the chunks of any size cut through every one of them.
    private static string Mixed()
    {
        var block = new StringBuilder();
        block.Append("<!-- a comment with a > and a ]]> and <![ inside -->\n");
        block.Append("<?target some data with > and ]]> ?>\n");
        block.Append("<!ENTITY % kind 'INCLUDE'>\n");
        block.Append("<![ %kind; [ <!ELEMENT inc (#PCDATA|a|b)*> <![ IGNORE [ <![ INCLUDE [ deep ]]> ]]> ]]>\n");
        block.Append("<![ IGNORE [ <!ELEMENT ign EMPTY> <![ INCLUDE [ x ]]> ]]>\n");
        block.Append("<!ENTITY % body \"a, b?, (c | d)+\">\n");
        block.Append("<!ELEMENT seq (%body;)>\n");
        block.Append("<!ATTLIST seq id ID #REQUIRED kind (x|y|z) 'x' text CDATA \"a > b ]]> 'c' ü€ 𝄞\">\n");
        block.Append("<!ENTITY lit \"a > b ]]> %kind; 'q' ü€ 𝄞\">\n");
        block.Append("<!NOTATION n PUBLIC '-//X//Y//EN' 'http://example.org/n'>\n");
        return block.ToString();
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(5)]
    [Arguments(7)]
    [Arguments(4095)]
    [Arguments(4096)]
    [Arguments(4097)]
    public async Task Chunking_DoesNotChangeTheModel(int maxRead)
    {
        var subset = string.Concat(Enumerable.Repeat(Mixed(), 1)) + Entities(400);
        var whole = Parse(string.Empty, SubsetHarness.Serving(subset));
        var chunked = Parse(
            string.Empty,
            new ReadCountingResolver(maxRead).Add(SubsetHarness.SubsetUri, Encoding.UTF8.GetBytes(subset))
        );

        await Assert.That(whole.Error).IsNull();
        await Assert.That(whole.Model).Contains("E:seq:(a,b?,(c|d)+)");
        await Assert.That(whole.Model).Contains("E:inc:(#PCDATA|a|b)*");
        await Assert.That(whole.Model).DoesNotContain("E:ign");
        await Assert.That(chunked).IsEqualTo(whole);
    }

    [Test]
    [Arguments(4090)]
    [Arguments(4094)]
    [Arguments(4096)]
    [Arguments(8190)]
    public async Task DeclarationsThatStraddleTheChunkBoundary_GiveTheSameModel(int padding)
    {
        // The padding moves the declarations across the 4096 characters that the frame reads at a time.
        var subset = "<!--" + new string('p', padding) + "-->" + Mixed() + Entities(200);
        var whole = Parse(string.Empty, SubsetHarness.Serving(subset));
        var small = Parse(
            string.Empty,
            new ReadCountingResolver(maxRead: 13).Add(SubsetHarness.SubsetUri, Encoding.UTF8.GetBytes(subset))
        );

        await Assert.That(whole.Error).IsNull();
        await Assert.That(whole.Model).Contains("GE:e199=\"v199\"");
        await Assert.That(small).IsEqualTo(whole);
    }

    [Test]
    public async Task Text_TryAppend_DeliversTheEntityInChunks_AndTheEndIsReported()
    {
        var content = string.Concat(Enumerable.Repeat("<!ELEMENT a EMPTY>", 600));
        var reader = new ExternalSubsetReader(
            LoaderHarness.Open(
                new ReadCountingResolver(maxRead: 1000).Add(SubsetHarness.SubsetUri, Encoding.UTF8.GetBytes(content))
            ),
            XmlReaderLimits.Default
        );

        var opened = reader.TryOpen(
            ResourceKind.ExternalSubset,
            "doc",
            null,
            "ext.dtd",
            SubsetHarness.DocumentUri,
            out var entity,
            out _
        );
        using var owned = entity;
        var builder = new StringBuilder();
        var chunks = 0;
        while (entity!.TryAppend(builder))
        {
            chunks++;
        }

        await Assert.That(opened).IsTrue();
        await Assert.That(chunks).IsGreaterThan(1);
        await Assert.That(builder.ToString()).IsEqualTo(content);
        await Assert.That(entity.IsEnd).IsTrue();
        await Assert.That(entity.Error).IsNull();
        await Assert.That(entity.TryAppend(builder)).IsFalse();
    }

    [Test]
    public async Task Text_TryReadAll_ReadsTheRest_AndStopsAtTheBound()
    {
        var reader = new ExternalSubsetReader(
            LoaderHarness.Open(SubsetHarness.Serving("0123456789")),
            XmlReaderLimits.Default
        );

        _ = reader.TryOpen(
            ResourceKind.ExternalSubset,
            "doc",
            null,
            "ext.dtd",
            SubsetHarness.DocumentUri,
            out var all,
            out _
        );
        var readAll = all!.TryReadAll(10, out var text, out var error);
        all.Dispose();

        _ = reader.TryOpen(
            ResourceKind.ExternalSubset,
            "doc",
            null,
            "ext.dtd",
            SubsetHarness.DocumentUri,
            out var cut,
            out _
        );
        using var ownedCut = cut;
        var readCut = cut!.TryReadAll(9, out var none, out var cutError);

        await Assert.That(readAll).IsTrue();
        await Assert.That(text).IsEqualTo("0123456789");
        await Assert.That(error).IsNull();
        await Assert.That(readCut).IsFalse();
        await Assert.That(none).IsNull();
        await Assert.That(cutError).IsSameReferenceAs(XmlErrorCodes.XML1115);
    }

    [Test]
    public async Task Text_ReadingFailure_IsReportedThroughTheError()
    {
        var content = Encoding.UTF8.GetBytes("<!ELEMENT a EMPTY>").Concat(new byte[] { 0xFF }).ToArray();
        var reader = new ExternalSubsetReader(
            LoaderHarness.Open(new ReadCountingResolver().Add(SubsetHarness.SubsetUri, content)),
            XmlReaderLimits.Default
        );

        _ = reader.TryOpen(
            ResourceKind.ExternalSubset,
            "doc",
            null,
            "ext.dtd",
            SubsetHarness.DocumentUri,
            out var entity,
            out _
        );
        using var owned = entity;
        var builder = new StringBuilder();
        while (entity!.TryAppend(builder)) { }

        await Assert.That(entity.IsEnd).IsTrue();
        await Assert.That(entity.Error).IsSameReferenceAs(XmlErrorCodes.XML4003);
        await Assert.That(builder.ToString()).IsEqualTo("<!ELEMENT a EMPTY>");
    }

    [Test]
    public async Task Text_Guards_RejectNullBuilderAndNegativeBound()
    {
        var reader = new ExternalSubsetReader(
            LoaderHarness.Open(SubsetHarness.Serving("<!ELEMENT a EMPTY>")),
            XmlReaderLimits.Default
        );
        _ = reader.TryOpen(
            ResourceKind.ExternalSubset,
            "doc",
            null,
            "ext.dtd",
            SubsetHarness.DocumentUri,
            out var entity,
            out _
        );
        using var owned = entity;

        await Assert.That(() => entity!.TryAppend(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => entity!.TryReadAll(-1, out _, out _)).Throws<ArgumentOutOfRangeException>();
    }
}
