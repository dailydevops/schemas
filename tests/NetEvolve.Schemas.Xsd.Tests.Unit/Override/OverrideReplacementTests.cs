namespace NetEvolve.Schemas.Xsd.Tests.Unit.Override;

using System;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Composition.CompositionFixture;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Override.OverrideFixture;

/// <summary>
/// The overriding definition replaces the matching one of the target document, in the place of the original
/// (Structures 1.1, 4.2.5 and F.2); a child that matches nothing is ignored.
/// </summary>
public class OverrideReplacementTests
{
    private const string A = "file:///a.xsd";

    private static string Override(string children) =>
        "\n  <xs:override schemaLocation=\"a.xsd\">" + children + "</xs:override>";

    [Test]
    [Arguments(
        "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:string\"/></xs:simpleType>",
        "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:int\"/></xs:simpleType>",
        "simpleType"
    )]
    [Arguments(
        "<xs:complexType name=\"T\"><xs:sequence><xs:element name=\"a\"/></xs:sequence></xs:complexType>",
        "<xs:complexType name=\"T\"><xs:sequence><xs:element name=\"b\"/></xs:sequence></xs:complexType>",
        "complexType"
    )]
    [Arguments(
        "<xs:group name=\"T\"><xs:sequence><xs:element name=\"a\"/></xs:sequence></xs:group>",
        "<xs:group name=\"T\"><xs:choice><xs:element name=\"b\"/></xs:choice></xs:group>",
        "group"
    )]
    [Arguments(
        "<xs:attributeGroup name=\"T\"><xs:attribute name=\"a\"/></xs:attributeGroup>",
        "<xs:attributeGroup name=\"T\"><xs:attribute name=\"b\"/></xs:attributeGroup>",
        "attributeGroup"
    )]
    [Arguments("<xs:element name=\"T\" type=\"xs:string\"/>", "<xs:element name=\"T\" type=\"xs:int\"/>", "element")]
    [Arguments(
        "<xs:attribute name=\"T\" type=\"xs:string\"/>",
        "<xs:attribute name=\"T\" type=\"xs:int\"/>",
        "attribute"
    )]
    [Arguments("<xs:notation name=\"T\" public=\"a\"/>", "<xs:notation name=\"T\" public=\"b\"/>", "notation")]
    public async Task Override_ReplacesTheComponentOfTheTarget_InItsPlace(
        string original,
        string replacement,
        string kind
    )
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(replacement);
        ArgumentNullException.ThrowIfNull(kind);

        var (set, issues) = Load(
            Schema(Override(replacement)),
            new MapResolver().Add(A, Schema("<xs:element name=\"Z\"/>" + original + "<xs:element name=\"Y\"/>"))
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
        await Assert.That(Names(set.Units[0])).IsEqualTo(string.Empty);
        await Assert.That(Names(set.Units[1])).IsEqualTo("element:Z," + kind + ":T,element:Y");
        await Assert.That(ReferenceEquals(set.Units[1].Components[1], OverrideChild(set.Units[0], 0, 0))).IsTrue();
        await Assert.That(set.Units[0].Included.Contains(set.Units[1])).IsTrue();
    }

    [Test]
    public async Task Override_ChildrenThatMatchNothing_AreIgnored()
    {
        var (set, issues) = Load(
            Schema(
                Override(
                    "<xs:simpleType name=\"U\"><xs:restriction base=\"xs:string\"/></xs:simpleType>"
                        + "<xs:element name=\"T\"/>"
                )
            ),
            new MapResolver().Add(
                A,
                Schema("<xs:simpleType name=\"T\"><xs:restriction base=\"xs:string\"/></xs:simpleType>")
            )
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
        await Assert.That(Names(set.Units[0])).IsEqualTo(string.Empty);
        await Assert.That(Names(set.Units[1])).IsEqualTo("simpleType:T");
        await Assert.That(Component(set.Units[1], "T").Name.LocalName).IsEqualTo("simpleType");
        await Assert.That(Component(set.Units[1], "T").GetChildNode(0).FindAttribute("base")).IsEqualTo("xs:string");
    }

    [Test]
    public async Task Override_WithoutAChild_IsAnInclude()
    {
        var (set, issues) = Load(
            Schema("<xs:override schemaLocation=\"a.xsd\"/>"),
            new MapResolver().Add(A, Schema("<xs:element name=\"T\"/>"))
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
        await Assert.That(Names(set.Units[1])).IsEqualTo("element:T");
    }

    [Test]
    public async Task Override_OfAChameleonDocument_TakesTheNamespaceOfTheOverridingDocument()
    {
        var (set, issues) = Load(
            Schema(Override("<xs:element name=\"T\" type=\"xs:int\"/>"), "urn:x"),
            new MapResolver().Add(A, Schema("<xs:element name=\"T\" type=\"xs:string\"/>"))
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
        await Assert.That(set.Units[1].EffectiveNamespace).IsEqualTo("urn:x");
        await Assert.That(ReferenceEquals(set.Units[1].Components[0], OverrideChild(set.Units[0], 0, 0))).IsTrue();
    }

    [Test]
    public async Task Override_OfADocumentWithAnotherTargetNamespace_IsASchemaError_AtTheDirective()
    {
        var (set, issues) = Load(
            Schema(Override("<xs:element name=\"T\"/>"), "urn:x"),
            new MapResolver().Add(A, Schema("<xs:element name=\"T\"/>", "urn:other"))
        );

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-override"));
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Override_InADocumentWithoutNamespace_OfADocumentWithOne_IsASchemaError()
    {
        var (set, issues) = Load(
            Schema(Override("<xs:element name=\"T\"/>")),
            new MapResolver().Add(A, Schema("<xs:element name=\"T\"/>", "urn:other"))
        );

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-override"));
    }

    [Test]
    public async Task Override_PutsTheReplacementInTheUnitOfTheOverriddenDocument_WhichCarriesItsDefaults()
    {
        var (set, issues) = Load(
            Schema(Override("<xs:element name=\"T\" type=\"xs:int\"/>")),
            new MapResolver().Add(
                A,
                "<xs:schema xmlns:xs=\""
                    + Xs
                    + "\" elementFormDefault=\"qualified\"><xs:element name=\"T\"/></xs:schema>"
            )
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units[0].Root.FindAttribute("elementFormDefault")).IsNull();
        await Assert.That(set.Units[1].Root.FindAttribute("elementFormDefault")).IsEqualTo("qualified");
        await Assert.That(ReferenceEquals(set.Units[1].Components[0], OverrideChild(set.Units[0], 0, 0))).IsTrue();
    }
}
