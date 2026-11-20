namespace NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion;

using static NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion.ConditionalInclusionFixture;

/// <summary>vc:facetAvailable and vc:facetUnavailable test the names against the supported facets (Structures 1.1, 4.2.2).</summary>
public class FacetAvailabilityTests
{
    [Test]
    [Arguments("facetAvailable", "xs:pattern", "a")]
    [Arguments("facetAvailable", "xs:minInclusive xs:maxLength xs:whiteSpace", "a")]
    [Arguments("facetAvailable", "xs:explicitTimezone", "a")]
    [Arguments("facetAvailable", "xs:foo", "")]
    [Arguments("facetAvailable", "xs:Pattern", "")]
    [Arguments("facetAvailable", "xs:pattern xs:foo", "")]
    [Arguments("facetAvailable", "xs:string", "")]
    [Arguments("facetAvailable", "", "a")]
    [Arguments("facetUnavailable", "xs:pattern", "")]
    [Arguments("facetUnavailable", "xs:length xs:enumeration", "")]
    [Arguments("facetUnavailable", "xs:foo", "a")]
    [Arguments("facetUnavailable", "xs:pattern xs:foo", "a")]
    [Arguments("facetUnavailable", "", "")]
    public async Task FacetAttribute_KeepsOrPrunesTheElement(string attribute, string value, string expected)
    {
        var (root, issues) = Run(Element("vc:" + attribute + "=\"" + value + "\""));

        await Assert.That(Children(root!)).IsEqualTo(expected);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    // The assertion facet is not supported before #321, so it counts as unavailable and its absence selects the fallback.
    [Test]
    public async Task TheAssertionFacet_IsUnavailableUntilItIsSupported()
    {
        var (available, _) = Run(Element("vc:facetAvailable=\"xs:assertion\""));
        var (unavailable, _) = Run(Element("vc:facetUnavailable=\"xs:assertion\""));

        await Assert.That(available!.ChildCount).IsEqualTo(0);
        await Assert.That(Children(unavailable!)).IsEqualTo("a");
    }

    [Test]
    public async Task AFacetNameIsNotATypeName()
    {
        var (root, _) = Run(Element("vc:typeAvailable=\"xs:pattern\""));

        await Assert.That(root!.ChildCount).IsEqualTo(0);
    }
}
