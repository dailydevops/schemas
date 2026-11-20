namespace NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Declarations11;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11.SyntaxFixture;

public class SubstitutionGroupListTests
{
    private static readonly SchemaNamespaceScope Scope = new("urn:s", false);

    private static string Element(string attributes) =>
        WithNamespace + "  ^<xs:element name=\"e\" type=\"xs:string\" " + attributes + "/>" + EndOfSchema;

    [Test]
    public async Task Read_Version11_ReturnsEveryHeadInListOrder()
    {
        var (root, _, _) = ParseMarked(Element("substitutionGroup=\"t:h1 b:h2 t:h3\""));
        var reporter = Reporter(out var collector);

        var heads = SubstitutionGroupList.Read(root.GetChildNode(0), Scope, XsdVersion.V11, reporter);

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
        await Assert.That(heads.Length).IsEqualTo(3);
        await Assert.That(heads[0]).IsEqualTo(new XPathQName("urn:test", "h1"));
        await Assert.That(heads[1]).IsEqualTo(new XPathQName("urn:b", "h2"));
        await Assert.That(heads[2]).IsEqualTo(new XPathQName("urn:test", "h3"));
    }

    [Test]
    public async Task Read_AnyWhiteSpaceSeparatesTheHeads_AndADuplicateCountsOnce()
    {
        var (root, _, _) = ParseMarked(Element("substitutionGroup=\"  t:h1&#9;t:h2&#10;t:h1  \""));
        var reporter = Reporter(out var collector);

        var heads = SubstitutionGroupList.Read(root.GetChildNode(0), Scope, XsdVersion.V11, reporter);

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
        await Assert.That(heads.Length).IsEqualTo(2);
        await Assert.That(heads[1]).IsEqualTo(new XPathQName("urn:test", "h2"));
    }

    [Test]
    public async Task Read_OneHead_ReportsNothingInBothVersions()
    {
        var (root, _, _) = ParseMarked(Element("substitutionGroup=\"t:h1\""));
        var reporter = Reporter(out var collector);

        var strict = SubstitutionGroupList.Read(root.GetChildNode(0), Scope, XsdVersion.V10, reporter);
        var lax = SubstitutionGroupList.Read(root.GetChildNode(0), Scope, XsdVersion.V11, reporter);

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
        await Assert.That(strict.Length).IsEqualTo(1);
        await Assert.That(lax.Length).IsEqualTo(1);
    }

    [Test]
    public async Task Read_NoAttribute_ReturnsNoHeads()
    {
        var (root, _, _) = ParseMarked(Element(""));
        var reporter = Reporter(out var collector);

        var heads = SubstitutionGroupList.Read(root.GetChildNode(0), Scope, XsdVersion.V11, reporter);

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
        await Assert.That(heads.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Read_Strict10_SeveralHeadsAreALocatedSchemaError_AndOnlyTheFirstIsKept()
    {
        var (root, line, column) = ParseMarked(Element("substitutionGroup=\"t:h1 t:h2\""));
        var reporter = Reporter(out var collector);

        var heads = SubstitutionGroupList.Read(root.GetChildNode(0), Scope, XsdVersion.V10, reporter);

        var issues = Issues(collector);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.Strict10SubstitutionGroupList);
        await Assert.That(issues[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(issues[0].Location.Column).IsEqualTo(column);
        await Assert.That(heads.Length).IsEqualTo(1);
        await Assert.That(heads[0]).IsEqualTo(new XPathQName("urn:test", "h1"));
    }

    [Test]
    public async Task Read_AnUndeclaredPrefix_IsASchemaErrorAndTheHeadIsLeftOut()
    {
        var (root, line, _) = ParseMarked(Element("substitutionGroup=\"t:h1 q:h2\""));
        var reporter = Reporter(out var collector);

        var heads = SubstitutionGroupList.Read(root.GetChildNode(0), Scope, XsdVersion.V11, reporter);

        var issues = Issues(collector);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.SrcResolve);
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(heads.Length).IsEqualTo(1);
    }

    [Test]
    public async Task Read_AnUnprefixedHead_ResolvesIntoTheTargetNamespaceOfAChameleon()
    {
        var (root, _, _) = ParseMarked(Element("substitutionGroup=\"h1 h2\""));
        var reporter = Reporter(out var collector);

        var heads = SubstitutionGroupList.Read(
            root.GetChildNode(0),
            new SchemaNamespaceScope("urn:in", true),
            XsdVersion.V11,
            reporter
        );

        await Assert.That(Issues(collector).Count).IsEqualTo(0);
        await Assert.That(heads[0]).IsEqualTo(new XPathQName("urn:in", "h1"));
        await Assert.That(heads[1]).IsEqualTo(new XPathQName("urn:in", "h2"));
    }

    [Test]
    public async Task Read_NullArguments_Throw()
    {
        var (root, _, _) = ParseMarked(Element(""));
        var reporter = Reporter(out _);
        var element = root.GetChildNode(0);

        await Assert
            .That(() => SubstitutionGroupList.Read(null!, Scope, XsdVersion.V11, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => SubstitutionGroupList.Read(element, Scope, XsdVersion.V11, null!))
            .Throws<ArgumentNullException>();
    }
}
