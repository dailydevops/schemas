namespace NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion.ConditionalInclusionFixture;

/// <summary>
/// An attribute of the version control namespace other than the six known ones is reported as a warning, once per attribute
/// (Structures 1.1, 4.2.2), and never excludes or removes the element.
/// </summary>
public class UnknownAttributeTests
{
    [Test]
    public async Task AnUnknownAttributeOnAKeptElement_IsWarnedAtTheElementAndTheElementIsKept()
    {
        var (root, issues) = Run(Schema("<xs:element name=\"a\" type=\"xs:string\" vc:other=\"x\"/>"));

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.UnknownVersionControlAttribute);
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Warning);
        await Assert.That(issues[0].Location.Line).IsEqualTo(2);
        await Assert.That(issues[0].Location.Column).IsEqualTo(1);
    }

    [Test]
    public async Task TwoUnknownAttributes_AreWarnedOnceEach()
    {
        var (root, issues) = Run(Element("vc:one=\"1\" vc:two=\"2\""));

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.UnknownVersionControlAttribute);
        await Assert.That(issues[1].Code).IsEqualTo(XsdRuleCodes.UnknownVersionControlAttribute);
    }

    [Test]
    public async Task AnUnknownAttributeNextToAKnownOne_KeepsTheKnownBehavior()
    {
        var (root, issues) = Run(Element("vc:minVersion=\"1.1\" vc:other=\"x\""));

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.UnknownVersionControlAttribute);
    }

    [Test]
    public async Task AnUnknownAttributeOnAnExcludedElement_IsNotReported()
    {
        var (root, issues) = Run(Element("vc:minVersion=\"9.0\" vc:other=\"x\""));

        await Assert.That(root!.ChildCount).IsEqualTo(0);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AnUnknownAttributeOnAnExcludedRoot_IsNotReported()
    {
        var (root, issues) = Run(
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:vc=\""
                + Vc
                + "\" vc:minVersion=\"9.0\" vc:other=\"x\"><xs:element name=\"a\"/></xs:schema>"
        );

        await Assert.That(root!.ChildCount).IsEqualTo(0);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AnUnknownAttributeOnAKeptRoot_IsWarned()
    {
        var (_, issues) = Run(
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:vc=\"" + Vc + "\" vc:other=\"x\"/>"
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.UnknownVersionControlAttribute);
    }

    [Test]
    public async Task AnUnknownAttributeUnderXsd10_IsNotReported()
    {
        var (root, issues) = Run(Element("vc:other=\"x\""), XsdVersion.V10);

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(issues.Count).IsEqualTo(0);
    }
}
