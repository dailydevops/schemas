namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

/// <summary>
/// A group or attribute group that is redefined without a self-reference must restrict the original (Structures 1.0, 4.2.2,
/// src-redefine 6.2 and 7.2); a redefinition that refers to itself extends the original and is not compared.
/// </summary>
public class RedefineRestrictionTests
{
    private const string Sequence =
        "<xs:sequence><xs:element name=\"a\"/><xs:element name=\"b\" minOccurs=\"0\"/></xs:sequence>";

    private static SchemaCompilation Redefine(string original, string redefinition, XsdVersion version = XsdVersion.V10)
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(original));
        return Compile(
            Schema("<xs:redefine schemaLocation=\"a.xsd\">" + redefinition + "</xs:redefine>"),
            version,
            resolver
        );
    }

    private static string Group(string content) => "<xs:group name=\"g\">" + content + "</xs:group>";

    private static string AttributeGroup(string content) =>
        "<xs:attributeGroup name=\"g\">" + content + "</xs:attributeGroup>";

    [Test]
    [Arguments("<xs:sequence><xs:element name=\"a\"/></xs:sequence>")]
    [Arguments("<xs:sequence><xs:element name=\"a\"/><xs:element name=\"b\" minOccurs=\"0\"/></xs:sequence>")]
    public async Task RedefinedGroupWithoutSelfReference_ThatRestrictsTheOriginal_IsValid(string redefinition)
    {
        ArgumentNullException.ThrowIfNull(redefinition);

        var compilation = Redefine(Group(Sequence), Group(redefinition));

        await Assert.That(Describe(compilation)).IsEqualTo(string.Empty);
        await Assert.That(compilation.IsValid).IsTrue();
    }

    [Test]
    [Arguments("<xs:sequence><xs:element name=\"a\"/><xs:element name=\"c\"/></xs:sequence>")]
    [Arguments("<xs:sequence><xs:element name=\"b\" minOccurs=\"0\"/></xs:sequence>")]
    public async Task RedefinedGroupWithoutSelfReference_ThatDoesNotRestrictTheOriginal_IsReported(string redefinition)
    {
        ArgumentNullException.ThrowIfNull(redefinition);

        var compilation = Redefine(Group(Sequence), Group(redefinition));

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert.That(compilation.Result.Issues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task RedefinedGroupWithSelfReference_IsNotComparedWithTheOriginal()
    {
        var compilation = Redefine(
            Group(Sequence),
            Group("<xs:sequence><xs:group ref=\"t:g\"/><xs:element name=\"c\"/></xs:sequence>")
        );

        await Assert.That(Describe(compilation)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task RedefinedGroup_InXsd11_IsNotComparedWithTheOriginal()
    {
        var compilation = Redefine(
            Group(Sequence),
            Group("<xs:sequence><xs:element name=\"c\"/></xs:sequence>"),
            XsdVersion.V11
        );

        await Assert.That(compilation.Result.Issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("<xs:attribute name=\"x\" type=\"xs:int\" use=\"required\"/>")]
    [Arguments(
        "<xs:attribute name=\"x\" type=\"xs:int\" use=\"required\"/><xs:attribute name=\"y\" type=\"xs:short\"/>"
    )]
    [Arguments(
        "<xs:attribute name=\"x\" type=\"xs:int\" use=\"required\"/><xs:attribute name=\"y\" type=\"xs:int\" use=\"prohibited\"/>"
    )]
    [Arguments("<xs:attribute name=\"x\" type=\"xs:int\" use=\"required\"/><xs:anyAttribute namespace=\"##local\"/>")]
    public async Task RedefinedAttributeGroup_ThatRestrictsTheOriginal_IsValid(string redefinition)
    {
        ArgumentNullException.ThrowIfNull(redefinition);

        var original = AttributeGroup(
            "<xs:attribute name=\"x\" type=\"xs:int\" use=\"required\"/><xs:attribute name=\"y\" type=\"xs:int\"/><xs:anyAttribute namespace=\"##any\"/>"
        );
        var compilation = Redefine(original, AttributeGroup(redefinition));

        await Assert.That(Describe(compilation)).IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments("<xs:attribute name=\"y\" type=\"xs:int\"/>", "3")]
    [Arguments("<xs:attribute name=\"x\" type=\"xs:int\"/>", "2.1.1")]
    [Arguments("<xs:attribute name=\"x\" type=\"xs:string\" use=\"required\"/>", "2.1.2")]
    [Arguments("<xs:attribute name=\"x\" type=\"xs:int\" use=\"required\" fixed=\"7\"/>", "2.1.3")]
    [Arguments(
        "<xs:attribute name=\"x\" type=\"xs:int\" use=\"required\" fixed=\"5\"/><xs:attribute name=\"z\"/>",
        "2.2"
    )]
    [Arguments(
        "<xs:attribute name=\"x\" type=\"xs:int\" use=\"required\" fixed=\"5\"/><xs:anyAttribute namespace=\"##local\"/>",
        "4.2"
    )]
    public async Task RedefinedAttributeGroup_ThatDoesNotRestrictTheOriginal_IsReported(
        string redefinition,
        string clause
    )
    {
        ArgumentNullException.ThrowIfNull(redefinition);
        ArgumentNullException.ThrowIfNull(clause);

        var original = AttributeGroup(
            "<xs:attribute name=\"x\" type=\"xs:int\" use=\"required\" fixed=\"5\"/><xs:attribute name=\"y\" type=\"xs:int\"/><xs:anyAttribute namespace=\"##other\"/>"
        );
        var compilation = Redefine(original, AttributeGroup(redefinition));

        await Assert.That(compilation.Result.Issues.Count).IsEqualTo(1);
        await Assert.That(compilation.Result.Issues[0].Code).IsEqualTo(XsdRuleCodes.SrcRedefine);
        await Assert.That(compilation.Result.Issues[0].Constraint).IsEqualTo("src-redefine.7.2.2");
        await Assert.That(compilation.Result.Issues[0].Message).Contains("derivation-ok-restriction." + clause);
    }

    [Test]
    public async Task RedefinedAttributeGroup_WithAWeakerWildcard_IsReported()
    {
        var compilation = Redefine(
            AttributeGroup("<xs:anyAttribute processContents=\"strict\"/>"),
            AttributeGroup("<xs:anyAttribute processContents=\"lax\"/>")
        );

        await Assert.That(compilation.Result.Issues[0].Message).Contains("derivation-ok-restriction.4.3");
    }

    [Test]
    public async Task RedefinedAttributeGroup_WithAWildcardTheOriginalLacks_IsReported()
    {
        var compilation = Redefine(
            AttributeGroup("<xs:attribute name=\"x\"/>"),
            AttributeGroup("<xs:attribute name=\"x\"/><xs:anyAttribute/>")
        );

        await Assert.That(compilation.Result.Issues[0].Message).Contains("derivation-ok-restriction.4.1");
    }

    [Test]
    public async Task RedefinedAttributeGroupWithSelfReference_IsNotComparedWithTheOriginal()
    {
        var compilation = Redefine(
            AttributeGroup("<xs:attribute name=\"x\"/>"),
            AttributeGroup("<xs:attributeGroup ref=\"t:g\"/><xs:attribute name=\"z\"/>")
        );

        await Assert.That(Describe(compilation)).IsEqualTo(string.Empty);
    }
}
