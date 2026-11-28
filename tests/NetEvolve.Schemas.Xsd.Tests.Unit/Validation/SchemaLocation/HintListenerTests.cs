namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.SchemaLocation;

using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

/// <summary>The identity constraint, ID, and entity checks of a schema that a hint loaded (#960).</summary>
public class HintListenerTests
{
    private const string Hint = "xsi:schemaLocation=\"urn:h h.xsd\"";

    private const string KeyHinted =
        "<xs:schema xmlns:xs=\""
        + HintFixture.Xs
        + "\" targetNamespace=\""
        + HintFixture.Hinted
        + "\" elementFormDefault=\"qualified\"><xs:element name=\"root\"><xs:complexType><xs:sequence>"
        + "<xs:element name=\"i\" type=\"xs:string\" minOccurs=\"0\" maxOccurs=\"unbounded\"/></xs:sequence></xs:complexType>"
        + "<xs:key name=\"k\"><xs:selector xpath=\"h:i\" xmlns:h=\""
        + HintFixture.Hinted
        + "\"/><xs:field xpath=\".\"/></xs:key></xs:element></xs:schema>";

    private const string IdHinted =
        "<xs:schema xmlns:xs=\""
        + HintFixture.Xs
        + "\" targetNamespace=\""
        + HintFixture.Hinted
        + "\" elementFormDefault=\"qualified\"><xs:element name=\"root\"><xs:complexType><xs:sequence>"
        + "<xs:element name=\"i\" minOccurs=\"0\" maxOccurs=\"unbounded\"><xs:complexType>"
        + "<xs:attribute name=\"id\" type=\"xs:ID\"/></xs:complexType></xs:element></xs:sequence></xs:complexType></xs:element></xs:schema>";

    private const string EntityHinted =
        "<xs:schema xmlns:xs=\""
        + HintFixture.Xs
        + "\" targetNamespace=\""
        + HintFixture.Hinted
        + "\" elementFormDefault=\"qualified\"><xs:element name=\"root\"><xs:complexType>"
        + "<xs:attribute name=\"e\" type=\"xs:ENTITY\"/></xs:complexType></xs:element></xs:schema>";

    private const string NotationHinted =
        "<xs:schema xmlns:xs=\""
        + HintFixture.Xs
        + "\" targetNamespace=\""
        + HintFixture.Hinted
        + "\" elementFormDefault=\"qualified\"><xs:notation name=\"gif\" public=\"image/gif\"/>"
        + "<xs:element name=\"root\"><xs:complexType><xs:attribute name=\"n\" type=\"xs:NOTATION\"/>"
        + "</xs:complexType></xs:element></xs:schema>";

    // A configured schema that already names ID and carries an identity constraint, so both listeners exist before a hint.
    private const string BaseWithListeners =
        "<xs:schema xmlns:xs=\""
        + HintFixture.Xs
        + "\" targetNamespace=\""
        + HintFixture.Base
        + "\" elementFormDefault=\"qualified\"><xs:element name=\"r\"><xs:complexType><xs:sequence>"
        + "<xs:any namespace=\"##other\" processContents=\"lax\" minOccurs=\"0\" maxOccurs=\"unbounded\"/></xs:sequence>"
        + "<xs:attribute name=\"id\" type=\"xs:ID\"/></xs:complexType>"
        + "<xs:unique name=\"u\"><xs:selector xpath=\"@id\"/><xs:field xpath=\".\"/></xs:unique></xs:element></xs:schema>";

    private static ValidationResult Run(
        string baseSchema,
        string hinted,
        string instance,
        string prefix = "",
        bool dtd = false
    )
    {
        var resolver = new MapResolver().Add(HintFixture.HintedId, hinted);
        var builder = SchemaValidator
            .ForXsd()
            .WithSchemaLocationHints(true)
            .WithResolver(resolver)
            .WithDtdProcessing(dtd);
        var validator = builder.AddSchema(baseSchema.AsMemory(), "file:///s.xsd").Build();
        return validator.Validate((prefix + instance).AsSpan(), HintFixture.InstanceId);
    }

    private static string Errors(ValidationResult result) =>
        string.Join("|", HintFixture.Errors(result).Select(i => i.Constraint));

    [Test]
    public async Task Key_DuplicateValueInTheHintedSchema_IsReported()
    {
        var result = Run(
            HintFixture.BaseSchema,
            KeyHinted,
            HintFixture.BaseRoot(string.Empty, "<h:root " + Hint + "><h:i>a</h:i><h:i>a</h:i></h:root>")
        );

        await Assert.That(Errors(result)).Contains("cvc-identity-constraint");
    }

    [Test]
    public async Task Key_DuplicateValueWithTheHintOnTheRoot_IsReported()
    {
        var result = Run(
            HintFixture.BaseSchema,
            KeyHinted,
            HintFixture.BaseRoot(Hint, "<h:root><h:i>a</h:i><h:i>a</h:i></h:root>")
        );

        await Assert.That(Errors(result)).Contains("cvc-identity-constraint");
    }

    [Test]
    public async Task Key_DistinctValues_IsValid()
    {
        var result = Run(
            HintFixture.BaseSchema,
            KeyHinted,
            HintFixture.BaseRoot(Hint, "<h:root><h:i>a</h:i><h:i>b</h:i></h:root>")
        );

        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task Key_BothTheConfiguredAndTheHintedConstraintsRun()
    {
        var hinted = Run(
            BaseWithListeners,
            KeyHinted,
            HintFixture.BaseRoot(Hint, "<h:root><h:i>a</h:i><h:i>a</h:i></h:root>")
        );
        var configured = Run(
            BaseWithListeners,
            KeyHinted,
            HintFixture.BaseRoot("id=\"x\"", "<h:root><h:i>a</h:i></h:root>")
        );

        await Assert.That(Errors(hinted)).Contains("cvc-identity-constraint");
        await Assert.That(configured.Issues).IsEmpty();
    }

    [Test]
    public async Task Id_DuplicateIdInTheHintedSchema_IsReported()
    {
        var result = Run(
            HintFixture.BaseSchema,
            IdHinted,
            HintFixture.BaseRoot(Hint, "<h:root><h:i id=\"a\"/><h:i id=\"a\"/></h:root>")
        );

        await Assert.That(HintFixture.Errors(result)).IsNotEmpty();
    }

    [Test]
    public async Task Entity_UndeclaredEntityInTheHintedSchema_IsReported()
    {
        var result = Run(HintFixture.BaseSchema, EntityHinted, HintFixture.BaseRoot(Hint, "<h:root e=\"pic\"/>"));

        await Assert.That(HintFixture.Errors(result)).IsNotEmpty();
    }

    [Test]
    public async Task Entity_DeclaredInTheDoctype_IsValid()
    {
        const string Doctype =
            "<!DOCTYPE r [<!NOTATION gif SYSTEM \"image/gif\"><!ENTITY pic SYSTEM \"pic.gif\" NDATA gif>]>";
        var result = Run(
            HintFixture.BaseSchema,
            EntityHinted,
            HintFixture.BaseRoot(Hint, "<h:root e=\"pic\"/>"),
            Doctype,
            dtd: true
        );

        await Assert.That(result.Issues).IsEmpty();
    }

    [Test]
    public async Task Notation_DeclaredInTheHintedSchema_IsValidWhenTheIdListenerExists()
    {
        var result = Run(BaseWithListeners, NotationHinted, HintFixture.BaseRoot(Hint, "<h:root n=\"gif\"/>"));

        await Assert.That(result.Issues).IsEmpty();
    }
}
