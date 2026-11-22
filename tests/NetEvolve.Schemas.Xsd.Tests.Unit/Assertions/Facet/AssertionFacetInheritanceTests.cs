namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.Facet;

/// <summary>A derived type holds its own assertion facets and those it inherits from the base types.</summary>
public class AssertionFacetInheritanceTests
{
    [Test]
    [Arguments("5", 0)]
    [Arguments("-1", 1)]
    [Arguments("12", 1)]
    [Arguments("-20", 1)]
    public async Task ADerivedType_ChecksItsOwnAndTheInheritedAssertionFacets(string text, int issues)
    {
        using var fixture = new FacetAssertionFixture();
        var baseType = fixture.Base("integer", "$value > 0");
        var derived = fixture.Derive(baseType, "$value < 10");

        var holds = fixture.Check(derived, text);

        await Assert.That(holds).IsEqualTo(issues == 0);
        await Assert.That(fixture.Issues.Count).IsEqualTo(issues);
    }

    [Test]
    public async Task AValueThatBreaksBoth_ReportsTheInheritedAssertionFirst()
    {
        using var fixture = new FacetAssertionFixture();
        var baseType = fixture.Base("integer", "$value > 100");
        var derived = fixture.Derive(baseType, "$value < 0");

        _ = fixture.Check(derived, "50");

        await Assert.That(fixture.Issues.Count).IsEqualTo(2);
        await Assert.That(fixture.Issues[0].Message).Contains("$value > 100");
        await Assert.That(fixture.Issues[1].Message).Contains("$value < 0");
    }

    [Test]
    public async Task TheBaseType_IsNotConstrainedByTheAssertionOfTheDerivedType()
    {
        using var fixture = new FacetAssertionFixture();
        var baseType = fixture.Base("integer", "$value > 0");
        _ = fixture.Derive(baseType, "$value < 10");

        await Assert.That(fixture.Check(baseType, "50")).IsTrue();
    }
}
