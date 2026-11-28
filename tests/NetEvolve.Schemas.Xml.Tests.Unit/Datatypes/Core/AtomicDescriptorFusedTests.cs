namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core.Fakes;

public class AtomicDescriptorFusedTests
{
    private static AtomicDescriptor<int> Restricted(FakeFusedDigitsMapping mapping, params IFacet[] facets) =>
        new("small", new AtomicDescriptor<int>("digits", XsdVersion.V11, mapping), null, facets);

    [Test]
    public async Task Parse_UsesTheFusedPathOnce_AndNeverCheckOrMap()
    {
        var mapping = new FakeFusedDigitsMapping();
        var descriptor = new AtomicDescriptor<int>("digits", XsdVersion.V11, mapping);

        var result = descriptor.Parse("  042 ");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
        await Assert.That(mapping.TryMapCalls).IsEqualTo(1);
        await Assert.That(mapping.CheckCalls).IsEqualTo(0);
        await Assert.That(mapping.MapCalls).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithoutValueFacets_KeepsTheCheckOnlyPath()
    {
        var mapping = new FakeFusedDigitsMapping();
        var descriptor = new AtomicDescriptor<int>("digits", XsdVersion.V11, mapping);

        var result = descriptor.Validate("42");

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(mapping.CheckCalls).IsEqualTo(1);
        await Assert.That(mapping.TryMapCalls).IsEqualTo(0);
        await Assert.That(mapping.MapCalls).IsEqualTo(0);
    }

    [Test]
    public async Task Validate_WithValueFacets_UsesTheFusedPathOnce()
    {
        var mapping = new FakeFusedDigitsMapping();
        var descriptor = Restricted(mapping, new FakeMaxInclusiveFacet(5));

        var valid = descriptor.Validate("4");
        var invalid = descriptor.Validate("7");

        await Assert.That(valid.IsValid).IsTrue();
        await Assert.That(invalid.IsValid).IsFalse();
        await Assert.That(invalid.FailedFacet).IsEqualTo(FacetKind.MaxInclusive);
        await Assert.That(mapping.TryMapCalls).IsEqualTo(2);
        await Assert.That(mapping.CheckCalls).IsEqualTo(0);
        await Assert.That(mapping.MapCalls).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_FusedLexicalError_ReportsTheOffsetInTheOriginalInput()
    {
        var descriptor = new AtomicDescriptor<int>("digits", XsdVersion.V11, new FakeFusedDigitsMapping());

        var result = descriptor.Parse("  12x");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(result.ErrorOffset).IsEqualTo(4);
    }

    [Test]
    public async Task Parse_FusedLexicalFacetFailure_IsReportedBeforeValueFacets()
    {
        var descriptor = Restricted(new FakeFusedDigitsMapping(), new FakeMaxInclusiveFacet(5), new FakeLengthFacet(1));

        var result = descriptor.Validate("99");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.MaxLength);
    }
}
