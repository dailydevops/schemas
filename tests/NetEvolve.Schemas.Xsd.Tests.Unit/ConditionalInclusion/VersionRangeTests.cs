namespace NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion;

using static NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion.ConditionalInclusionFixture;

/// <summary>vc:minVersion and vc:maxVersion are compared with the processor version 1.1 (Structures 1.1, 4.2.2).</summary>
public class VersionRangeTests
{
    [Test]
    [Arguments("1.2")]
    [Arguments("2")]
    [Arguments("3.2")]
    [Arguments("1.1000001")]
    [Arguments("+1.2")]
    [Arguments(" 1.2 ")]
    public async Task MinVersionAbove11_PrunesTheElement(string value)
    {
        var (root, issues) = Run(Element("vc:minVersion=\"" + value + "\""));

        await Assert.That(root!.ChildCount).IsEqualTo(0);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("1.1")]
    [Arguments("1.10")]
    [Arguments("1.1000")]
    [Arguments("1.0")]
    [Arguments("1")]
    [Arguments("0.5")]
    [Arguments("-3")]
    [Arguments(".5")]
    public async Task MinVersionAtOrBelow11_KeepsTheElement(string value)
    {
        var (root, issues) = Run(Element("vc:minVersion=\"" + value + "\""));

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("1.1")]
    [Arguments("1.10")]
    [Arguments("1.0")]
    [Arguments("1.")]
    [Arguments("1")]
    [Arguments("0")]
    [Arguments("-1.5")]
    public async Task MaxVersionAtOrBelow11_PrunesTheElement(string value)
    {
        var (root, issues) = Run(Element("vc:maxVersion=\"" + value + "\""));

        await Assert.That(root!.ChildCount).IsEqualTo(0);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("1.2")]
    [Arguments("1.1000001")]
    [Arguments("2")]
    [Arguments("3.2")]
    public async Task MaxVersionAbove11_KeepsTheElement(string value)
    {
        var (root, issues) = Run(Element("vc:maxVersion=\"" + value + "\""));

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("1.0", "1.2", "a")]
    [Arguments("1.1", "3.2", "a")]
    [Arguments("1.2", "3.2", "")]
    [Arguments("1.0", "1.1", "")]
    [Arguments("1.1", "1.1", "")]
    [Arguments("2.0", "1.0", "")]
    public async Task MinAndMaxVersionTogether_KeepTheElementOnlyInsideTheRange(
        string minVersion,
        string maxVersion,
        string expected
    )
    {
        var (root, issues) = Run(Element("vc:minVersion=\"" + minVersion + "\" vc:maxVersion=\"" + maxVersion + "\""));

        await Assert.That(Children(root!)).IsEqualTo(expected);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TwoDeclarationsForOneName_KeepOnlyTheOneThatMatchesTheProcessor()
    {
        var (root, issues) = Run(
            Schema(
                "<xs:element name=\"e\" type=\"xs:int\" vc:minVersion=\"3.2\"/>"
                    + "<xs:element name=\"e\" type=\"xs:string\" vc:minVersion=\"1.1\" vc:maxVersion=\"3.2\"/>"
            )
        );

        await Assert.That(root!.ChildCount).IsEqualTo(1);
        await Assert.That(root.GetChildNode(0).FindAttribute("type")).IsEqualTo("xs:string");
        await Assert.That(issues.Count).IsEqualTo(0);
    }
}
