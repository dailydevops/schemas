namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Facets.Ordered;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Binary;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;

public class BinaryLengthFacetTests
{
    private static ValidationResult Validate(string type, BinaryLengthFacet facet, string lexical) =>
        OrderedFacetSupport.Restrict<XsdBinary>(type, XsdVersion.V11, facet).Validate(lexical.AsSpan());

    [Test]
    [Arguments("0A0B0C", 3, true)]
    [Arguments("0A0B0C", 6, false)]
    [Arguments("0A0B", 2, true)]
    [Arguments("0A0B", 4, false)]
    [Arguments("0a0b0c", 3, true)]
    [Arguments("", 0, true)]
    [Arguments("", 1, false)]
    [Arguments("  0A0B  ", 2, true)]
    public async Task HexBinary_Length_CountsOctets(string lexical, int length, bool valid)
    {
        var result = Validate("hexBinary", BinaryLengthFacet.Length(length, BinaryKind.HexBinary), lexical);

        await Assert.That(result.IsValid).IsEqualTo(valid).Because($"length {length}, value '{lexical}'");
        if (!valid)
        {
            await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.Length);
        }
    }

    [Test]
    [Arguments("AQID", 3, true)]
    [Arguments("AQID", 4, false)]
    [Arguments("AQI=", 2, true)]
    [Arguments("AQI=", 4, false)]
    [Arguments("AQ==", 1, true)]
    [Arguments("AQ==", 4, false)]
    [Arguments("AQID AQID", 6, true)]
    [Arguments("AQID AQID", 8, false)]
    [Arguments("", 0, true)]
    [Arguments("  AQID  ", 3, true)]
    public async Task Base64Binary_Length_CountsOctetsNotCharacters(string lexical, int length, bool valid)
    {
        var result = Validate("base64Binary", BinaryLengthFacet.Length(length, BinaryKind.Base64Binary), lexical);

        await Assert.That(result.IsValid).IsEqualTo(valid).Because($"length {length}, value '{lexical}'");
        if (!valid)
        {
            await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.Length);
        }
    }

    [Test]
    [Arguments("0A", false)]
    [Arguments("0A0B", true)]
    [Arguments("0A0B0C", true)]
    public async Task HexBinary_MinLength_CountsOctets(string lexical, bool valid)
    {
        var result = Validate("hexBinary", BinaryLengthFacet.MinLength(2, BinaryKind.HexBinary), lexical);

        await Assert.That(result.IsValid).IsEqualTo(valid);
        if (!valid)
        {
            await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.MinLength);
        }
    }

    [Test]
    [Arguments("0A", true)]
    [Arguments("0A0B", true)]
    [Arguments("0A0B0C", false)]
    public async Task HexBinary_MaxLength_CountsOctets(string lexical, bool valid)
    {
        var result = Validate("hexBinary", BinaryLengthFacet.MaxLength(2, BinaryKind.HexBinary), lexical);

        await Assert.That(result.IsValid).IsEqualTo(valid);
        if (!valid)
        {
            await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.MaxLength);
        }
    }

    [Test]
    [Arguments("AQ==", false, true)]
    [Arguments("AQI=", true, true)]
    [Arguments("AQID", true, true)]
    [Arguments("AQIDBA==", true, false)]
    public async Task Base64Binary_MinLengthAndMaxLength_CountOctets(string lexical, bool minOk, bool maxOk)
    {
        var minLength = Validate("base64Binary", BinaryLengthFacet.MinLength(2, BinaryKind.Base64Binary), lexical);
        var maxLength = Validate("base64Binary", BinaryLengthFacet.MaxLength(3, BinaryKind.Base64Binary), lexical);

        await Assert.That(minLength.IsValid).IsEqualTo(minOk);
        await Assert.That(maxLength.IsValid).IsEqualTo(maxOk);
    }

    [Test]
    public async Task LengthFacets_CombineOnOneRestriction()
    {
        var type = OrderedFacetSupport.Restrict<XsdBinary>(
            "hexBinary",
            XsdVersion.V11,
            BinaryLengthFacet.MinLength(2, BinaryKind.HexBinary),
            BinaryLengthFacet.MaxLength(4, BinaryKind.HexBinary)
        );

        await Assert.That(type.Validate("0A".AsSpan()).FailedFacet).IsEqualTo(FacetKind.MinLength);
        await Assert.That(type.Validate("0A0B".AsSpan()).IsValid).IsTrue();
        await Assert.That(type.Validate("0A0B0C0D".AsSpan()).IsValid).IsTrue();
        await Assert.That(type.Validate("0A0B0C0D0E".AsSpan()).FailedFacet).IsEqualTo(FacetKind.MaxLength);
    }

    [Test]
    public async Task Facet_ExposesKindValueAndEncoding()
    {
        var length = BinaryLengthFacet.Length(3, BinaryKind.HexBinary);

        await Assert.That(length.Kind).IsEqualTo(FacetKind.Length);
        await Assert.That(BinaryLengthFacet.MinLength(1, BinaryKind.Base64Binary).Kind).IsEqualTo(FacetKind.MinLength);
        await Assert.That(BinaryLengthFacet.MaxLength(1, BinaryKind.Base64Binary).Kind).IsEqualTo(FacetKind.MaxLength);
        await Assert.That(length.Value).IsEqualTo(3);
        await Assert.That(length.Encoding).IsEqualTo(BinaryKind.HexBinary);
    }

    [Test]
    public async Task Factories_RejectANegativeLengthAndAnUndefinedEncoding()
    {
        await Assert
            .That(() => BinaryLengthFacet.Length(-1, BinaryKind.HexBinary))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => BinaryLengthFacet.MinLength(-1, BinaryKind.HexBinary))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => BinaryLengthFacet.MaxLength(-1, BinaryKind.HexBinary))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => BinaryLengthFacet.Length(1, (BinaryKind)2)).Throws<ArgumentOutOfRangeException>();
    }

    // The length facets of the binary types are valid restrictions under the framework rules of the core facets.
    [Test]
    [Arguments(PrimitiveKind.HexBinary)]
    [Arguments(PrimitiveKind.Base64Binary)]
    public async Task LengthRestrictions_OnBinaryTypes_FollowTheCoreRules(PrimitiveKind primitive)
    {
        var conflicting = FacetRestriction.Validate(
            new FacetSet(minLength: 4),
            new FacetSet(maxLength: 3),
            DatatypeVariety.Atomic,
            primitive,
            XsdVersion.V11
        );
        var narrower = FacetRestriction.Validate(
            new FacetSet(minLength: 1, maxLength: 8),
            new FacetSet(minLength: 2, maxLength: 4),
            DatatypeVariety.Atomic,
            primitive,
            XsdVersion.V11
        );

        await Assert.That(conflicting.Error).IsEqualTo(FacetRestrictionError.MinLengthGreaterThanMaxLength);
        await Assert.That(narrower.IsValid).IsTrue();
    }
}
