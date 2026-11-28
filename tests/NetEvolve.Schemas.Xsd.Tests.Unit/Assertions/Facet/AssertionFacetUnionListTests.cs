namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.Facet;

using NetEvolve.Schemas.Xsd.Tests.Unit.SimpleTypes.Checks;

/// <summary>The assertions of the item type of a list and of the member types of a union are checked with the list or union (#949).</summary>
public class AssertionFacetUnionListTests
{
    [Test]
    [Arguments("7", false)]
    [Arguments("3", true)]
    [Arguments("abc", true)]
    public async Task Assertion_Union_PicksTheMemberThatSatisfiesItsAssertions(string text, bool expected)
    {
        using var fixture = new FacetAssertionFixture();
        var big = fixture.Base("integer", "$value > 5");

        // "3" is a valid integer, but not for the member with the assertion, so the second member accepts it and $value is an xs:string.
        var union = fixture.UnionOf([big, fixture.Types.Wrapped("string")], "$value instance of xs:string");

        await Assert.That(fixture.Check(union, text)).IsEqualTo(expected);
        await Assert.That(fixture.Issues.Count).IsEqualTo(expected ? 0 : 1);
    }

    [Test]
    public async Task Assertion_Union_WithNoMemberThatHolds_IsOneIssue()
    {
        using var fixture = new FacetAssertionFixture();
        var big = fixture.Base("integer", "$value > 5");
        var long3 = fixture.Base("string", "string-length($value) > 3");
        var union = fixture.UnionOf([big, long3]);

        var holds = fixture.Check(union, "3");

        await Assert.That(holds).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
        await Assert.That(fixture.Issues[0].Code).IsEqualTo(FacetFixture.CodeOf("cvc-assertions-valid"));
        await Assert.That(fixture.Issues[0].Message).Contains("any member type");
    }

    [Test]
    [Arguments("6 7", true, 0)]
    [Arguments("6 3 2", false, 2)]
    [Arguments("", true, 0)]
    public async Task Assertion_ListItems_CheckTheAssertionsOfTheItemType(string text, bool expected, int issues)
    {
        using var fixture = new FacetAssertionFixture();
        var list = fixture.ListOf(fixture.Base("integer", "$value > 5"));

        var holds = fixture.Check(list, text);

        await Assert.That(holds).IsEqualTo(expected);
        await Assert.That(fixture.Issues.Count).IsEqualTo(issues);
    }

    [Test]
    public async Task Assertion_ListOfUnion_ChecksTheMemberOfEveryItem_AndTheOwnAssertion()
    {
        using var fixture = new FacetAssertionFixture();
        var union = fixture.UnionOf([fixture.Base("integer", "$value > 5"), fixture.Types.Wrapped("string")]);
        var list = fixture.ListOf(union, "count($value) = 2");

        await Assert.That(fixture.Check(list, "7 x")).IsTrue();
        await Assert.That(fixture.Check(list, "7 x y")).IsFalse();
        await Assert.That(fixture.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Assertion_ListAndUnionWithoutAnyAssertion_AreNotEvaluated()
    {
        using var fixture = new FacetAssertionFixture();
        var plain = fixture.Types.Wrapped("integer");
        var list = fixture.ListOf(plain);
        var union = fixture.UnionOf([plain, fixture.Types.Wrapped("string")]);
        var table = fixture.Table();

        await Assert.That(table.Reaches(list)).IsFalse();
        await Assert.That(table.Reaches(union)).IsFalse();
        await Assert.That(fixture.Check(list, "1 2")).IsTrue();
        await Assert.That(fixture.Check(union, "x")).IsTrue();
    }

    [Test]
    public async Task Reaches_FollowsTheItemTypeAndTheMembers()
    {
        using var fixture = new FacetAssertionFixture();
        var asserting = fixture.Base("integer", "$value > 5");
        var list = fixture.ListOf(asserting);
        var union = fixture.UnionOf([fixture.Types.Wrapped("string"), asserting]);
        var table = fixture.Table();

        await Assert.That(table.Reaches(asserting)).IsTrue();
        await Assert.That(table.Reaches(list)).IsTrue();
        await Assert.That(table.Reaches(union)).IsTrue();
        await Assert.That(table.Reaches(fixture.Types.Wrapped("string"))).IsFalse();
    }
}
