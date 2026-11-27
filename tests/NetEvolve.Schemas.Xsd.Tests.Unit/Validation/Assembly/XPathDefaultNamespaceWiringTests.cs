namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// The <c>xpathDefaultNamespace</c> attribute of the selector and the field of an identity constraint and the default on the
/// <c>schema</c> element are honored by the assembled validator (XSD-78, #360). The elements of the instances are in <c>urn:t</c>,
/// so an unprefixed name test finds them only when the namespace in force is <c>urn:t</c>.
/// </summary>
public class XPathDefaultNamespaceWiringTests
{
    private const string Duplicate = "<root xmlns=\"urn:t\"><e a=\"1\"><k>1</k></e><e a=\"1\"><k>1</k></e></root>";

    private static string Schema(
        string selectorAttribute,
        string fieldXPath,
        string fieldAttribute,
        string schemaAttributes = ""
    ) =>
        AssemblyFixture.WrapSchema(
            "<xs:element name=\"root\"><xs:complexType><xs:sequence>"
                + "<xs:element name=\"e\" maxOccurs=\"unbounded\"><xs:complexType><xs:sequence><xs:element name=\"k\" type=\"xs:string\"/></xs:sequence>"
                + "<xs:attribute name=\"a\" type=\"xs:string\"/></xs:complexType></xs:element>"
                + "</xs:sequence></xs:complexType>"
                + "<xs:unique name=\"u\"><xs:selector xpath=\"e\" "
                + selectorAttribute
                + "/><xs:field xpath=\""
                + fieldXPath
                + "\" "
                + fieldAttribute
                + "/></xs:unique></xs:element>",
            schemaAttributes
        );

    private static async Task AssertMatched(string schema) =>
        await Assert.That(AssemblyFixture.Run(AssemblyFixture.Build(schema), Duplicate).IsValid).IsFalse();

    private static async Task AssertNotMatched(string schema) =>
        await Assert.That(AssemblyFixture.Run(AssemblyFixture.Build(schema), Duplicate).IsValid).IsTrue();

    [Test]
    public async Task Selector_DefaultNamespace_UsesTheDefaultNamespaceInScope() =>
        await AssertMatched(Schema("xpathDefaultNamespace=\"##defaultNamespace\"", "@a", "", "xmlns=\"urn:t\""));

    [Test]
    public async Task Selector_TargetNamespace_UsesTheTargetNamespace() =>
        await AssertMatched(Schema("xpathDefaultNamespace=\"##targetNamespace\"", "@a", ""));

    [Test]
    public async Task Selector_Local_MeansNoNamespace() =>
        await AssertNotMatched(Schema("xpathDefaultNamespace=\"##local\"", "@a", ""));

    [Test]
    public async Task Selector_ExplicitNamespace_UsesThatNamespace()
    {
        await AssertMatched(Schema("xpathDefaultNamespace=\"urn:t\"", "@a", ""));
        await AssertNotMatched(Schema("xpathDefaultNamespace=\"urn:other\"", "@a", ""));
    }

    [Test]
    public async Task Selector_WithoutAttribute_MeansNoNamespace() => await AssertNotMatched(Schema("", "@a", ""));

    private const string TargetSelector = "xpathDefaultNamespace=\"##targetNamespace\"";

    [Test]
    public async Task Field_DefaultNamespace_UsesTheDefaultNamespaceInScope() =>
        await AssertMatched(
            Schema(TargetSelector, "k", "xpathDefaultNamespace=\"##defaultNamespace\"", "xmlns=\"urn:t\"")
        );

    [Test]
    public async Task Field_TargetNamespace_UsesTheTargetNamespace() =>
        await AssertMatched(Schema(TargetSelector, "k", "xpathDefaultNamespace=\"##targetNamespace\""));

    [Test]
    public async Task Field_Local_MeansNoNamespace() =>
        await AssertNotMatched(Schema(TargetSelector, "k", "xpathDefaultNamespace=\"##local\""));

    [Test]
    public async Task Field_ExplicitNamespace_UsesThatNamespace()
    {
        await AssertMatched(Schema(TargetSelector, "k", "xpathDefaultNamespace=\"urn:t\""));
        await AssertNotMatched(Schema(TargetSelector, "k", "xpathDefaultNamespace=\"urn:other\""));
    }

    [Test]
    public async Task SchemaDefault_AppliesToSelectorAndField() =>
        await AssertMatched(Schema("", "k", "", "xpathDefaultNamespace=\"##targetNamespace\""));

    [Test]
    public async Task SchemaDefault_IsOverriddenByALocalAttribute()
    {
        const string Default = "xpathDefaultNamespace=\"##targetNamespace\"";

        await AssertNotMatched(Schema("xpathDefaultNamespace=\"##local\"", "k", "", Default));
        await AssertNotMatched(Schema("", "k", "xpathDefaultNamespace=\"##local\"", Default));
    }

    [Test]
    public async Task InvalidValue_IsALocatedSchemaError()
    {
        var exception = AssemblyFixture.Failure(() =>
            AssemblyFixture.Build(Schema("xpathDefaultNamespace=\"##bogus\"", "@a", ""))
        );

        await Assert.That(exception.Issues.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(exception.Issues.All(i => i.Location.Line > 0)).IsTrue();
        await Assert.That(exception.Issues.Any(i => i.Code.Number == 4070)).IsFalse();
    }
}
