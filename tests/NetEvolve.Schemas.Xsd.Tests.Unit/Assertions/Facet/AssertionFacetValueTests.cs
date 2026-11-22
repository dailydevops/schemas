namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.Facet;

using System;
using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

/// <summary>The assertion facet decides on the value alone: an atomic value, the items of a list, and the value of the accepting union member.</summary>
public class AssertionFacetValueTests
{
    [Test]
    [Arguments("4", true)]
    [Arguments("-10", true)]
    [Arguments("3", false)]
    [Arguments("0", true)]
    public async Task AnAssertionOnAnAtomicIntegerType_PassesAndFailsByValue(string text, bool expected)
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("integer", "$value mod 2 = 0");

        var holds = fixture.Check(type, text);

        await Assert.That(holds).IsEqualTo(expected);
        await Assert.That(fixture.Issues.Count).IsEqualTo(expected ? 0 : 1);
    }

    [Test]
    public async Task AFailedAssertion_IsAValidationIssueAtTheValue_ThatNamesTheTest()
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("integer", "$value > 5");

        _ = fixture.Check(type, "3");

        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("cvc-assertions-valid"));
        await Assert.That(fixture.Issues[0].Location.Line).IsEqualTo(FacetAssertionFixture.Value.Line);
        await Assert.That(fixture.Issues[0].Message).Contains("$value > 5");
    }

    [Test]
    public async Task TheValueOfAnAtomicType_HasTheTypeOfTheBuiltInAncestor()
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("integer", "$value instance of xs:integer");

        var holds = fixture.Check(type, " 7 ");

        await Assert.That(holds).IsTrue();
    }

    [Test]
    [Arguments("1 2 3", true)]
    [Arguments("1 2", false)]
    [Arguments("1 2 3 4", false)]
    public async Task ForAListType_TheValueIsASequence_AndCountWorks(string text, bool expected)
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("list:integer", "count($value) = 3");

        var holds = fixture.Check(type, text);

        await Assert.That(holds).IsEqualTo(expected);
    }

    [Test]
    public async Task ForAListType_EveryItemIsTyped_AndTheSequenceCanBeAggregated()
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base(
            "list:integer",
            "(every $i in $value satisfies $i instance of xs:integer) and sum($value) = 6"
        );

        await Assert.That(fixture.Check(type, "1 2 3")).IsTrue();
        await Assert.That(fixture.Check(type, "1 2 4")).IsFalse();
    }

    [Test]
    public async Task ForAnEmptyList_TheValueIsTheEmptySequence()
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("list:integer", "empty($value)");

        await Assert.That(fixture.Check(type, string.Empty)).IsTrue();
        await Assert.That(fixture.Check(type, "1")).IsFalse();
    }

    [Test]
    [Arguments("5", "integer", "string")]
    [Arguments("abc", "string", "integer")]
    public async Task ForAUnionType_TheValueHasTheTypeOfTheMatchingMember(string text, string member, string other)
    {
        using var fixture = new FacetAssertionFixture();
        var test = "$value instance of xs:" + member + " and not($value instance of xs:" + other + ")";
        var type = fixture.Base("union:integer+string", test);

        var holds = fixture.Check(type, text);

        await Assert.That(holds).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AnAssertionOfAnotherType_DoesNotConstrainTheType()
    {
        using var fixture = new FacetAssertionFixture();
        _ = fixture.Base("integer", "false()");
        var other = fixture.Base("string");

        await Assert.That(fixture.Check(other, "5")).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ATypeWithoutAssertionFacets_HoldsAndReportsNothing()
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("integer");

        await Assert.That(fixture.Check(type, "3")).IsTrue();
        await Assert.That(fixture.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SeveralAssertionFacets_ReportEveryFailureOnce()
    {
        using var fixture = new FacetAssertionFixture();
        var type = fixture.Base("integer", "$value > 10", "$value < 0");

        var holds = fixture.Check(type, "5");

        await Assert.That(holds).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(2);
    }
}
