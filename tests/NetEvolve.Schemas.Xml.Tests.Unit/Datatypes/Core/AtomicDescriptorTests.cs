namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Contracts.Fakes;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core.Fakes;

public class AtomicDescriptorTests
{
    private static AtomicDescriptor<int> Primitive() => new("digits", XsdVersion.V11, new FakeDigitsMapping());

    private static AtomicDescriptor<int> Restricted(params IFacet[] facets) => new("small", Primitive(), null, facets);

    [Test]
    public async Task Primitive_ExposesTheDescriptorShape()
    {
        var descriptor = Primitive();

        await Assert.That(descriptor.Name).IsEqualTo("digits");
        await Assert.That(descriptor.Variety).IsEqualTo(DatatypeVariety.Atomic);
        await Assert.That(descriptor.BaseType).IsNull();
        await Assert.That(descriptor.LexicalMode).IsEqualTo(XsdVersion.V11);
        await Assert.That(descriptor.IsOrdered).IsTrue();
        await Assert.That(descriptor.Whitespace).IsEqualTo(WhitespaceMode.Collapse);
        await Assert.That(descriptor.Facets.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_RunsWhitespaceThenLexicalCheckThenValueMapping()
    {
        var result = Primitive().Parse("  \t042\n ");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Parse_InvalidLexical_ReportsTheErrorOffsetInTheOriginalInput()
    {
        var result = Primitive().Parse("  12x");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(result.ErrorOffset).IsEqualTo(4);
    }

    [Test]
    public async Task Validate_InvalidLexical_ReportsTheErrorOffsetInTheOriginalInput()
    {
        var result = Primitive().Validate("\t\t9z");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.None);
        await Assert.That(result.Offset).IsEqualTo(3);
    }

    [Test]
    public async Task Restriction_InheritsTheMappingAndAccumulatesFacets()
    {
        var derived = new AtomicDescriptor<int>(
            "short",
            Restricted(new FakeMaxInclusiveFacet(500)),
            null,
            [new FakeLengthFacet(2)]
        );

        await Assert.That(derived.BaseType!.Name).IsEqualTo("small");
        await Assert.That(derived.LexicalMode).IsEqualTo(XsdVersion.V11);
        await Assert.That(derived.Facets.Count).IsEqualTo(2);
        await Assert.That(derived.Facets[0].Kind).IsEqualTo(FacetKind.MaxInclusive);
        await Assert.That(derived.Facets[1].Kind).IsEqualTo(FacetKind.MaxLength);
        await Assert.That(derived.Validate("99").IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_LexicalFacetFailure_IsReportedBeforeValueFacets()
    {
        var descriptor = Restricted(new FakeMaxInclusiveFacet(5), new FakeLengthFacet(1));

        var result = descriptor.Validate("99");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.MaxLength);
        await Assert.That(result.Error).IsEqualTo(DatatypeError.None);
    }

    [Test]
    public async Task Validate_ValueFacetFailure_ReportsTheFacet()
    {
        var result = Restricted(new FakeMaxInclusiveFacet(5)).Validate("7");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.MaxInclusive);
    }

    [Test]
    public async Task Parse_FacetFailure_ReportsAFacetViolation()
    {
        var result = Restricted(new FakeMaxInclusiveFacet(5)).Parse("7");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.FacetViolation);
    }

    [Test]
    public async Task Parse_PassesTheEffectiveWhitespaceModeToTheMapping()
    {
        var mapping = new FakeDigitsMapping();
        var restricted = new AtomicDescriptor<int>("r", new AtomicDescriptor<int>("p", XsdVersion.V11, mapping), WhitespaceMode.Preserve, []);

        _ = restricted.Parse("12");

        await Assert.That(mapping.LastMode).IsEqualTo(WhitespaceMode.Preserve);
    }

    [Test]
    public async Task Restriction_WhitespaceOverride_ReplacesTheInheritedMode()
    {
        var preserved = new AtomicDescriptor<int>("p", Primitive(), WhitespaceMode.Preserve, []);

        await Assert.That(preserved.Whitespace).IsEqualTo(WhitespaceMode.Preserve);
        await Assert.That(preserved.Validate(" 1").IsValid).IsFalse();
    }

    [Test]
    public async Task CompareAndIdentity_DelegateToTheMapping()
    {
        var descriptor = Restricted();

        await Assert.That(descriptor.Compare(1, 2)).IsEqualTo(XsdOrder.Less);
        await Assert.That(descriptor.Compare(2, 2)).IsEqualTo(XsdOrder.Equal);
        await Assert.That(descriptor.IsIdentical(2, 2)).IsTrue();
    }

    [Test]
    public async Task CrossValueTypeRestriction_DerivesFromABaseOfAnotherValueType()
    {
        var derived = new AtomicDescriptor<long>(
            "wide",
            Restricted(new FakeMaxInclusiveFacet(500)),
            new WideMapping(),
            null,
            [new FakeLengthFacet(3)]
        );

        await Assert.That(derived.BaseType!.Name).IsEqualTo("small");
        await Assert.That(derived.LexicalMode).IsEqualTo(XsdVersion.V11);
        await Assert.That(derived.Whitespace).IsEqualTo(WhitespaceMode.Collapse);
        await Assert.That(derived.Facets.Count).IsEqualTo(2);
        await Assert.That(derived.Facets[0].Kind).IsEqualTo(FacetKind.MaxInclusive);
        await Assert.That(derived.Facets[1].Kind).IsEqualTo(FacetKind.MaxLength);
        await Assert.That(derived.Parse(" 123 ").Value).IsEqualTo(123L);
        await Assert.That(derived.Validate("1234").FailedFacet).IsEqualTo(FacetKind.MaxLength);
    }

    [Test]
    public async Task CrossValueTypeRestriction_WhitespaceOverride_ReplacesTheMappingMode()
    {
        var derived = new AtomicDescriptor<long>("wide", Primitive(), new WideMapping(), WhitespaceMode.Preserve, []);

        await Assert.That(derived.Whitespace).IsEqualTo(WhitespaceMode.Preserve);
    }

    private sealed class WideMapping : ILexicalMapping<long>
    {
        private readonly FakeDigitsMapping _digits = new();

        public WhitespaceMode Whitespace => WhitespaceMode.Collapse;

        public bool IsOrdered => true;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset) =>
            _digits.Check(normalized, version, out errorOffset);

        public long Map(ReadOnlySpan<char> normalized, XsdVersion version) => _digits.Map(normalized, version);

        public XsdOrder Compare(in long a, in long b, XsdVersion version) =>
            a < b ? XsdOrder.Less
            : a > b ? XsdOrder.Greater
            : XsdOrder.Equal;

        public bool IsIdentical(in long a, in long b, XsdVersion version) => a == b;
    }

    [Test]
    public async Task Constructor_NullArguments_Throw()
    {
        await Assert
            .That(() => new AtomicDescriptor<int>(null!, XsdVersion.V10, new FakeDigitsMapping()))
            .Throws<ArgumentNullException>();
        await Assert.That(() => new AtomicDescriptor<int>("x", XsdVersion.V10, null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new AtomicDescriptor<int>("x", null!, null, [])).Throws<ArgumentNullException>();
    }
}
