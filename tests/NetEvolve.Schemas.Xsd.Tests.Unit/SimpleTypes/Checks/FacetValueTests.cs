namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Facets.Core;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

/// <summary>The values of the facets: enumeration and pattern values in the base value space, malformed values, repeated facets, and the guards.</summary>
public class FacetValueTests
{
    [Test]
    [Arguments("int", "5")]
    [Arguments("int", "-2147483648")]
    [Arguments("byte", "127")]
    [Arguments("string", "anything at all")]
    [Arguments("boolean", "true")]
    [Arguments("decimal", "1.50")]
    [Arguments("date", "2020-01-01")]
    [Arguments("QName", "a:b")]
    [Arguments("list:int", "1 2 3")]
    [Arguments("union:int+boolean", "true")]
    [Arguments("union:int+boolean", "7")]
    public async Task AnEnumerationValueInTheBaseValueSpace_IsValid(string type, string value)
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base(type), FacetFixture.Decl(FacetKind.Enumeration, value));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("int", "abc")]
    [Arguments("int", "2147483648")]
    [Arguments("byte", "300")]
    [Arguments("boolean", "maybe")]
    [Arguments("decimal", "1,5")]
    [Arguments("date", "2020-13-45")]
    [Arguments("list:int", "1 x 3")]
    [Arguments("union:int+boolean", "yes")]
    public async Task AnEnumerationValueOutsideTheBaseValueSpace_IsALocatedSchemaError(string type, string value)
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base(type), FacetFixture.Decl(FacetKind.Enumeration, value, line: 81));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("enumeration-valid-restriction"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(81);
    }

    [Test]
    public async Task EveryEnumerationValueIsChecked_EachInvalidOneIsReportedAtItsOwnElement()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("int"),
            FacetFixture.Decl(FacetKind.Enumeration, "1", line: 82),
            FacetFixture.Decl(FacetKind.Enumeration, "x", line: 83),
            FacetFixture.Decl(FacetKind.Enumeration, "3", line: 84),
            FacetFixture.Decl(FacetKind.Enumeration, "y", line: 85)
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(2);
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(83);
        await Assert.That(fixture.Issues[1].Location.Line).IsEqualTo(85);
    }

    [Test]
    public async Task AnEnumerationValueIsCheckedAgainstTheBuiltInBoundsOfTheBase()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("unsignedByte");

        var inside = fixture.Check(baseType, FacetFixture.Decl(FacetKind.Enumeration, "255"));
        var outside = fixture.Check(baseType, FacetFixture.Decl(FacetKind.Enumeration, "256", line: 86));

        await Assert.That(inside.IsValid).IsTrue();
        await Assert.That(outside.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(86);
    }

    [Test]
    [Arguments("string", "[a-z]+")]
    [Arguments("string", "\\d{3}-\\d{4}")]
    [Arguments("string", "a|b|c")]
    [Arguments("string", "")]
    [Arguments("int", "[0-9]+")]
    [Arguments("boolean", "true|false")]
    public async Task APatternThatCompiles_IsValid(string type, string pattern)
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base(type), FacetFixture.Decl(FacetKind.Pattern, pattern));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("(")]
    [Arguments("[a-")]
    [Arguments("a{3,1}")]
    [Arguments("\\")]
    [Arguments("*")]
    public async Task APatternThatDoesNotCompile_IsALocatedSchemaError(string pattern)
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base("string"), FacetFixture.Decl(FacetKind.Pattern, pattern, line: 91));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("src-pattern-value"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(91);
    }

    [Test]
    public async Task SeveralPatternsAndEnumerationsInOneStep_AreAllowed()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("string"),
            FacetFixture.Decl(FacetKind.Pattern, "[a-z]+"),
            FacetFixture.Decl(FacetKind.Pattern, "[0-9]+"),
            FacetFixture.Decl(FacetKind.Enumeration, "a"),
            FacetFixture.Decl(FacetKind.Enumeration, "1")
        );

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(FacetKind.MaxLength, "abc", "string")]
    [Arguments(FacetKind.MaxLength, "-1", "string")]
    [Arguments(FacetKind.MinLength, "", "string")]
    [Arguments(FacetKind.Length, "1.5", "string")]
    [Arguments(FacetKind.Length, "+", "hexBinary")]
    [Arguments(FacetKind.TotalDigits, "0", "decimal")]
    [Arguments(FacetKind.TotalDigits, "x", "decimal")]
    [Arguments(FacetKind.FractionDigits, "-1", "decimal")]
    [Arguments(FacetKind.WhiteSpace, "bogus", "string")]
    [Arguments(FacetKind.WhiteSpace, "Collapse", "string")]
    [Arguments(FacetKind.MaxInclusive, "abc", "decimal")]
    [Arguments(FacetKind.MinInclusive, "", "int")]
    [Arguments(FacetKind.MaxExclusive, "1.5", "int")]
    [Arguments(FacetKind.MinInclusive, "2020", "dateTime")]
    [Arguments(FacetKind.MaxInclusive, "1 day", "duration")]
    [Arguments(FacetKind.MaxInclusive, "ten", "float")]
    public async Task AFacetValueThatIsNotARepresentationOfTheFacet_IsALocatedSchemaError(
        FacetKind kind,
        string value,
        string type
    )
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base(type), FacetFixture.Decl(kind, value, line: 101));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("st-restrict-facets"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(101);
    }

    [Test]
    [Arguments("0", "string")]
    [Arguments("007", "string")]
    [Arguments("2147483648", "string")]
    [Arguments("99999999999999999999", "string")]
    public async Task AMaxLengthThatIsANonNegativeInteger_IsValidEvenBeyondTheIntRange(string value, string type)
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base(type), FacetFixture.Decl(FacetKind.MaxLength, value));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(FacetKind.MaxLength, "string")]
    [Arguments(FacetKind.MinLength, "string")]
    [Arguments(FacetKind.Length, "string")]
    [Arguments(FacetKind.WhiteSpace, "string")]
    [Arguments(FacetKind.MaxInclusive, "decimal")]
    [Arguments(FacetKind.MinExclusive, "decimal")]
    [Arguments(FacetKind.TotalDigits, "decimal")]
    [Arguments(FacetKind.FractionDigits, "decimal")]
    public async Task ASingleValuedFacetStatedTwice_IsALocatedSchemaErrorOnTheSecond(FacetKind kind, string type)
    {
        var fixture = new FacetFixture();
        var value = kind == FacetKind.WhiteSpace ? "collapse" : "5";

        var result = fixture.Check(
            fixture.Base(type),
            FacetFixture.Decl(kind, value, line: 111),
            FacetFixture.Decl(kind, value, line: 112)
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("src-single-facet-value"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(112);
    }

    [Test]
    public async Task NotationRequiresAnEnumeration()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("NOTATION");

        var without = fixture.Check(baseType);
        var withLength = fixture.Check(baseType, FacetFixture.Decl(FacetKind.MaxLength, "5"));
        var with = fixture.Check(baseType, FacetFixture.Decl(FacetKind.Enumeration, "png"));

        await Assert.That(without.IsValid).IsFalse();
        await Assert.That(withLength.IsValid).IsFalse();
        await Assert.That(with.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(2);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("enumeration-required-notation"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(3);
        await Assert.That(fixture.Issues[1].Code).IsEqualTo(FacetFixture.CodeOf("enumeration-required-notation"));
    }

    [Test]
    public async Task ATypeDerivedFromNotationThatAlreadyEnumerates_NeedsNoSecondEnumeration()
    {
        var fixture = new FacetFixture();
        var enumerated = fixture.Base("NOTATION", new LexicalEnumerationFacet(["png"]));

        var result = fixture.Check(enumerated, FacetFixture.Decl(FacetKind.MaxLength, "5"));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ABaseThatIsNotASimpleType_IsLeftToTheCallerAndReportsNothing()
    {
        var fixture = new FacetFixture();
        var complex = new FakeComponent(NetEvolve.Schemas.Xsd.Contracts.XsdComponentKind.ComplexType, "c");

        var result = fixture.Check(complex, FacetFixture.Decl(FacetKind.MaxLength, "5"));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ABaseWhoseRootIsNotABuiltInType_IsLeftToTheCallerAndReportsNothing()
    {
        var fixture = new FacetFixture();
        var unknown = NetEvolve.Schemas.Xsd.SimpleTypes.Definitions.SimpleTypeDefinition.FromDatatype(
            new NetEvolve.Schemas.XPath.Api.XPathQName("urn:other", "mystery"),
            FacetFixture.Where,
            fixture.Descriptor("string")
        );

        var result = fixture.Check(unknown, FacetFixture.Decl(FacetKind.MinInclusive, "5"));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AnEmptyStep_IsValidAndKeepsTheFixedFacetsOfTheBase()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(fixture.Base("string"), FacetFixture.Fixed(FacetKind.MaxLength));

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Fixed.Contains(FacetKind.MaxLength)).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TheCheck_BehavesTheSameInXsd10()
    {
        var fixture = new FacetFixture(XsdVersion.V10);
        var baseType = fixture.Base("decimal", fixture.Facet("decimal", FacetKind.MaxInclusive, "10"));

        var narrowed = fixture.Check(baseType, FacetFixture.Decl(FacetKind.MaxInclusive, "5"));
        var widened = fixture.Check(baseType, FacetFixture.Decl(FacetKind.MaxInclusive, "20", line: 121));

        await Assert.That(narrowed.IsValid).IsTrue();
        await Assert.That(widened.IsValid).IsFalse();
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(121);
    }

    private static FacetCheckResult Run(
        FacetFixture fixture,
        IXsdComponent? baseType = null,
        IReadOnlyList<FacetDeclaration>? step = null,
        bool noAssessor = false,
        bool noReporter = false,
        XsdVersion? version = null
    ) =>
        SimpleTypeFacetCheck.Check(
            baseType ?? fixture.Base("string"),
            default,
            step ?? [],
            FacetFixture.Where,
            noAssessor ? null! : fixture.Assessor,
            version ?? fixture.Version,
            noReporter ? null! : fixture.Reporter
        );

    [Test]
    public async Task TheGuards_RejectNullArguments()
    {
        var fixture = new FacetFixture();

        await Assert
            .That(() =>
                SimpleTypeFacetCheck.Check(
                    null!,
                    default,
                    [],
                    FacetFixture.Where,
                    fixture.Assessor,
                    fixture.Version,
                    fixture.Reporter
                )
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                SimpleTypeFacetCheck.Check(
                    fixture.Base("string"),
                    default,
                    null!,
                    FacetFixture.Where,
                    fixture.Assessor,
                    fixture.Version,
                    fixture.Reporter
                )
            )
            .Throws<ArgumentNullException>();
        await Assert.That(() => Run(fixture, noAssessor: true)).Throws<ArgumentNullException>();
        await Assert.That(() => Run(fixture, noReporter: true)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task TheGuards_RejectAVersionThatIsNotDefined()
    {
        var fixture = new FacetFixture();

        await Assert.That(() => Run(fixture, version: (XsdVersion)7)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => Run(fixture, version: (XsdVersion)(-1))).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TheDeclarationGuards_RejectAFacetThatIsNoFacetAndANullValue()
    {
        await Assert
            .That(() => new FacetDeclaration(FacetKind.None, "1", FacetFixture.Where))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => new FacetDeclaration((FacetKind)99, "1", FacetFixture.Where))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => new FacetDeclaration(FacetKind.Length, null!, FacetFixture.Where))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ADeclaration_KeepsWhatTheSchemaStates()
    {
        var declaration = new FacetDeclaration(FacetKind.MaxLength, "5", FacetFixture.Where, true);

        await Assert.That(declaration.Kind).IsEqualTo(FacetKind.MaxLength);
        await Assert.That(declaration.Value).IsEqualTo("5");
        await Assert.That(declaration.Location).IsEqualTo(FacetFixture.Where);
        await Assert.That(declaration.IsFixed).IsTrue();
        await Assert.That(new FacetDeclaration(FacetKind.Length, "1", FacetFixture.Where).IsFixed).IsFalse();
    }

    [Test]
    public async Task TheFixedSetOfAResult_IsTheOneOfTheCallerWhenNothingIsSeeded()
    {
        var set = FacetFixture.Fixed(FacetKind.Length, FacetKind.Pattern);
        var result = new FacetCheckResult(true, set);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Fixed.Contains(FacetKind.Length)).IsTrue();
        await Assert.That(result.Fixed.Contains(FacetKind.MaxLength)).IsFalse();
    }
}
