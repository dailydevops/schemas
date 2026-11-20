namespace NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.ConditionalInclusion.ConditionalInclusionFixture;

/// <summary>
/// A version control attribute whose value is not valid is a schema error (src-cip, Structures 1.1, 4.2.2). Decision: the
/// attribute cannot be evaluated, so it excludes nothing and the element is kept.
/// </summary>
public class InvalidValueTests
{
    [Test]
    [Arguments("vc:minVersion=\"abc\"")]
    [Arguments("vc:minVersion=\"\"")]
    [Arguments("vc:minVersion=\"1.1e0\"")]
    [Arguments("vc:minVersion=\"1,1\"")]
    [Arguments("vc:minVersion=\"1.1 2\"")]
    [Arguments("vc:maxVersion=\".\"")]
    [Arguments("vc:maxVersion=\"INF\"")]
    [Arguments("vc:typeAvailable=\"xs:string bad:name\"")]
    [Arguments("vc:typeAvailable=\"nope:string\"")]
    [Arguments("vc:typeUnavailable=\"1bad\"")]
    [Arguments("vc:typeUnavailable=\"a:b:c\"")]
    [Arguments("vc:facetAvailable=\"xs:pattern :x\"")]
    [Arguments("vc:facetUnavailable=\"nope:pattern\"")]
    public async Task AnInvalidValue_IsReportedAsSrcCipAtTheElementAndTheElementIsKept(string attribute)
    {
        var (root, issues) = Run(Schema("<xs:element name=\"a\" type=\"xs:string\" " + attribute + "/>"));

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.SrcCip);
        await Assert.That(issues[0].Location.Line).IsEqualTo(2);
        await Assert.That(issues[0].Location.Column).IsEqualTo(1);
    }

    [Test]
    public async Task TwoInvalidValuesOnOneElement_AreReportedOnceEach()
    {
        var (root, issues) = Run(Element("vc:minVersion=\"x\" vc:typeAvailable=\"nope:string\""));

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.SrcCip);
        await Assert.That(issues[1].Code).IsEqualTo(XsdRuleCodes.SrcCip);
    }

    [Test]
    public async Task AnInvalidValueNextToAValidAttributeThatExcludes_IsNotReportedBecauseTheElementIsGone()
    {
        var (root, issues) = Run(Element("vc:minVersion=\"x\" vc:maxVersion=\"1.0\""));

        await Assert.That(root!.ChildCount).IsEqualTo(0);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AnInvalidValueNextToAValidAttributeThatDoesNotExclude_IsStillReported()
    {
        var (root, issues) = Run(Element("vc:minVersion=\"x\" vc:maxVersion=\"2.0\""));

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task APrefixDeclaredOnAnExcludedElement_IsNotInScopeForItsSiblings()
    {
        var (root, issues) = Run(
            Schema(
                "<xs:element name=\"a\" xmlns:t=\"http://www.w3.org/2001/XMLSchema\" vc:minVersion=\"3.2\"/>"
                    + "<xs:element name=\"b\" vc:typeAvailable=\"t:string\"/>"
            )
        );

        await Assert.That(Children(root!)).IsEqualTo("b");
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.SrcCip);
    }

    [Test]
    public async Task AnInvalidVersionAttributeOfAnExcludedRoot_StaysInTheDocumentAndIsReported()
    {
        var (root, issues) = Run(
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:vc=\""
                + Vc
                + "\" vc:minVersion=\"x\" vc:maxVersion=\"1.0\"><xs:element name=\"a\"/></xs:schema>"
        );

        await Assert.That(root!.ChildCount).IsEqualTo(0);
        await Assert.That(root.AttributeCount).IsEqualTo(2);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.SrcCip);
    }

    [Test]
    public async Task AnUnknownVersionControlAttribute_IsNeitherEvaluatedNorReported()
    {
        var (root, issues) = Run(Element("vc:futureAttribute=\"3.2\""));

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task InStrict10Mode_AnInvalidValueIsIgnored()
    {
        var (root, issues) = Run(Element("vc:minVersion=\"x\""), XsdVersion.V10);

        await Assert.That(Children(root!)).IsEqualTo("a");
        await Assert.That(issues.Count).IsEqualTo(0);
    }
}
