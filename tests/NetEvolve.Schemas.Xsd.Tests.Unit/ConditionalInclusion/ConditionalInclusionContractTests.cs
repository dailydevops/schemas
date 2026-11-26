namespace NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion;

using System.Text;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Reader.Pipeline;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.ConditionalInclusion;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using static NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion.ConditionalInclusionFixture;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>The built-in types and facets that the processor reports as available, the input paths, and the argument guards.</summary>
public class ConditionalInclusionContractTests
{
    private const string XsdNamespace = "http://www.w3.org/2001/XMLSchema";

    [Test]
    public async Task EveryBuiltInTypeOfVersion11_IsAvailableInTheXsdNamespaceOnly()
    {
        var types = XsdBuiltInTypes.For(XsdVersion.V11);

        await Assert.That(types.Count).IsGreaterThan(40);
        for (var i = 0; i < types.Count; i++)
        {
            var name = types[i].Name;
            await Assert.That(ProcessorCapabilities.IsTypeAvailable(XsdNamespace, name.LocalName)).IsTrue();
            await Assert.That(ProcessorCapabilities.IsTypeAvailable(string.Empty, name.LocalName)).IsFalse();
            await Assert.That(ProcessorCapabilities.IsTypeAvailable("urn:other", name.LocalName)).IsFalse();
        }

        await Assert.That(ProcessorCapabilities.IsTypeAvailable(XsdNamespace, "foo")).IsFalse();
    }

    [Test]
    public async Task EveryFacet_IsAvailableByTheNameOfItsElement()
    {
        foreach (var facet in Enum.GetValues<FacetKind>())
        {
            if (facet is FacetKind.None or FacetKind.Assertions)
            {
                continue;
            }

            var name = char.ToLowerInvariant(facet.ToString()[0]) + facet.ToString().Substring(1);

            await Assert.That(ProcessorCapabilities.IsFacetAvailable(XsdNamespace, name)).IsTrue();
            await Assert.That(ProcessorCapabilities.IsFacetAvailable(string.Empty, name)).IsFalse();
        }

        await Assert.That(ProcessorCapabilities.IsFacetAvailable(XsdNamespace, "assertion")).IsTrue();
        await Assert.That(ProcessorCapabilities.IsFacetAvailable(string.Empty, "assertion")).IsFalse();
        await Assert.That(ProcessorCapabilities.IsFacetAvailable(XsdNamespace, "assertions")).IsFalse();
        await Assert.That(ProcessorCapabilities.IsFacetAvailable(XsdNamespace, "string")).IsFalse();
    }

    [Test]
    public async Task ByteInput_GivesTheSameResultAsCharacterInput()
    {
        var text = Schema(
            "<xs:element name=\"a\" vc:minVersion=\"3.2\"><xs:bogus/></xs:element><xs:element name=\"b\" vc:typeAvailable=\"xs:string\"/>"
        );
        var (fromChars, _) = Run(text);

        var reporter = NewReporter(out var collector);
        var inner = new SchemaSyntaxSink(SystemId, reporter);
        var filter = new ConditionalInclusionSink(inner, XsdVersion.V11, SystemId, reporter);
        using var reader = XmlPipelineReader.Create(Encoding.UTF8.GetBytes(text).AsSpan());
        _ = reader.Pump(ref filter);

        await Assert.That(Dump(inner.Root!)).IsEqualTo(Dump(fromChars!));
        await Assert.That(collector.ToResult().Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TheFilterSharesTheNameTableOfTheInnerSink()
    {
        var reporter = NewReporter(out _);
        var inner = new SchemaSyntaxSink(SystemId, reporter);
        var filter = new ConditionalInclusionSink(inner, XsdVersion.V11, SystemId, reporter);

        await Assert.That(filter.Names).IsSameReferenceAs(inner.Names);
    }

    [Test]
    public async Task ABaseUriIsOptional()
    {
        var reporter = NewReporter(out var collector);
        var inner = new SchemaSyntaxSink(null, reporter);
        var filter = new ConditionalInclusionSink(inner, XsdVersion.V11, null, reporter);
        using var reader = XmlPipelineReader.Create(Element("vc:minVersion=\"x\"").AsSpan());
        _ = reader.Pump(ref filter);

        await Assert.That(collector.ToResult().Issues.Count).IsEqualTo(1);
        await Assert.That(collector.ToResult().Issues[0].Location.Source).IsNull();
    }

    [Test]
    public async Task TheConstructor_RejectsAMissingInnerSinkAReporterAndAnUndefinedVersion()
    {
        var reporter = NewReporter(out _);
        var inner = new SchemaSyntaxSink(SystemId, reporter);

        await Assert
            .That(() => new ConditionalInclusionSink(null!, XsdVersion.V11, SystemId, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new ConditionalInclusionSink(inner, XsdVersion.V11, SystemId, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new ConditionalInclusionSink(inner, (XsdVersion)99, SystemId, reporter))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => new ConditionalInclusionSink(inner, (XsdVersion)(-1), SystemId, reporter))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TheCapabilityLookups_RejectMissingNames()
    {
        await Assert.That(() => ProcessorCapabilities.IsTypeAvailable(null!, "string")).Throws<ArgumentNullException>();
        await Assert
            .That(() => ProcessorCapabilities.IsTypeAvailable(XsdNamespace, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ProcessorCapabilities.IsFacetAvailable(null!, "pattern"))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ProcessorCapabilities.IsFacetAvailable(XsdNamespace, null!))
            .Throws<ArgumentNullException>();
    }
}
