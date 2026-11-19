namespace NetEvolve.Schemas.Xsd.Tests.Unit.Override;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Composition.CompositionFixture;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Override.OverrideFixture;

/// <summary>
/// A document that is overridden and redefined: the override is applied first, to the children of the target and of
/// its <c>redefine</c> elements, and the result is processed as a normal schema document (Structures 1.1, 4.2.5, F.2). The
/// same document redefined and non-vacuously overridden gives two conflicting versions (4.2.5, last note).
/// </summary>
public class OverrideRedefineTests
{
    private const string Simple = "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:string\"/></xs:simpleType>";
    private const string SimpleRedefinition =
        "<xs:simpleType name=\"T\"><xs:restriction base=\"T\"><xs:maxLength value=\"3\"/></xs:restriction></xs:simpleType>";
    private const string Group = "<xs:group name=\"G\"><xs:sequence><xs:element name=\"a\"/></xs:sequence></xs:group>";

    private static string Override(string location, string children) =>
        "\n  <xs:override schemaLocation=\"" + location + "\">" + children + "</xs:override>";

    private static string Redefine(string location, string children) =>
        "\n  <xs:redefine schemaLocation=\"" + location + "\">" + children + "</xs:redefine>";

    [Test]
    public async Task Override_OfAChildOfARedefine_WithoutASelfReference_IsAnInvalidRedefinition()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema(Redefine("c.xsd", SimpleRedefinition)))
            .Add("file:///c.xsd", Schema(Simple));

        var (_, issues) = Load(
            Schema(Override("a.xsd", "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:int\"/></xs:simpleType>")),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("src-redefine"));
        await Assert.That(issues[0].Location.Source).IsEqualTo(RootUri);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task Override_OfAChildOfARedefine_WithASelfReference_ReplacesTheRedefinition()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema(Redefine("c.xsd", SimpleRedefinition)))
            .Add("file:///c.xsd", Schema(Simple));
        const string Replacement =
            "<xs:simpleType name=\"T\"><xs:restriction base=\"T\"><xs:maxLength value=\"9\"/></xs:restriction></xs:simpleType>";

        var (set, issues) = Load(Schema(Override("a.xsd", Replacement)), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(ReferenceEquals(Component(set.Units[1], "T"), OverrideChild(set.Units[0], 0, 0))).IsTrue();

        var renamed = set.Units[2].Components[0].FindAttribute("name")!;
        await Assert.That(renamed).IsNotEqualTo("T");
        await Assert.That(Component(set.Units[1], "T").GetChildNode(0).FindAttribute("base")).IsEqualTo(renamed);
    }

    [Test]
    public async Task Override_OfAGroupInARedefine_WithoutASelfReference_IsAccepted()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema(Redefine("c.xsd", Group)))
            .Add("file:///c.xsd", Schema(Group));

        var (set, issues) = Load(
            Schema(
                Override("a.xsd", "<xs:group name=\"G\"><xs:choice><xs:element name=\"b\"/></xs:choice></xs:group>")
            ),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(ReferenceEquals(Component(set.Units[1], "G"), OverrideChild(set.Units[0], 0, 0))).IsTrue();
    }

    [Test]
    public async Task Override_AndRedefine_OfTheSameDocument_GiveTwoConflictingVersions()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(Simple));

        var (set, issues) = Load(
            Schema(
                Redefine("a.xsd", SimpleRedefinition)
                    + Override("a.xsd", "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:int\"/></xs:simpleType>")
            ),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);

        // The redefinition defines T in the redefining unit, and the override defines T in a second copy of the target
        // (the redefined original lives on under a fresh name); the schema holds two definitions of T, which
        // sch-props-correct.2 rejects when the components are built.
        await Assert.That(Names(set.Units[0])).IsEqualTo("simpleType:T");
        await Assert.That(ReferenceEquals(Component(set.Units[2], "T"), OverrideChild(set.Units[0], 0, 0))).IsTrue();
        await Assert.That(set.Units[1].Components[0].FindAttribute("name")).IsNotEqualTo("T");
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Override_ThatChangesNothing_AndRedefine_OfTheSameDocument_ShareOneUnit(bool overrideFirst)
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(Simple));
        var redefine = Redefine("a.xsd", SimpleRedefinition);
        var vacuous = Override("a.xsd", "<xs:element name=\"Unmatched\"/>");

        var (set, issues) = Load(Schema(overrideFirst ? vacuous + redefine : redefine + vacuous), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Override_AndInclude_OfTheSameDocument_GiveTwoConflictingVersions()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(Simple));

        var (set, issues) = Load(
            Schema(
                "<xs:include schemaLocation=\"a.xsd\"/>"
                    + Override("a.xsd", "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:int\"/></xs:simpleType>")
            ),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(Component(set.Units[1], "T").GetChildNode(0).FindAttribute("base")).IsEqualTo("xs:string");
        await Assert.That(ReferenceEquals(Component(set.Units[2], "T"), OverrideChild(set.Units[0], 0, 0))).IsTrue();
    }

    [Test]
    public async Task Override_WithTheDefaultResolver_IsRefusedWithACodedLocatedError()
    {
        var (set, issues) = Load(Schema(Override("a.xsd", Simple)), DenyAllResourceResolver.Instance);

        await Assert.That(set.Units.Count).IsEqualTo(1);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceDenied);
        await Assert.That(issues[0].Location.Source).IsEqualTo(RootUri);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
        await Assert.That(issues[0].Location.Column).IsEqualTo(3);
    }
}
