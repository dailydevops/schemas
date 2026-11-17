namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Contracts;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Contracts.Fakes;

public class DatatypeContractTests
{
    private static FakeDatatypeDescriptor V10() => new(XsdVersion.V10);

    private static FakeDatatypeDescriptor V11() => new(XsdVersion.V11);

    [Test]
    public async Task Parse_ValidLexical_ReturnsTheValue()
    {
        var result = V10().Parse("042");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
        await Assert.That(result.Error).IsEqualTo(DatatypeError.None);
    }

    [Test]
    public async Task Parse_InvalidLexical_ReturnsACodedErrorWithOffset()
    {
        var invalid = V10().Parse("12x");
        var capped = V10().Parse("1234567890");

        await Assert.That(invalid.Success).IsFalse();
        await Assert.That(invalid.Value).IsEqualTo(0);
        await Assert.That(invalid.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(invalid.ErrorOffset).IsEqualTo(2);
        await Assert.That(capped.Error).IsEqualTo(DatatypeError.DigitCap);
    }

    [Test]
    public async Task ParseResult_Fail_RejectsANegativeOffset() =>
        await Assert
            .That(() => new ParseResult<int>(DatatypeError.InvalidCharacter, -1))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task DatatypeError_HasTheStableCodesOfTheDesign()
    {
        await Assert.That(Enum.GetName((DatatypeError)0)).IsEqualTo("None");
        await Assert.That(Enum.GetName((DatatypeError)1)).IsEqualTo("InvalidLexicalForm");
        await Assert.That(Enum.GetName((DatatypeError)2)).IsEqualTo("FacetViolation");
        await Assert.That(Enum.GetName((DatatypeError)3)).IsEqualTo("InvalidListItem");
        await Assert.That(Enum.GetName((DatatypeError)4)).IsEqualTo("NoUnionMember");
        await Assert.That(Enum.GetName((DatatypeError)5)).IsEqualTo("DivisionByZero");
        await Assert.That(Enum.GetName((DatatypeError)6)).IsEqualTo("Overflow");
        await Assert.That(Enum.GetName((DatatypeError)7)).IsEqualTo("InvalidNumericOperand");
        await Assert.That(Enum.GetName((DatatypeError)10)).IsEqualTo("DigitCap");
        await Assert.That(Enum.GetName((DatatypeError)11)).IsEqualTo("YearOrComponentCap");
        await Assert.That(Enum.GetName((DatatypeError)13)).IsEqualTo("InvalidCharacter");
        await Assert.That(Enum.GetValues<DatatypeError>().Length).IsEqualTo(11);
    }

    [Test]
    public async Task Validate_ValidLexical_ReturnsNoError()
    {
        var result = V10().Validate("7");

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.None);
        await Assert.That(result.FailedFacet).IsEqualTo(FacetKind.None);
    }

    [Test]
    public async Task Validate_InvalidLexical_ReportsErrorAndOffset()
    {
        var result = V10().Validate("1!");

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Error).IsEqualTo(DatatypeError.InvalidCharacter);
        await Assert.That(result.Offset).IsEqualTo(1);
    }

    [Test]
    public async Task Validate_FacetViolation_ReportsTheFailedFacet()
    {
        var descriptor = new FakeDatatypeDescriptor(
            XsdVersion.V10,
            null,
            DatatypeVariety.Atomic,
            new FakeMaxInclusiveFacet(5)
        );

        await Assert.That(descriptor.Validate("5").IsValid).IsTrue();
        await Assert.That(descriptor.Validate("6").FailedFacet).IsEqualTo(FacetKind.MaxInclusive);
        await Assert.That(descriptor.Facets.Count).IsEqualTo(1);
    }

    [Test]
    public async Task LexicalFacet_IsEvaluatedOverASpan()
    {
        var facet = new FakeLengthFacet(2);

        await Assert.That(facet.Kind).IsEqualTo(FacetKind.MaxLength);
        await Assert.That(facet.IsSatisfiedBy("ab")).IsTrue();
        await Assert.That(facet.IsSatisfiedBy("abc")).IsFalse();
    }

    [Test]
    public async Task DefaultResults_AreNeverValid()
    {
        await Assert.That(default(ValidationResult).IsValid).IsFalse();
        await Assert.That(default(ParseResult<int>).Success).IsFalse();
    }

    [Test]
    public async Task ValidationResult_Invalid_RejectsANegativeOffset() =>
        await Assert
            .That(() => ValidationResult.Invalid(DatatypeError.InvalidCharacter, FacetKind.None, -1))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Compare_ReturnsLessEqualGreaterOrIncomparable()
    {
        var descriptor = V10();

        await Assert.That(descriptor.Compare(1, 2)).IsEqualTo(XsdOrder.Less);
        await Assert.That(descriptor.Compare(2, 2)).IsEqualTo(XsdOrder.Equal);
        await Assert.That(descriptor.Compare(3, 2)).IsEqualTo(XsdOrder.Greater);
        await Assert.That(descriptor.Compare(int.MinValue, 2)).IsEqualTo(XsdOrder.Incomparable);
        await Assert.That(descriptor.Compare(2, int.MinValue)).IsEqualTo(XsdOrder.Incomparable);
        await Assert.That(descriptor.Compare(int.MinValue, int.MinValue)).IsEqualTo(XsdOrder.Incomparable);
    }

    [Test]
    public async Task IsIdentical_IsSeparateFromCompare()
    {
        var descriptor = V11();

        await Assert.That(descriptor.IsIdentical(2, 2)).IsTrue();
        await Assert.That(descriptor.IsIdentical(2, 3)).IsFalse();
        await Assert.That(descriptor.IsIdentical(int.MinValue, int.MinValue)).IsFalse();
    }

    [Test]
    public async Task LexicalMode_DistinguishesTheTwoVersionsForTheSameInput()
    {
        var v10 = V10();
        var v11 = V11();

        await Assert.That(v10.LexicalMode).IsEqualTo(XsdVersion.V10);
        await Assert.That(v11.LexicalMode).IsEqualTo(XsdVersion.V11);
        await Assert.That(v10.Validate("+5").IsValid).IsFalse();
        await Assert.That(v11.Validate("+5").IsValid).IsTrue();
        await Assert.That(v10.Validate("5").IsValid).IsTrue();
        await Assert.That(v11.Validate("5").IsValid).IsTrue();
    }

    [Test]
    public async Task Descriptor_ExposesVarietyBaseTypeAndOrdering()
    {
        var baseType = V10();
        var list = new FakeDatatypeDescriptor(XsdVersion.V11, baseType, DatatypeVariety.List);

        await Assert.That(list.Variety).IsEqualTo(DatatypeVariety.List);
        await Assert.That(list.BaseType).IsSameReferenceAs(baseType);
        await Assert.That(baseType.BaseType).IsNull();
        await Assert.That(baseType.Variety).IsEqualTo(DatatypeVariety.Atomic);
        await Assert.That(baseType.Name).IsEqualTo("fakeInteger");
        await Assert.That(baseType.IsOrdered).IsTrue();
        await Assert.That(Enum.GetValues<DatatypeVariety>().Length).IsEqualTo(3);
    }

    [Test]
    public async Task FacetKind_ListsTheTwelveFacetsOfXsd10AndTheTwoOfXsd11()
    {
        string[] expected =
        [
            "Length",
            "MinLength",
            "MaxLength",
            "Pattern",
            "Enumeration",
            "WhiteSpace",
            "MaxInclusive",
            "MaxExclusive",
            "MinExclusive",
            "MinInclusive",
            "TotalDigits",
            "FractionDigits",
            "Assertions",
            "ExplicitTimezone",
        ];
        var names = new List<string>(Enum.GetNames<FacetKind>());
        names.Remove("None");

        await Assert.That(names).IsEquivalentTo(expected);
        await Assert.That(Enum.GetName((FacetKind)0)).IsEqualTo("None");
    }

    [Test]
    public async Task XsdOrder_HasFourOutcomes() => await Assert.That(Enum.GetValues<XsdOrder>().Length).IsEqualTo(4);

    [Test]
    public async Task TextRange_SlicesTheCallersSpanAndRejectsNegativeValues()
    {
        var range = new TextRange(1, 2);

        await Assert.That(range.Offset).IsEqualTo(1);
        await Assert.That(range.Length).IsEqualTo(2);
        await Assert.That(range.Slice("abcd").ToString()).IsEqualTo("bc");
        await Assert.That(() => new TextRange(-1, 0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new TextRange(0, -1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Signatures_AcceptReadOnlySpanOfCharAndNeverAString()
    {
        var spanType = typeof(ReadOnlySpan<char>);
        var spanMethods = 0;
        var failures = new List<string>();

        foreach (
            var type in new[] { typeof(IDatatypeDescriptor<int>), typeof(IDatatypeDescriptor), typeof(ILexicalFacet) }
        )
        {
            foreach (var method in type.GetMethods())
            {
                var parameters = method.GetParameters();
                if (Array.Exists(parameters, p => p.ParameterType == spanType))
                {
                    spanMethods++;
                }

                if (Array.Exists(parameters, p => p.ParameterType == typeof(string)))
                {
                    failures.Add(type.Name + "." + method.Name);
                }
            }
        }

        await Assert.That(failures).IsEmpty();

        // Parse (generic), Validate (non-generic), IsSatisfiedBy (lexical facet).
        await Assert.That(spanMethods).IsEqualTo(3);
    }
}
