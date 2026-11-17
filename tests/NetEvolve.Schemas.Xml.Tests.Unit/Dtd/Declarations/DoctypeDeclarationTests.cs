namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Declarations;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Tests.Unit.Reader.Core;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>Productions 28 and 29, comments and instructions in the subset, the opt-in, and chunking.</summary>
public class DoctypeDeclarationTests
{
    private static readonly XmlReaderOptions OptIn = XmlReaderOptions.Default.WithDtdProcessing(true);

    private static (XmlReadStatus Status, XmlErrorCode? Code, string Model) Read(string xml, XmlReaderOptions options)
    {
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), options);
        var sink = new RecordingXmlSink(new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames));
        var status = reader.Pump(ref sink);

        return (
            status,
            reader.Diagnostic?.Code,
            reader.Declarations is null ? "<none>" : DtdHarness.Dump(reader.Declarations)
        );
    }

    [Test]
    [Arguments("<!DOCTYPE a><a/>")]
    [Arguments("<!DOCTYPE a [<!ELEMENT a EMPTY>]><a/>")]
    [Arguments("<!DOCTYPE a SYSTEM \"a.dtd\"><a/>")]
    public async Task DefaultOptions_RejectAnyDoctypeWithXml2001(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var (status, code, model) = Read(xml, XmlReaderOptions.Default);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(code).IsSameReferenceAs(XmlErrorCodes.XML2001);
        await Assert.That(model).IsEqualTo("<none>");
    }

    [Test]
    public async Task DefaultOptions_RejectDoctypeForEveryFactory()
    {
        const string xml = "<!DOCTYPE a [<!ELEMENT a EMPTY>]><a/>";
        var bytes = System.Text.Encoding.UTF8.GetBytes(xml);

        foreach (
            var reader in new[]
            {
                XmlPipelineReader.Create(bytes.AsSpan()),
                XmlPipelineReader.CreateFromMemory(xml.AsMemory()),
                XmlPipelineReader.CreateFromMemory(bytes.AsMemory()),
            }
        )
        {
            using (reader)
            {
                var sink = new RecordingXmlSink(new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames));
                var status = reader.Pump(ref sink);

                await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
                await Assert.That(reader.Diagnostic?.Code).IsSameReferenceAs(XmlErrorCodes.XML2001);
            }
        }
    }

    [Test]
    public async Task Option_IsOffByDefault_AndSurvivesOtherWithMethods()
    {
        var limits = XmlReaderLimits.Default.WithMaxDoctypeLength(77);
        var on = XmlReaderOptions.Default.WithDtdProcessing(true).WithLimits(limits);

        await Assert.That(XmlReaderOptions.Default.DtdProcessing).IsFalse();
        await Assert.That(on.DtdProcessing).IsTrue();
        await Assert.That(on.Limits.MaxDoctypeLength).IsEqualTo(77);
        await Assert.That(on.WithDtdProcessing(false).DtdProcessing).IsFalse();
    }

    [Test]
    public async Task OptIn_ParsesTheNameIdentifiersAndSubset_AndCompletesTheDocument()
    {
        const string xml =
            "<!DOCTYPE note PUBLIC \"-//X//Y\" 'x.dtd' [<!ELEMENT note EMPTY>\r\n<!ATTLIST note id ID #IMPLIED>]>\r\n<note/>";
        using var reader = XmlPipelineReader.Create(xml.AsSpan(), OptIn);
        var sink = new RecordingXmlSink(new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames));

        var status = reader.Pump(ref sink);
        var model = reader.Declarations!;

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(model.Name).IsEqualTo("note");
        await Assert.That(model.PublicId).IsEqualTo("-//X//Y");
        await Assert.That(model.SystemId).IsEqualTo("x.dtd");
        await Assert.That(model.HasExternalSubset).IsTrue();
        await Assert.That(DtdHarness.Dump(model)).IsEqualTo("E:note:EMPTY|A:note/id:ID:#IMPLIED");
    }

    [Test]
    public async Task OptIn_WithoutSubsetOrIdentifiers_GivesAnEmptyModel()
    {
        var (status, code, model) = Read("<!DOCTYPE a><a/>", OptIn);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(code).IsNull();
        await Assert.That(model).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task OptIn_InvalidDeclaration_ReportsTheCodedErrorForTheDocument()
    {
        var (status, code, _) = Read("<!DOCTYPE a [<!ELEMENT a>]><a/>", OptIn);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(code).IsSameReferenceAs(XmlErrorCodes.XML0011);
    }

    [Test]
    public async Task OptIn_ParameterEntityInsideADeclaration_ReportsXml0019()
    {
        var (status, code, _) = Read("<!DOCTYPE a [<!ELEMENT a %c;>]><a/>", OptIn);

        await Assert.That(status).IsEqualTo(XmlReadStatus.Error);
        await Assert.That(code).IsSameReferenceAs(XmlErrorCodes.XML0019);
    }

    [Test]
    [Arguments("<!-- c ] --><?pi ] ?><!ELEMENT a EMPTY>", "E:a:EMPTY", null)]
    [Arguments("<!--a--b-->", "<none>", "XML0014")]
    [Arguments("<?xml version=\"1.0\"?>", "<none>", "XML0015")]
    [Arguments("<?XmL x?>", "<none>", "XML0015")]
    [Arguments("<![INCLUDE[ ]]>", "<none>", "XML0011")]
    [Arguments("junk", "<none>", "XML0011")]
    [Arguments("<!FOO a>", "<none>", "XML0011")]
    [Arguments("<!ELEMENT a EMPTY>]", "<none>", "XML0011")]
    public async Task SubsetMarkup_CommentsAndInstructionsAreSkipped_OthersAreCodedErrors(
        string subset,
        string model,
        string? alias
    )
    {
        ArgumentNullException.ThrowIfNull(subset);
        var (actual, error) = DtdHarness.Parse(subset);

        await Assert.That(error?.Alias).IsEqualTo(alias);
        await Assert.That(actual).IsEqualTo(model);
    }

    [Test]
    public async Task Subset_SplitAtEveryOffset_GivesTheSameModel()
    {
        const string xml =
            "<?xml version=\"1.0\"?><!DOCTYPE doc SYSTEM 's.dtd' [<!ENTITY % d \"<!ELEMENT doc (a|b)*>\">%d;"
            + "<!ATTLIST doc x NOTATION (n) 'n' y CDATA #REQUIRED><!ENTITY g \"v&#65;\"><!ENTITY u SYSTEM 'u' NDATA n>"
            + "<!NOTATION n PUBLIC 'p' 's'><!-- ] --><?p ]?>]><doc/>";
        var expectedHandler = new DtdDoctypeHandler(XmlReaderLimits.Default);
        var expected = ReaderHarness.Run(xml, doctype: expectedHandler);
        var expectedModel = DtdHarness.Dump(expectedHandler.Declarations!);

        await Assert.That(expected.Status).IsEqualTo(XmlReadStatus.Completed);
        await Assert.That(expectedModel).Contains("E:doc:(a|b)*");

        for (var split = 0; split <= xml.Length; split++)
        {
            var handler = new DtdDoctypeHandler(XmlReaderLimits.Default);
            var run = ReaderHarness.Run(xml, split: split, doctype: handler);

            await Assert.That(run).IsEqualTo(expected);
            await Assert.That(DtdHarness.Dump(handler.Declarations!)).IsEqualTo(expectedModel);
        }
    }

    [Test]
    public async Task Subset_SplitAtEveryOffset_GivesTheSameError()
    {
        const string xml = "<!DOCTYPE doc [<!ELEMENT doc EMPTY><!ENTITY g \"a%p;\">]><doc/>";
        var expected = ReaderHarness.Run(xml, doctype: new DtdDoctypeHandler(XmlReaderLimits.Default));

        await Assert.That(expected.Code).IsSameReferenceAs(XmlErrorCodes.XML0019);

        for (var split = 0; split <= xml.Length; split++)
        {
            var run = ReaderHarness.Run(xml, split: split, doctype: new DtdDoctypeHandler(XmlReaderLimits.Default));

            await Assert.That(run).IsEqualTo(expected);
        }
    }
}
