namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using static NetEvolve.Schemas.Xml.Tests.Unit.Constraints.ConstraintHarness;

/// <summary>
/// One violation and one conforming document for each validity constraint of XML 1.0 (Fifth Edition) that concerns the
/// document, the elements, and the entity and notation declarations, read through the validating reader mode. A violation is a
/// validity error with the code of the constraint and no fatal status; a constraint that is not enforced turns its violation
/// test red. The constraints on attributes are in <see cref="AttributeValidityConstraintTests"/>.
/// </summary>
public class ValidityConstraintTests
{
    private const string Standalone = "<?xml version=\"1.0\" standalone=\"yes\"?>";
    private const string ExternalDefault = "<!ELEMENT r ANY><!ATTLIST r a CDATA \"d\">";

    // Section 2.8, production 28.
    [Test]
    public async Task RootElementType_RootNamedLikeAnotherElementType_ReportsXml5001() =>
        await ReportsValidity(Doc("<!ELEMENT r ANY><!ELEMENT b ANY>", "<b/>"), "XML5001");

    [Test]
    public async Task RootElementType_RootNamedLikeTheDocumentType_IsValid() =>
        await IsValid(Doc("<!ELEMENT r ANY>", "<r/>"));

    // Section 2.9, production 32: the clause for an attribute default that comes from the external subset.
    [Test]
    public async Task StandaloneDocumentDeclaration_ExternallyDeclaredDefaultApplied_ReportsXml5003() =>
        await ReportsValidity(
            Standalone + "<!DOCTYPE r SYSTEM \"ext.dtd\"><r/>",
            "XML5003",
            ("ext.dtd", ExternalDefault)
        );

    [Test]
    public async Task StandaloneDocumentDeclaration_ExternallyDeclaredDefaultSpecified_IsValid() =>
        await IsValid(Standalone + "<!DOCTYPE r SYSTEM \"ext.dtd\"><r a=\"x\"/>", ("ext.dtd", ExternalDefault));

    // Section 3, production 39.
    [Test]
    public async Task ElementValid_CharacterDataInAnEmptyElement_ReportsXml5004() =>
        await ReportsValidity(Doc("<!ELEMENT r EMPTY>", "<r>x</r>"), "XML5004");

    [Test]
    public async Task ElementValid_ElementWithoutADeclaration_ReportsXml5004() =>
        await ReportsValidity(Doc("<!ELEMENT r ANY>", "<r><u/></r>"), "XML5004");

    [Test]
    public async Task ElementValid_ChildOutsideTheContentModel_ReportsXml5004() =>
        await ReportsValidity(Doc("<!ELEMENT r (a)><!ELEMENT a EMPTY><!ELEMENT b EMPTY>", "<r><b/></r>"), "XML5004");

    [Test]
    public async Task ElementValid_EmptyElementWithoutContent_IsValid() =>
        await IsValid(Doc("<!ELEMENT r EMPTY>", "<r/>"));

    [Test]
    public async Task ElementValid_ChildInsideTheContentModel_IsValid() =>
        await IsValid(Doc("<!ELEMENT r (a)><!ELEMENT a EMPTY>", "<r><a/></r>"));

    // Section 3.1, production 41: the attribute is declared.
    [Test]
    public async Task AttributeValueType_AttributeWithoutADeclaration_ReportsXml5005() =>
        await ReportsValidity(Doc("<!ELEMENT r EMPTY>", "<r a=\"1\"/>"), "XML5005");

    [Test]
    public async Task AttributeValueType_DeclaredAttribute_IsValid() =>
        await IsValid(Doc("<!ELEMENT r EMPTY><!ATTLIST r a CDATA #IMPLIED>", "<r a=\"1\"/>"));

    // Section 3.2.2, production 51.
    [Test]
    public async Task NoDuplicateTypes_NameTwiceInMixedContent_ReportsXml5008() =>
        await ReportsValidity(Doc("<!ELEMENT r (#PCDATA|a|a)*><!ELEMENT a ANY>", "<r/>"), "XML5008");

    [Test]
    public async Task NoDuplicateTypes_DistinctNamesInMixedContent_IsValid() =>
        await IsValid(Doc("<!ELEMENT r (#PCDATA|a|b)*><!ELEMENT a ANY><!ELEMENT b ANY>", "<r/>"));

    // Section 4.2.2, production 76: the notation of an unparsed entity is declared.
    [Test]
    public async Task NotationDeclared_UnparsedEntityWithAnUndeclaredNotation_ReportsXml5025() =>
        await ReportsValidity(Doc("<!ELEMENT r ANY><!ENTITY e SYSTEM \"e\" NDATA n>", "<r/>"), "XML5025");

    [Test]
    public async Task NotationDeclared_UnparsedEntityWithADeclaredNotation_IsValid() =>
        await IsValid(Doc("<!ELEMENT r ANY><!NOTATION n SYSTEM \"n\"><!ENTITY e SYSTEM \"e\" NDATA n>", "<r/>"));

    // Section 4.7, production 82.
    [Test]
    public async Task UniqueNotationName_NotationDeclaredTwice_ReportsXml5026() =>
        await ReportsValidity(
            Doc("<!ELEMENT r ANY><!NOTATION n SYSTEM \"a\"><!NOTATION n SYSTEM \"b\">", "<r/>"),
            "XML5026"
        );

    [Test]
    public async Task UniqueNotationName_DistinctNotationNames_IsValid() =>
        await IsValid(Doc("<!ELEMENT r ANY><!NOTATION n SYSTEM \"a\"><!NOTATION m SYSTEM \"b\">", "<r/>"));
}
