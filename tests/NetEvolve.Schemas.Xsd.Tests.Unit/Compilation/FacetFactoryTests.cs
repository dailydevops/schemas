namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System;
using System.Collections.Generic;
using System.Linq;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Ordered;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Pattern;
using NetEvolve.Schemas.Xml.Datatypes.Numeric;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

/// <summary>The facets of a restriction step become the facet objects of the datatype contract that the type definition carries.</summary>
public class FacetFactoryTests
{
    // The own facets of a type: the facets of a built-in base type are listed first and are skipped. The type is read from the
    // context, not from a compiled set, so a facet that the check rejects is still seen.
    private static IFacet[] Facets(string baseType, string facets)
    {
        var inherited = Built("<xs:simpleType name=\"p\"><xs:restriction base=\"" + baseType + "\"/></xs:simpleType>");
        var type = Built(
            "<xs:simpleType name=\"s\"><xs:restriction base=\""
                + baseType
                + "\">"
                + facets
                + "</xs:restriction></xs:simpleType>"
        );

        return type.Facets.Skip(inherited.Facets.Count).ToArray();
    }

    private static SimpleTypeDefinition Built(string body)
    {
        var kit = new ContextKit(body);

        return (SimpleTypeDefinition)kit.Context.EnsureType(kit.Context.Definitions[0]);
    }

    [Test]
    public async Task StringFacets_AreCarriedInDocumentOrderWithTheirPatternsCombined()
    {
        var facets = Facets(
            "xs:string",
            "<xs:pattern value=\"a+\"/><xs:length value=\"3\"/><xs:pattern value=\"b+\"/>"
                + "<xs:enumeration value=\"aaa\"/><xs:enumeration value=\"bbb\"/><xs:whiteSpace value=\"collapse\"/>"
        );

        await Assert.That(facets.Count).IsEqualTo(4);
        await Assert.That(facets[0]).IsTypeOf<PatternFacet>();
        await Assert.That(((PatternFacet)facets[0]).Patterns).IsEquivalentTo(["a+", "b+"]);
        await Assert.That(facets[1]).IsTypeOf<LengthFacet>();
        await Assert.That(facets[2]).IsTypeOf<LexicalEnumerationFacet>();
        await Assert.That(facets[3]).IsTypeOf<WhiteSpaceFacet>();
    }

    [Test]
    [Arguments("preserve")]
    [Arguments("replace")]
    [Arguments("collapse")]
    public async Task WhiteSpace_OfEveryMode_IsCarried(string mode)
    {
        var facets = Facets("xs:string", "<xs:whiteSpace value=\"" + mode + "\"/>");

        await Assert.That(facets.Single()).IsTypeOf<WhiteSpaceFacet>();
    }

    [Test]
    public async Task Length_OfAListIsCountedInItemsAndOfABinaryTypeInOctets()
    {
        var list = Compile(
            Schema(
                "<xs:simpleType name=\"l\"><xs:list itemType=\"xs:int\"/></xs:simpleType>"
                    + "<xs:simpleType name=\"m\"><xs:restriction base=\"t:l\"><xs:minLength value=\"1\"/></xs:restriction></xs:simpleType>"
            )
        );
        var binary = Facets("xs:hexBinary", "<xs:maxLength value=\"4\"/><xs:minLength value=\"1\"/>");
        var base64 = Facets("xs:base64Binary", "<xs:length value=\"4\"/>");

        await Assert
            .That(Get<SimpleTypeDefinition>(list, XsdComponentKind.SimpleType, "m").Facets.Single())
            .IsTypeOf<LengthFacet>();
        await Assert.That(binary.Count).IsEqualTo(2);
        await Assert.That(binary[0]).IsTypeOf<BinaryLengthFacet>();
        await Assert.That(base64.Single()).IsTypeOf<BinaryLengthFacet>();
    }

    [Test]
    public async Task Length_BeyondTheRangeOfAnInt_ClampsAndAValueThatIsNotANumberIsLeftOut()
    {
        var clamped = Facets("xs:string", "<xs:maxLength value=\"99999999999999999999\"/>");
        var left = Facets("xs:string", "<xs:maxLength value=\"x\"/>");

        await Assert.That(((LengthFacet)clamped.Single()).Value).IsEqualTo(int.MaxValue);
        await Assert.That(left).IsEmpty();
    }

    [Test]
    public async Task DigitsAndBounds_OfTheNumericFamilies_AreCarried()
    {
        var decimals = Facets(
            "xs:decimal",
            "<xs:totalDigits value=\"5\"/><xs:fractionDigits value=\"2\"/><xs:minInclusive value=\"1.5\"/><xs:maxExclusive value=\"9\"/>"
        );
        var integers = Facets("xs:integer", "<xs:minExclusive value=\"1\"/><xs:maxInclusive value=\"9\"/>");
        var floats = Facets("xs:float", "<xs:minInclusive value=\"1\"/>");
        var doubles = Facets("xs:double", "<xs:maxInclusive value=\"1e3\"/>");

        await Assert.That(decimals.Count).IsEqualTo(4);
        await Assert.That(decimals[0]).IsTypeOf<TotalDigitsFacet>();
        await Assert.That(decimals[1]).IsTypeOf<FractionDigitsFacet>();
        await Assert.That(decimals[2]).IsTypeOf<BoundFacet<NetEvolve.Schemas.Xml.Datatypes.Decimals.XsdDecimal>>();
        await Assert.That(decimals[3].Kind).IsEqualTo(FacetKind.MaxExclusive);
        await Assert.That(integers.Count).IsEqualTo(2);
        await Assert.That(floats.Single().Kind).IsEqualTo(FacetKind.MinInclusive);
        await Assert.That(doubles.Single().Kind).IsEqualTo(FacetKind.MaxInclusive);
    }

    [Test]
    public async Task Bounds_OfTheTemporalFamilies_AreCarried()
    {
        var duration = Facets("xs:duration", "<xs:minInclusive value=\"P1D\"/>");
        var date = Facets("xs:date", "<xs:minInclusive value=\"2000-01-01Z\"/>");
        var dateTime = Facets("xs:dateTime", "<xs:maxInclusive value=\"2000-01-01T00:00:00Z\"/>");

        await Assert.That(duration.Single().Kind).IsEqualTo(FacetKind.MinInclusive);
        await Assert.That(date.Single().Kind).IsEqualTo(FacetKind.MinInclusive);
        await Assert.That(dateTime.Single().Kind).IsEqualTo(FacetKind.MaxInclusive);
    }

    [Test]
    public async Task Enumeration_OfAnOrderedFamily_IsCarriedAsATypedFacet()
    {
        var integers = Facets("xs:int", "<xs:enumeration value=\"1\"/><xs:enumeration value=\"2\"/>");
        var booleans = Facets("xs:boolean", "<xs:enumeration value=\"true\"/>");

        await Assert.That(integers.Single()).IsTypeOf<EnumerationFacet<XsdInteger>>();
        await Assert.That(booleans.Single()).IsTypeOf<EnumerationFacet<bool>>();
    }

    [Test]
    [Arguments("required", ExplicitTimezoneMode.Required)]
    [Arguments("prohibited", ExplicitTimezoneMode.Prohibited)]
    [Arguments("optional", ExplicitTimezoneMode.Optional)]
    public async Task ExplicitTimezone_OfEveryValue_IsCarried(string value, ExplicitTimezoneMode mode)
    {
        var step = new List<FacetDeclaration>
        {
            new(FacetKind.ExplicitTimezone, value, new DocumentLocation("file:///f.xsd", 1, 1), false),
        };

        var facets = FacetFactory.Create(step, null, XsdVersion.V11, null);

        await Assert.That(facets.Single()).IsTypeOf<ExplicitTimezoneFacet>();
        await Assert.That(((ExplicitTimezoneFacet)facets.Single()).Mode).IsEqualTo(mode);
    }

    [Test]
    public async Task ExplicitTimezone_OfAnUnknownValue_IsLeftOut()
    {
        var step = new List<FacetDeclaration>
        {
            new(FacetKind.ExplicitTimezone, "maybe", new DocumentLocation("file:///f.xsd", 1, 1), false),
        };

        await Assert.That(FacetFactory.Create(step, null, XsdVersion.V11, null)).IsEmpty();
    }

    [Test]
    public async Task Facets_OfAFamilyThatDoesNotCarryThem_AreLeftOut()
    {
        var enumerated = Facets("xs:hexBinary", "<xs:enumeration value=\"0A\"/>");
        var digits = Facets("xs:string", "<xs:totalDigits value=\"2\"/>");
        var bound = Facets("xs:boolean", "<xs:minInclusive value=\"true\"/>");

        await Assert.That(enumerated).IsEmpty();
        await Assert.That(digits).IsEmpty();
        await Assert.That(bound).IsEmpty();
    }

    [Test]
    public async Task ACompiledPatternOfASingleDeclaration_IsCarriedWithoutCompilingAgain()
    {
        PatternFacet.TryCreate(["a+"], XsdVersion.V11, null, out var compiled, out _);
        var step = new List<FacetDeclaration>
        {
            new(FacetKind.Pattern, "a+", new DocumentLocation("file:///f.xsd", 1, 1), false),
        };

        var reused = FacetFactory.Create(step, null, XsdVersion.V11, compiled);
        var rebuilt = FacetFactory.Create(step, null, XsdVersion.V11, null);

        await Assert.That(reused.Single()).IsSameReferenceAs(compiled!);
        await Assert.That(((PatternFacet)rebuilt.Single()).Patterns).IsEquivalentTo(["a+"]);
    }

    [Test]
    public async Task ACompiledPatternIsIgnored_WhenTheStepStatesSeveralPatterns()
    {
        PatternFacet.TryCreate(["a+"], XsdVersion.V11, null, out var compiled, out _);
        var where = new DocumentLocation("file:///f.xsd", 1, 1);
        var step = new List<FacetDeclaration>
        {
            new(FacetKind.Pattern, "a+", where, false),
            new(FacetKind.Pattern, "b+", where, false),
        };

        var facets = FacetFactory.Create(step, null, XsdVersion.V11, compiled);

        await Assert.That(((PatternFacet)facets.Single()).Patterns).IsEquivalentTo(["a+", "b+"]);
    }

    [Test]
    public async Task Create_GuardsItsArguments()
    {
        var kit = new ContextKit("<xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"/></xs:simpleType>");
        kit.Registry.TryGet(
            NetEvolve.Schemas.Xsd.Components.XsdSymbolSpace.Types,
            new NetEvolve.Schemas.XPath.Api.XPathQName(Xs, "int"),
            out var builtIn
        );

        await Assert.That(() => FacetFactory.Create(null!, [], XsdVersion.V10)).Throws<ArgumentNullException>();
        await Assert.That(() => FacetFactory.Create(builtIn!, null!, XsdVersion.V10)).Throws<ArgumentNullException>();
        await Assert.That(FacetFactory.Create(builtIn!, new List<FacetDeclaration>(), XsdVersion.V10)).IsEmpty();
    }
}
