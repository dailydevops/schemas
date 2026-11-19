namespace NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

/// <summary>The rules that tie facets of one type together: each rule has a valid and an invalid schema, and the issue sits on the facet that breaks it.</summary>
public class FacetCombinationTests
{
    private static async Task AssertValid(FacetFixture fixture, FacetCheckResult result)
    {
        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    private static async Task AssertInvalid(FacetFixture fixture, FacetCheckResult result, string rule, int line)
    {
        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf(rule));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(line);
    }

    // length-minLength-maxLength (Datatypes 4.3.1.4)
    [Test]
    public async Task LengthWithMinLengthAndMaxLength_Valid()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("string"),
            FacetFixture.Decl(FacetKind.Length, "5"),
            FacetFixture.Decl(FacetKind.MinLength, "3"),
            FacetFixture.Decl(FacetKind.MaxLength, "8")
        );

        await AssertValid(fixture, result);
    }

    [Test]
    public async Task LengthBelowMinLength_IsAnErrorOnTheMinLength()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("string"),
            FacetFixture.Decl(FacetKind.Length, "5", line: 51),
            FacetFixture.Decl(FacetKind.MinLength, "7", line: 52)
        );

        await AssertInvalid(fixture, result, "length-minLength-maxLength", 52);
    }

    [Test]
    public async Task LengthAboveMaxLength_IsAnErrorOnTheMaxLength()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("string"),
            FacetFixture.Decl(FacetKind.Length, "5", line: 51),
            FacetFixture.Decl(FacetKind.MaxLength, "3", line: 52)
        );

        await AssertInvalid(fixture, result, "length-minLength-maxLength", 52);
    }

    [Test]
    public async Task LengthAgainstTheMaxLengthOfTheBase_IsAnError()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("string", fixture.Facet("string", FacetKind.MaxLength, "5"));

        var valid = fixture.Check(baseType, FacetFixture.Decl(FacetKind.Length, "4"));
        await AssertValid(fixture, valid);

        var invalid = fixture.Check(baseType, FacetFixture.Decl(FacetKind.Length, "8", line: 53));
        await AssertInvalid(fixture, invalid, "length-minLength-maxLength", 53);
    }

    // minLength-less-than-equal-to-maxLength (Datatypes 4.3.2.4)
    [Test]
    public async Task MinLengthNotAboveMaxLength_Valid()
    {
        var fixture = new FacetFixture();

        var below = fixture.Check(
            fixture.Base("string"),
            FacetFixture.Decl(FacetKind.MinLength, "3"),
            FacetFixture.Decl(FacetKind.MaxLength, "5")
        );
        var equal = fixture.Check(
            fixture.Base("string"),
            FacetFixture.Decl(FacetKind.MinLength, "5"),
            FacetFixture.Decl(FacetKind.MaxLength, "5")
        );

        await AssertValid(fixture, below);
        await Assert.That(equal.IsValid).IsTrue();
    }

    [Test]
    public async Task MinLengthAboveMaxLength_IsAnErrorOnTheMinLength()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("string"),
            FacetFixture.Decl(FacetKind.MinLength, "6", line: 54),
            FacetFixture.Decl(FacetKind.MaxLength, "5", line: 55)
        );

        await AssertInvalid(fixture, result, "minLength-less-than-equal-to-maxLength", 54);
    }

    // minInclusive-less-than-equal-to-maxInclusive (Datatypes 4.3.7.4)
    [Test]
    public async Task MinInclusiveNotAboveMaxInclusive_Valid()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.MinInclusive, "1"),
            FacetFixture.Decl(FacetKind.MaxInclusive, "5")
        );
        var equal = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.MinInclusive, "5"),
            FacetFixture.Decl(FacetKind.MaxInclusive, "5")
        );

        await AssertValid(fixture, result);
        await Assert.That(equal.IsValid).IsTrue();
    }

    [Test]
    public async Task MinInclusiveAboveMaxInclusive_IsAnError()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.MinInclusive, "6", line: 56),
            FacetFixture.Decl(FacetKind.MaxInclusive, "5", line: 57)
        );

        await AssertInvalid(fixture, result, "minInclusive-less-than-equal-to-maxInclusive", 56);
    }

    // minExclusive-less-than-equal-to-maxExclusive (Datatypes 4.3.8.4)
    [Test]
    public async Task MinExclusiveBelowMaxExclusive_Valid()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.MinExclusive, "1"),
            FacetFixture.Decl(FacetKind.MaxExclusive, "5")
        );

        await AssertValid(fixture, result);
    }

    [Test]
    public async Task MinExclusiveAboveMaxExclusive_IsAnError()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.MinExclusive, "6", line: 58),
            FacetFixture.Decl(FacetKind.MaxExclusive, "5", line: 59)
        );

        await AssertInvalid(fixture, result, "minExclusive-less-than-equal-to-maxExclusive", 58);
    }

    // minExclusive-less-than-maxInclusive (Datatypes 4.3.9.4)
    [Test]
    public async Task MinExclusiveBelowMaxInclusive_Valid()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.MinExclusive, "4"),
            FacetFixture.Decl(FacetKind.MaxInclusive, "5")
        );

        await AssertValid(fixture, result);
    }

    [Test]
    public async Task MinExclusiveAboveMaxInclusive_IsAnError()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.MinExclusive, "6", line: 60),
            FacetFixture.Decl(FacetKind.MaxInclusive, "5", line: 61)
        );

        await AssertInvalid(fixture, result, "minExclusive-less-than-maxInclusive", 60);
    }

    // minInclusive-less-than-maxExclusive (Datatypes 4.3.10.4)
    [Test]
    public async Task MinInclusiveBelowMaxExclusive_Valid()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.MinInclusive, "4"),
            FacetFixture.Decl(FacetKind.MaxExclusive, "5")
        );

        await AssertValid(fixture, result);
    }

    [Test]
    public async Task MinInclusiveAboveMaxExclusive_IsAnError()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.MinInclusive, "6", line: 62),
            FacetFixture.Decl(FacetKind.MaxExclusive, "5", line: 63)
        );

        await AssertInvalid(fixture, result, "minInclusive-less-than-maxExclusive", 62);
    }

    // maxInclusive-maxExclusive (Datatypes 4.3.8.4) and minInclusive-minExclusive (Datatypes 4.3.9.4)
    [Test]
    public async Task MaxInclusiveAlone_AndMaxExclusiveAlone_AreValid()
    {
        var fixture = new FacetFixture();

        var inclusive = fixture.Check(fixture.Base("decimal"), FacetFixture.Decl(FacetKind.MaxInclusive, "5"));
        var exclusive = fixture.Check(fixture.Base("decimal"), FacetFixture.Decl(FacetKind.MaxExclusive, "5"));

        await AssertValid(fixture, inclusive);
        await Assert.That(exclusive.IsValid).IsTrue();
    }

    [Test]
    public async Task MaxInclusiveWithMaxExclusive_IsAnErrorOnTheExclusiveBound()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.MaxInclusive, "5", line: 64),
            FacetFixture.Decl(FacetKind.MaxExclusive, "5", line: 65)
        );

        await AssertInvalid(fixture, result, "maxInclusive-maxExclusive", 65);
    }

    [Test]
    public async Task MinInclusiveAloneAndMinExclusiveAlone_AreValid()
    {
        var fixture = new FacetFixture();

        var inclusive = fixture.Check(fixture.Base("decimal"), FacetFixture.Decl(FacetKind.MinInclusive, "5"));
        var exclusive = fixture.Check(fixture.Base("decimal"), FacetFixture.Decl(FacetKind.MinExclusive, "5"));

        await AssertValid(fixture, inclusive);
        await Assert.That(exclusive.IsValid).IsTrue();
    }

    [Test]
    public async Task MinInclusiveWithMinExclusive_IsAnErrorOnTheExclusiveBound()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.MinInclusive, "5", line: 66),
            FacetFixture.Decl(FacetKind.MinExclusive, "5", line: 67)
        );

        await AssertInvalid(fixture, result, "minInclusive-minExclusive", 67);
    }

    // fractionDigits-totalDigits (Datatypes 4.3.12.4)
    [Test]
    public async Task FractionDigitsNotAboveTotalDigits_Valid()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.TotalDigits, "5"),
            FacetFixture.Decl(FacetKind.FractionDigits, "3")
        );

        await AssertValid(fixture, result);
    }

    [Test]
    public async Task FractionDigitsAboveTotalDigits_IsAnErrorOnTheFractionDigits()
    {
        var fixture = new FacetFixture();

        var result = fixture.Check(
            fixture.Base("decimal"),
            FacetFixture.Decl(FacetKind.TotalDigits, "3", line: 68),
            FacetFixture.Decl(FacetKind.FractionDigits, "5", line: 69)
        );

        await AssertInvalid(fixture, result, "fractionDigits-totalDigits", 69);
    }

    [Test]
    public async Task FractionDigitsAboveTheTotalDigitsOfTheBase_IsAnError()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("decimal", fixture.Facet("decimal", FacetKind.TotalDigits, "4"));

        var result = fixture.Check(baseType, FacetFixture.Decl(FacetKind.FractionDigits, "5", line: 70));

        await AssertInvalid(fixture, result, "fractionDigits-totalDigits", 70);
    }

    [Test]
    public async Task TheBoundsOfTheStepAgainstTheOppositeBoundOfTheBase_AreChecked()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base("decimal", fixture.Facet("decimal", FacetKind.MaxInclusive, "10"));

        var result = fixture.Check(baseType, FacetFixture.Decl(FacetKind.MinInclusive, "11", line: 71));

        await AssertInvalid(fixture, result, "minInclusive-valid-restriction", 71);
    }

    [Test]
    public async Task TheBoundsAndDigitsOfOneStep_ReportTheFirstViolationOnly()
    {
        var fixture = new FacetFixture();
        var baseType = fixture.Base(
            "decimal",
            fixture.Facet("decimal", FacetKind.MaxInclusive, "10"),
            fixture.Facet("decimal", FacetKind.TotalDigits, "4")
        );

        var result = fixture.Check(
            baseType,
            FacetFixture.Decl(FacetKind.MaxInclusive, "20", line: 72),
            FacetFixture.Decl(FacetKind.TotalDigits, "6", line: 73)
        );

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(72);
    }
}
