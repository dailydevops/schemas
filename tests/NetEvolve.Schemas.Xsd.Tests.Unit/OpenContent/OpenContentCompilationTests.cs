namespace NetEvolve.Schemas.Xsd.Tests.Unit.OpenContent;

using System.Linq;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

/// <summary>The builder compiles <c>openContent</c> and <c>defaultOpenContent</c> into complex types (XSD-78, #1424).</summary>
public class OpenContentCompilationTests
{
    private const string Ab =
        "<xs:sequence><xs:element name=\"a\" type=\"xs:string\"/><xs:element name=\"b\" type=\"xs:string\"/></xs:sequence>";

    private static string Mode(string mode) =>
        "<xs:openContent mode=\""
        + mode
        + "\"><xs:any namespace=\"##other\" processContents=\"lax\"/></xs:openContent>";

    private static string DefaultOpen(string attributes) =>
        "<xs:defaultOpenContent"
        + attributes
        + "><xs:any namespace=\"##other\" processContents=\"lax\"/></xs:defaultOpenContent>";

    private static string Root(string type) =>
        "<xs:element name=\"r\" type=\"t:c\"/><xs:complexType name=\"c\">" + type + "</xs:complexType>";

    private static bool Valid(string schema, string body) =>
        AssemblyFixture
            .Run(
                AssemblyFixture.Build(AssemblyFixture.WrapSchema(schema)),
                "<r xmlns=\"urn:t\" xmlns:f=\"urn:foreign\">" + body + "</r>"
            )
            .IsValid;

    private static ComplexTypeDefinition Compile(string body, string attributes = "")
    {
        var compilation = CompilationFixture.Compile(
            "<xs:schema xmlns:xs=\""
                + CompilationFixture.Xs
                + "\" xmlns:t=\"urn:t\" targetNamespace=\"urn:t\" "
                + attributes
                + ">"
                + body
                + "</xs:schema>",
            XsdVersion.V11
        );
        return CompilationFixture.Get<ComplexTypeDefinition>(compilation, XsdComponentKind.ComplexType, "c");
    }

    [Test]
    [Arguments("interleave", "Interleave")]
    [Arguments("suffix", "Suffix")]
    public async Task Compile_OpenContent_SetsModeAndWildcard(string mode, string expected)
    {
        var type = Compile("<xs:complexType name=\"c\">" + Mode(mode) + Ab + "</xs:complexType>");

        await Assert.That(type.OpenContent).IsNotNull();
        await Assert.That(type.OpenContent!.Mode.ToString()).IsEqualTo(expected);
        await Assert.That(type.OpenContent.Wildcard).IsNotNull();
    }

    [Test]
    public async Task Compile_ModeNone_HasNoOpenContent()
    {
        var type = Compile("<xs:complexType name=\"c\"><xs:openContent mode=\"none\"/>" + Ab + "</xs:complexType>");

        await Assert.That(type.OpenContent).IsNull();
    }

    [Test]
    public async Task Validate_Interleave_AllowsForeignElementsEverywhere()
    {
        var schema = Root(Mode("interleave") + Ab);

        await Assert.That(Valid(schema, "<f:x/><a/><f:x/><b/><f:x/>")).IsTrue();
        await Assert.That(Valid(schema, "<a/><b/><t:z xmlns:t=\"urn:t\"/>")).IsFalse();
        await Assert.That(Valid(schema, "<b/><a/>")).IsFalse();
    }

    [Test]
    public async Task Validate_Suffix_AllowsForeignElementsOnlyAfterTheContentModel()
    {
        var schema = Root(Mode("suffix") + Ab);

        await Assert.That(Valid(schema, "<a/><b/><f:x/><f:y/>")).IsTrue();
        await Assert.That(Valid(schema, "<a/><f:x/><b/>")).IsFalse();
    }

    [Test]
    public async Task Validate_WithoutOpenContent_RejectsForeignElements() =>
        await Assert.That(Valid(Root(Ab), "<a/><b/><f:x/>")).IsFalse();

    [Test]
    public async Task Validate_AppliesToEmpty_DecidesWhetherTheDefaultOpensEmptyContent()
    {
        var without = DefaultOpen("") + Root("");
        var with = DefaultOpen(" appliesToEmpty=\"true\"") + Root("");

        await Assert.That(Valid(without, "<f:x/>")).IsFalse();
        await Assert.That(Valid(with, "<f:x/>")).IsTrue();
    }

    [Test]
    public async Task Validate_DefaultOpenContent_AppliesToTypesWithoutOwnAndModeNoneSwitchesItOff()
    {
        var defaultOpen = DefaultOpen(" mode=\"suffix\"");

        await Assert.That(Valid(defaultOpen + Root(Ab), "<a/><b/><f:x/>")).IsTrue();
        await Assert.That(Valid(defaultOpen + Root(Ab), "<a/><f:x/><b/>")).IsFalse();
        await Assert
            .That(Valid(defaultOpen + Root("<xs:openContent mode=\"none\"/>" + Ab), "<a/><b/><f:x/>"))
            .IsFalse();
        await Assert.That(Valid(defaultOpen + Root(Mode("interleave") + Ab), "<a/><f:x/><b/>")).IsTrue();
    }

    [Test]
    public async Task Compile_DefaultOpenContent_AppliesToEmptyOnlyWithTheAttribute()
    {
        var type = Compile(DefaultOpen(" appliesToEmpty=\"true\"") + "<xs:complexType name=\"c\"/>");
        var none = Compile(DefaultOpen("") + "<xs:complexType name=\"c\"/>");

        await Assert.That(type.OpenContent).IsNotNull();
        await Assert.That(none.OpenContent).IsNull();
    }

    [Test]
    public async Task Extension_FromInterleaveToSuffix_IsALocatedSchemaError()
    {
        var body =
            "<xs:complexType name=\"b\">"
            + Mode("interleave")
            + Ab
            + "</xs:complexType>"
            + "<xs:complexType name=\"c\"><xs:complexContent><xs:extension base=\"t:b\">"
            + Mode("suffix")
            + "</xs:extension></xs:complexContent></xs:complexType>";
        var compilation = CompilationFixture.Compile(
            "<xs:schema xmlns:xs=\""
                + CompilationFixture.Xs
                + "\" xmlns:t=\"urn:t\" targetNamespace=\"urn:t\">"
                + body
                + "</xs:schema>",
            XsdVersion.V11
        );

        var issue = compilation.Result.Issues.Single(i => i.Constraint == "cos-ct-extends.1.4.3.2.2.3");
        await Assert.That(issue.Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task Restriction_WideningTheOpenContent_IsALocatedSchemaError()
    {
        var baseType =
            "<xs:complexType name=\"b\"><xs:openContent mode=\"suffix\"><xs:any namespace=\"urn:foreign\"/></xs:openContent>"
            + Ab
            + "</xs:complexType>";
        string Derived(string open) =>
            "<xs:complexType name=\"c\"><xs:complexContent><xs:restriction base=\"t:b\">"
            + open
            + Ab
            + "</xs:restriction></xs:complexContent></xs:complexType>";
        string Run(string open) =>
            string.Join(
                "|",
                CompilationFixture
                    .Compile(
                        "<xs:schema xmlns:xs=\""
                            + CompilationFixture.Xs
                            + "\" xmlns:t=\"urn:t\" targetNamespace=\"urn:t\">"
                            + baseType
                            + Derived(open)
                            + "</xs:schema>",
                        XsdVersion.V11
                    )
                    .Result.Issues.Select(i => i.Constraint)
            );

        await Assert
            .That(Run("<xs:openContent mode=\"suffix\"><xs:any namespace=\"##any\"/></xs:openContent>"))
            .Contains("derivation-ok-restriction.open-content");
        await Assert
            .That(Run("<xs:openContent mode=\"interleave\"><xs:any namespace=\"urn:foreign\"/></xs:openContent>"))
            .Contains("derivation-ok-restriction.open-content");
        await Assert
            .That(Run("<xs:openContent mode=\"suffix\"><xs:any namespace=\"urn:foreign\"/></xs:openContent>"))
            .IsEqualTo("");
        await Assert.That(Run("")).IsEqualTo("");
    }
}
