namespace NetEvolve.Schemas.Xml.Tests.Unit.Readme;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Readme;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Loading;

// The regions between the sample markers are the code blocks of src/NetEvolve.Schemas.Xml/README.md, character for character.
public class XmlReadmeTests
{
    private const string Readme = "src/NetEvolve.Schemas.Xml/README.md";
    private const string Samples = "tests/NetEvolve.Schemas.Xml.Tests.Unit/Readme/XmlReadmeTests.cs";
    private const string DtdSamples = "tests/NetEvolve.Schemas.Xml.Tests.Unit/Dtd/Entry/DtdEntryReadmeTests.cs";
    private const string LegacyDtdSamples = "tests/NetEvolve.Schemas.Tests.LegacyCompiler/Dtd/DtdLegacyTests.cs";

    [Test]
    public async Task SpanSample_LoadsTheDocumentWithPositions()
    {
        // <sample>
        var tree = XmlDocumentLoader.Load("<order id=\"7\">\n  <item>tea</item>\n</order>".AsSpan());

        var order = tree.Document.FirstChild;
        var name = tree.Names.GetString(order.Name.Local);
        var item = order.FirstChild.NextSibling;
        var text = item.StringValue;
        var position = tree.GetPosition(item.Index);
        // </sample>

        await Assert.That(name).IsEqualTo("order");
        await Assert.That(text).IsEqualTo("tea");
        await Assert.That(position.Line).IsEqualTo(2);
        await Assert.That(position.Column).IsEqualTo(3);
    }

    [Test]
    public async Task MemorySample_LoadsTheDocumentFromBytes()
    {
        // <sample>
        ReadOnlyMemory<byte> utf8 = Encoding.UTF8.GetBytes("<order id=\"7\"><item>tea</item></order>");

        var tree = XmlDocumentLoader.LoadFromMemory(utf8);
        var text = tree.Document.FirstChild.FirstChild.StringValue;
        // </sample>

        await Assert.That(text).IsEqualTo("tea");
    }

    // <sample>
    internal sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    // </sample>

    [Test]
    public async Task SequenceSample_LoadsADocumentThatSpansTwoSegments()
    {
        // <sample>
        var first = new Segment("<order id=\"7\"><it"u8.ToArray());
        var last = first.Append("em>tea</item></order>"u8.ToArray());
        var sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);

        var tree = XmlDocumentLoader.Load(sequence);
        var text = tree.Document.FirstChild.FirstChild.StringValue;
        // </sample>

        await Assert.That(sequence.IsSingleSegment).IsFalse();
        await Assert.That(text).IsEqualTo("tea");
    }

    [Test]
    public async Task StreamSample_LoadsTheDocumentAsynchronously(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // <sample>
        await using var stream = new MemoryStream("<order id=\"7\"><item>tea</item></order>"u8.ToArray());

        var tree = await XmlDocumentLoader.LoadAsync(stream, cancellationToken: cancellationToken);
        var text = tree.Document.FirstChild.FirstChild.StringValue;
        // </sample>

        await Assert.That(text).IsEqualTo("tea");
        await Assert.That(stream.CanRead).IsTrue();
    }

    [Test]
    public async Task DoctypeSample_IsRejectedByDefault()
    {
        // <sample>
        string? code = null;
        try
        {
            XmlDocumentLoader.Load("<!DOCTYPE order><order/>".AsSpan());
        }
        catch (XmlParseException exception)
        {
            code = exception.Diagnostic?.Code.Alias;
        }
        // </sample>

        await Assert.That(code).IsEqualTo("XML2001");
    }

    [Test]
    public async Task DtdProcessingSample_ExpandsAnInternalEntity()
    {
        // <sample>
        var options = XmlReaderOptions.Default.WithDtdProcessing(true);
        var tree = XmlDocumentLoader.Load(
            "<!DOCTYPE note [<!ENTITY who \"Ada\">]><note>&who;</note>".AsSpan(),
            options
        );

        var text = tree.Document.FirstChild.StringValue;
        // </sample>

        await Assert.That(text).IsEqualTo("Ada");
    }

    [Test]
    public async Task ExternalAccessSample_RefusesAnExternalEntity()
    {
        // <sample>
        var options = XmlReaderOptions.Default.WithDtdProcessing(true);
        string? code = null;
        try
        {
            XmlDocumentLoader.Load(
                "<!DOCTYPE note [<!ENTITY who SYSTEM \"who.xml\">]><note>&who;</note>".AsSpan(),
                options
            );
        }
        catch (XmlParseException exception)
        {
            code = exception.Diagnostic?.Code.Alias;
        }
        // </sample>

        await Assert.That(code).IsEqualTo("XML2002");
    }

    [Test]
    public async Task LimitsSample_RejectsADocumentThatIsTooDeep()
    {
        // <sample>
        var limits = XmlReaderLimits.Default.WithMaxElementDepth(2);
        var options = XmlReaderOptions.Default.WithLimits(limits);
        string? code = null;
        try
        {
            XmlDocumentLoader.Load("<a><b><c/></b></a>".AsSpan(), options);
        }
        catch (XmlParseException exception)
        {
            code = exception.Diagnostic?.Code.Alias;
        }
        // </sample>

        await Assert.That(code).IsEqualTo("XML1102");
    }

    [Test]
    public async Task ExternalSubsetOfAnEnabledDoctype_IsSkippedWithoutAnyRequest()
    {
        var options = XmlReaderOptions.Default.WithDtdProcessing(true);

        var tree = XmlDocumentLoader.Load("<!DOCTYPE note SYSTEM \"note.dtd\"><note/>".AsSpan(), options);

        await Assert.That(tree.Names.GetString(tree.Document.FirstChild.Name.Local)).IsEqualTo("note");
    }

    [Test]
    public async Task XmlOneDotOneDocument_IsRejected()
    {
        string? code = null;
        try
        {
            XmlDocumentLoader.Load("<?xml version=\"1.1\"?><a/>".AsSpan());
        }
        catch (XmlParseException exception)
        {
            code = exception.Diagnostic?.Code.Alias;
        }

        await Assert.That(code).IsEqualTo("XML0010");
    }

    private static string? CodeOf(string xml, XmlReaderOptions? options = null)
    {
        try
        {
            _ = XmlDocumentLoader.Load(xml.AsSpan(), options);
            return null;
        }
        catch (XmlParseException exception)
        {
            return exception.Diagnostic?.Code.Alias;
        }
    }

    [Test]
    public async Task Limitations_AreBackedByTheReader(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dtd = XmlReaderOptions.Default.WithDtdProcessing(true);

        // Another version than 1.0, including one the Fifth Edition allows, is rejected.
        await Assert.That(CodeOf("<?xml version=\"1.7\"?><a/>")).IsEqualTo("XML0010");
        // Namespaces are always processed.
        await Assert.That(CodeOf("<p:a/>")).IsEqualTo("XML3001");
        // An external entity in an attribute value is always an error; an unreachable declaration is skipped.
        await Assert
            .That(CodeOf("<!DOCTYPE a [<!ENTITY x SYSTEM \"x.xml\">]><a b=\"&x;\"/>", dtd))
            .IsEqualTo("XML0022");
        await Assert.That(CodeOf("<!DOCTYPE a SYSTEM \"a.dtd\"><a>&x;</a>", dtd)).IsNull();
        // The notations of the declaration are not added to the tree.
        var tree = XmlDocumentLoader.Load("<!DOCTYPE a [<!NOTATION n SYSTEM \"n\">]><a/>".AsSpan(), dtd);
        await Assert.That(tree.Notations).IsEmpty();
        await Assert.That(tree.UnparsedEntities).IsEmpty();
        // The reader does not validate: an element that its declaration forbids is accepted; the validator of the entry point reports it.
        await Assert.That(CodeOf("<!DOCTYPE a [<!ELEMENT a EMPTY>]><a>x</a>", dtd)).IsNull();
        var validator = await SchemaValidator.ForDtd().BuildAsync(cancellationToken);
        await Assert.That(validator).IsAssignableTo<IDtdValidator>();
    }

    [Test]
    public async Task OptionsDefault_RejectsTheDoctypeAndAFixedLimitIsNeverDisabledByZero()
    {
        await Assert.That(XmlReaderOptions.Default.DtdProcessing).IsFalse();
        await Assert.That(XmlReaderOptions.Default.NameTable).IsNull();
        await Assert.That(() => XmlReaderLimits.Default.WithMaxElementDepth(0)).Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => XmlReaderLimits.Default.WithMaxDocumentCharacters(0))
            .Throws<ArgumentOutOfRangeException>();
    }

    private static readonly (string Name, string Code, string Meaning)[] LimitRows =
    [
        ("MaxDocumentCharacters", XmlErrorCodes.XML1101.Alias, "Decoded characters of one document."),
        ("MaxElementDepth", XmlErrorCodes.XML1102.Alias, "Element nesting depth."),
        ("MaxAttributesPerElement", XmlErrorCodes.XML1103.Alias, "Attributes of one element."),
        ("MaxNamespaceDeclarationsPerElement", XmlErrorCodes.XML1104.Alias, "Namespace declarations of one element."),
        ("MaxNameLength", XmlErrorCodes.XML1105.Alias, "Length of a name and of the body of a reference."),
        ("MaxTokenLength", XmlErrorCodes.XML1106.Alias, "Length of a comment or a processing instruction."),
        ("MaxTextNodeLength", XmlErrorCodes.XML1107.Alias, "Length of a coalesced text node."),
        ("MaxNodeCount", XmlErrorCodes.XML1108.Alias, "Nodes of one document."),
        (
            "MaxEntityExpansionCharacters",
            XmlErrorCodes.XML1109.Alias,
            "Characters that entity expansion produces, charged for every reference."
        ),
        ("MaxEntityDepth", XmlErrorCodes.XML1110.Alias, "Nesting depth of entity references."),
        ("MaxDistinctNames", XmlErrorCodes.XML1111.Alias, "Distinct names in a name table."),
        ("MaxAttributeValueLength", XmlErrorCodes.XML1112.Alias, "Length of a normalized attribute value."),
        ("MaxStartTagCharacters", XmlErrorCodes.XML1113.Alias, "Characters of a buffered start tag."),
        ("MaxXmlDeclarationLength", XmlErrorCodes.XML1114.Alias, "Length of the XML declaration."),
        ("MaxDoctypeLength", XmlErrorCodes.XML1115.Alias, "Length of the document type declaration."),
    ];

    [Test]
    public async Task Readme_NamesEveryLimitWithItsDefaultAndErrorCode()
    {
        var readme = ReadmeSamples.ReadText(Readme);
        var defaults = XmlReaderLimits.Default;
        var properties = new List<PropertyInfo>();
        foreach (var property in typeof(XmlReaderLimits).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType == typeof(int) || property.PropertyType == typeof(long))
            {
                properties.Add(property);
            }
        }

        await Assert.That(LimitRows.Length).IsEqualTo(properties.Count);
        foreach (var (name, code, meaning) in LimitRows)
        {
            var property = typeof(XmlReaderLimits).GetProperty(name)!;
            var value = Convert.ToInt64(property.GetValue(defaults), CultureInfo.InvariantCulture);

            await Assert
                .That(readme)
                .Contains($"| `{name}` | {value.ToString("N0", CultureInfo.InvariantCulture)} | {code} | {meaning} |");
        }
    }

    [Test]
    public async Task Readme_NamesTheInputKindsAndTheReaders()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).StartsWith("# NetEvolve.Schemas.Xml");
        await Assert.That(readme).Contains("dotnet add package NetEvolve.Schemas.Xml");
        foreach (
            var name in new[]
            {
                "XmlDocumentLoader",
                "Load(",
                "LoadFromMemory(",
                "LoadAsync(",
                "XmlPipelineReader",
                "XmlSequenceReader",
                "XmlStreamReader<TSink>",
                "XmlReaderOptions",
                "XmlReaderLimits",
                "XmlParseException",
            }
        )
        {
            await Assert.That(readme).Contains(name);
        }
    }

    [Test]
    public async Task Readme_StatesTheSecurityDefaults()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains("## Security defaults");
        await Assert.That(readme).Contains("XML2001");
        await Assert.That(readme).Contains("XML2002");
        await Assert.That(readme).Contains("WithDtdProcessing");
        await Assert.That(readme).Contains("never disables");
    }

    [Test]
    public async Task Readme_StatesTheLimitations()
    {
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains("## Limitations");
        await Assert.That(readme).Contains("XML 1.1 is not supported");
        await Assert.That(readme).Contains("DTD processing is off by default");
        await Assert.That(readme).Contains("Span inputs are synchronous");
        await Assert.That(readme).Contains("The stream path buffers");
    }

    [Test]
    public async Task EveryReadmeCodeBlock_IsACompiledSample()
    {
        var compiled = new List<string>(ReadmeSamples.SamplesIn(Samples));
        compiled.AddRange(ReadmeSamples.SamplesIn(DtdSamples));
        compiled.AddRange(ReadmeSamples.SamplesIn(LegacyDtdSamples));
        var blocks = ReadmeSamples.CodeBlocks(Readme);

        await Assert.That(blocks).Count().IsEqualTo(compiled.Count);
        foreach (var block in blocks)
        {
            await Assert.That(compiled).Contains(block);
        }
    }
}
