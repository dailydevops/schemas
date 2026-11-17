namespace NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Composition;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Composition.CompositionFixture;

/// <summary>
/// Redefine is applied on the syntax model (design #146 DP-3): the original keeps living under a fresh name that only the
/// redefinition refers to, and the redefinition takes over the name in the redefining unit.
/// </summary>
public class RedefineTests
{
    private const string BaseSimple = "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:string\"/></xs:simpleType>";
    private const string BaseComplex =
        "<xs:complexType name=\"T\"><xs:sequence><xs:element name=\"a\"/></xs:sequence></xs:complexType>";
    private const string BaseGroup =
        "<xs:group name=\"T\"><xs:sequence><xs:element name=\"a\"/></xs:sequence></xs:group>";
    private const string BaseAttributeGroup =
        "<xs:attributeGroup name=\"T\"><xs:attribute name=\"a\"/></xs:attributeGroup>";

    private static string Redefine(string child) =>
        "\n  <xs:redefine schemaLocation=\"a.xsd\">" + child + "</xs:redefine>";

    private static (SchemaDocumentSet Set, IReadOnlyList<ValidationIssue> Issues) Run(
        string original,
        string redefinition
    ) => Load(Schema(Redefine(redefinition)), new MapResolver().Add("file:///a.xsd", Schema(original)));

    [Test]
    [Arguments(
        BaseSimple,
        "<xs:simpleType name=\"T\"><xs:restriction base=\"T\"><xs:maxLength value=\"3\"/></xs:restriction></xs:simpleType>",
        "simpleType",
        "restriction"
    )]
    [Arguments(
        BaseComplex,
        "<xs:complexType name=\"T\"><xs:complexContent><xs:extension base=\"T\"><xs:attribute name=\"x\"/></xs:extension></xs:complexContent></xs:complexType>",
        "complexType",
        "complexContent/extension"
    )]
    [Arguments(
        BaseComplex,
        "<xs:complexType name=\"T\"><xs:complexContent><xs:restriction base=\"T\"><xs:sequence><xs:element name=\"a\"/></xs:sequence></xs:restriction></xs:complexContent></xs:complexType>",
        "complexType",
        "complexContent/restriction"
    )]
    [Arguments(
        BaseGroup,
        "<xs:group name=\"T\"><xs:sequence><xs:group ref=\"T\"/><xs:element name=\"b\"/></xs:sequence></xs:group>",
        "group",
        "sequence/group"
    )]
    [Arguments(
        BaseAttributeGroup,
        "<xs:attributeGroup name=\"T\"><xs:attributeGroup ref=\"T\"/><xs:attribute name=\"b\"/></xs:attributeGroup>",
        "attributeGroup",
        "attributeGroup"
    )]
    public async Task Redefine_ReplacesTheComponent_AndPointsTheSelfReferenceAtTheRenamedOriginal(
        string original,
        string redefinition,
        string kind,
        string path
    )
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(redefinition);
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(path);

        var (set, issues) = Run(original, redefinition);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);

        var redefining = set.Units[0].Components;
        await Assert.That(redefining.Count).IsEqualTo(1);
        await Assert.That(redefining[0].Name.LocalName).IsEqualTo(kind);
        await Assert.That(redefining[0].FindAttribute("name")).IsEqualTo("T");

        var renamed = set.Units[1].Components;
        await Assert.That(renamed.Count).IsEqualTo(1);
        var newName = renamed[0].FindAttribute("name");
        await Assert.That(newName).IsNotEqualTo("T");
        await Assert.That(newName![0] == 'T').IsTrue();

        var selfReference = redefining[0];
        foreach (var step in path.Split('/'))
        {
            SchemaSyntaxNode? next = null;
            for (var i = 0; i < selfReference.ChildCount && next is null; i++)
            {
                if (string.Equals(selfReference.GetChildNode(i).Name.LocalName, step, StringComparison.Ordinal))
                {
                    next = selfReference.GetChildNode(i);
                }
            }

            selfReference = next!;
        }

        var value = selfReference.FindAttribute(kind is "group" or "attributeGroup" ? "ref" : "base");
        await Assert.That(value!.EndsWith(newName, StringComparison.Ordinal)).IsTrue();
        await Assert.That(value.Length == newName.Length || value.StartsWith("t:", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Redefine_LeavesOtherComponentsOfTheRedefinedDocumentUntouched()
    {
        var original = BaseSimple + "<xs:element name=\"e\"/>";
        var (set, issues) = Run(original, "<xs:simpleType name=\"T\"><xs:restriction base=\"T\"/></xs:simpleType>");

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Names(set.Units[1]).EndsWith(",element:e", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Redefine_OfAChameleonDocument_UsesTheNamespaceOfTheRedefiningDocument()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(BaseSimple));

        var (set, issues) = Load(
            Schema(Redefine("<xs:simpleType name=\"T\"><xs:restriction base=\"t:T\"/></xs:simpleType>"), "urn:root"),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units[1].EffectiveNamespace).IsEqualTo("urn:root");
        await Assert.That(set.Units[0].Components.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments(BaseSimple, "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:string\"/></xs:simpleType>")]
    [Arguments(BaseSimple, "<xs:simpleType name=\"T\"><xs:restriction base=\"U\"/></xs:simpleType>")]
    [Arguments(BaseSimple, "<xs:simpleType name=\"T\"><xs:list itemType=\"xs:string\"/></xs:simpleType>")]
    [Arguments(BaseComplex, "<xs:complexType name=\"T\"><xs:sequence/></xs:complexType>")]
    [Arguments(
        BaseComplex,
        "<xs:complexType name=\"T\"><xs:complexContent><xs:extension base=\"xs:anyType\"/></xs:complexContent></xs:complexType>"
    )]
    [Arguments(
        BaseGroup,
        "<xs:group name=\"T\"><xs:sequence><xs:group ref=\"T\"/><xs:group ref=\"T\"/></xs:sequence></xs:group>"
    )]
    [Arguments(
        BaseGroup,
        "<xs:group name=\"T\"><xs:sequence><xs:group ref=\"T\" maxOccurs=\"2\"/></xs:sequence></xs:group>"
    )]
    [Arguments(
        BaseAttributeGroup,
        "<xs:attributeGroup name=\"T\"><xs:attributeGroup ref=\"T\"/><xs:attributeGroup ref=\"T\"/></xs:attributeGroup>"
    )]
    public async Task Redefine_WithAnInvalidSelfReference_IsASchemaError_AtTheRedefinedComponent(
        string original,
        string redefinition
    )
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(redefinition);

        var (_, issues) = Run(original, redefinition);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-redefine"));
        await Assert.That(issues[0].Location.Source).IsEqualTo(RootUri);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    [Arguments(BaseGroup, "<xs:group name=\"T\"><xs:sequence><xs:element name=\"a\"/></xs:sequence></xs:group>")]
    [Arguments(BaseAttributeGroup, "<xs:attributeGroup name=\"T\"><xs:attribute name=\"a\"/></xs:attributeGroup>")]
    public async Task Redefine_OfAGroupWithoutSelfReference_IsAccepted_ItIsARestrictionOfTheOriginal(
        string original,
        string redefinition
    )
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(redefinition);

        var (set, issues) = Run(original, redefinition);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units[0].Components.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Redefine_OfAComponentTheDocumentDoesNotDefine_IsASchemaError()
    {
        var (set, issues) = Run(
            BaseSimple,
            "<xs:simpleType name=\"Other\"><xs:restriction base=\"Other\"/></xs:simpleType>"
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-expredef"));
        await Assert.That(set.Units[0].Components.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Redefine_FindsTheOriginalInADocumentIncludedByTheRedefinedOne()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema("<xs:include schemaLocation=\"b.xsd\"/>"))
            .Add("file:///b.xsd", Schema(BaseSimple));

        var (set, issues) = Load(
            Schema(Redefine("<xs:simpleType name=\"T\"><xs:restriction base=\"T\"/></xs:simpleType>")),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(set.Units[2].Components[0].FindAttribute("name")).IsNotEqualTo("T");
    }

    [Test]
    public async Task Redefine_ChainedRedefinitions_RenameEachOriginalToADistinctName()
    {
        var resolver = new MapResolver()
            .Add(
                "file:///a.xsd",
                Schema(
                    "<xs:redefine schemaLocation=\"b.xsd\"><xs:simpleType name=\"T\"><xs:restriction base=\"T\"/></xs:simpleType></xs:redefine>"
                )
            )
            .Add("file:///b.xsd", Schema(BaseSimple));

        var (set, issues) = Load(
            Schema(Redefine("<xs:simpleType name=\"T\"><xs:restriction base=\"T\"/></xs:simpleType>")),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        var first = set.Units[2].Components[0].FindAttribute("name");
        var second = set.Units[1].Components[0].FindAttribute("name");
        await Assert.That(first).IsNotEqualTo("T");
        await Assert.That(second).IsNotEqualTo("T");
        await Assert.That(first).IsNotEqualTo(second);
        await Assert.That(set.Units[0].Components[0].FindAttribute("name")).IsEqualTo("T");
    }
}
