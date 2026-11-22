namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using static NetEvolve.Schemas.Xml.Tests.Unit.Constraints.ConstraintHarness;

/// <summary>
/// One violation and one conforming document for each validity constraint of XML 1.0 (Fifth Edition) on attribute types,
/// defaults, and values (sections 3.3.1 and 3.3.2), read through the validating reader mode. A violation is a validity error
/// with the code of the constraint and no fatal status.
/// </summary>
public class AttributeValidityConstraintTests
{
    private const string Root = "<!ELEMENT r ANY>";
    private const string Notation = "<!NOTATION n SYSTEM \"n\">";

    private static string Doc(string attributes, string content) => ConstraintHarness.Doc(Root + attributes, content);

    // Section 3.3.1, production 56: the value of an ID attribute is a Name, unique in the document.
    [Test]
    public async Task Id_ValueThatIsNotAName_ReportsXml5009() =>
        await ReportsValidity(Doc("<!ATTLIST r i ID #IMPLIED>", "<r i=\"1\"/>"), "XML5009");

    [Test]
    public async Task Id_ValueUsedTwice_ReportsXml5009() =>
        await ReportsValidity(Doc("<!ATTLIST r i ID #IMPLIED>", "<r i=\"a\"><r i=\"a\"/></r>"), "XML5009");

    [Test]
    public async Task Id_UniqueNameValue_IsValid() =>
        await IsValid(Doc("<!ATTLIST r i ID #IMPLIED>", "<r i=\"a\"><r i=\"b\"/></r>"));

    // Section 3.3.1, production 56.
    [Test]
    public async Task OneIdPerElementType_TwoIdAttributes_ReportsXml5010() =>
        await ReportsValidity(Doc("<!ATTLIST r i ID #IMPLIED j ID #IMPLIED>", "<r/>"), "XML5010");

    [Test]
    public async Task OneIdPerElementType_OneIdAttribute_IsValid() =>
        await IsValid(Doc("<!ATTLIST r i ID #IMPLIED j CDATA #IMPLIED>", "<r/>"));

    // Section 3.3.1, production 56: an ID attribute is #IMPLIED or #REQUIRED.
    [Test]
    public async Task IdAttributeDefault_ADefaultValue_ReportsXml5011() =>
        await ReportsValidity(Doc("<!ATTLIST r i ID \"x\">", "<r/>"), "XML5011");

    [Test]
    public async Task IdAttributeDefault_Required_IsValid() =>
        await IsValid(Doc("<!ATTLIST r i ID #REQUIRED>", "<r i=\"a\"/>"));

    // Section 3.3.1, production 56: an IDREF value matches an ID in the document.
    [Test]
    public async Task IdRef_ValueWithoutAnId_ReportsXml5012() =>
        await ReportsValidity(Doc("<!ATTLIST r i ID #IMPLIED p IDREF #IMPLIED>", "<r i=\"a\" p=\"b\"/>"), "XML5012");

    [Test]
    public async Task IdRef_ValueOfAnId_IsValid() =>
        await IsValid(Doc("<!ATTLIST r i ID #IMPLIED p IDREF #IMPLIED>", "<r i=\"a\" p=\"a\"/>"));

    // Section 3.3.1, production 56: an ENTITY value names an unparsed entity.
    [Test]
    public async Task EntityName_ValueOfAnUndeclaredEntity_ReportsXml5013() =>
        await ReportsValidity(Doc("<!ATTLIST r e ENTITY #IMPLIED>", "<r e=\"u\"/>"), "XML5013");

    [Test]
    public async Task EntityName_ValueOfAnUnparsedEntity_IsValid() =>
        await IsValid(Doc(Notation + "<!ENTITY u SYSTEM \"u\" NDATA n><!ATTLIST r e ENTITY #IMPLIED>", "<r e=\"u\"/>"));

    // Section 3.3.1, production 56: an NMTOKEN value matches Nmtoken.
    [Test]
    public async Task NameToken_ValueWithASpace_ReportsXml5014() =>
        await ReportsValidity(Doc("<!ATTLIST r t NMTOKEN #IMPLIED>", "<r t=\"a b\"/>"), "XML5014");

    [Test]
    public async Task NameToken_ValueThatIsANameToken_IsValid() =>
        await IsValid(Doc("<!ATTLIST r t NMTOKEN #IMPLIED>", "<r t=\"1a\"/>"));

    // Section 3.3.1, production 58: the value is one of the notation names of the declaration.
    [Test]
    public async Task NotationAttributes_ValueOutsideTheList_ReportsXml5015() =>
        await ReportsValidity(
            Doc(Notation + "<!NOTATION m SYSTEM \"m\"><!ATTLIST r t NOTATION (n) #IMPLIED>", "<r t=\"m\"/>"),
            "XML5015"
        );

    [Test]
    public async Task NotationAttributes_ValueInTheList_IsValid() =>
        await IsValid(Doc(Notation + "<!ATTLIST r t NOTATION (n) #IMPLIED>", "<r t=\"n\"/>"));

    // Section 3.3.1, production 58: at most one NOTATION attribute per element type.
    [Test]
    public async Task OneNotationPerElementType_TwoNotationAttributes_ReportsXml5016() =>
        await ReportsValidity(
            Doc(Notation + "<!ATTLIST r t NOTATION (n) #IMPLIED u NOTATION (n) #IMPLIED>", "<r/>"),
            "XML5016"
        );

    [Test]
    public async Task OneNotationPerElementType_OneNotationAttribute_IsValid() =>
        await IsValid(Doc(Notation + "<!ATTLIST r t NOTATION (n) #IMPLIED>", "<r/>"));

    // Section 3.3.1, production 58: no NOTATION attribute on an EMPTY element.
    [Test]
    public async Task NoNotationOnEmptyElement_NotationAttributeOnAnEmptyElement_ReportsXml5017() =>
        await ReportsValidity(
            ConstraintHarness.Doc("<!ELEMENT r EMPTY>" + Notation + "<!ATTLIST r t NOTATION (n) #IMPLIED>", "<r/>"),
            "XML5017"
        );

    [Test]
    public async Task NoNotationOnEmptyElement_NotationAttributeOnAnElementWithContent_IsValid() =>
        await IsValid(Doc(Notation + "<!ATTLIST r t NOTATION (n) #IMPLIED>", "<r/>"));

    // Section 3.3.1, productions 58 and 59: no token twice in an enumeration or a notation list.
    [Test]
    public async Task NoDuplicateTokens_TokenTwiceInAnEnumeration_ReportsXml5018() =>
        await ReportsValidity(Doc("<!ATTLIST r t (a|a) #IMPLIED>", "<r/>"), "XML5018");

    [Test]
    public async Task NoDuplicateTokens_NotationTwiceInAList_ReportsXml5018() =>
        await ReportsValidity(Doc(Notation + "<!ATTLIST r t NOTATION (n|n) #IMPLIED>", "<r/>"), "XML5018");

    [Test]
    public async Task NoDuplicateTokens_DistinctTokens_IsValid() =>
        await IsValid(Doc("<!ATTLIST r t (a|b) #IMPLIED>", "<r/>"));

    // Section 3.3.1, production 59: the value is one of the tokens of the enumeration.
    [Test]
    public async Task Enumeration_ValueOutsideTheEnumeration_ReportsXml5019() =>
        await ReportsValidity(Doc("<!ATTLIST r t (a|b) #IMPLIED>", "<r t=\"c\"/>"), "XML5019");

    [Test]
    public async Task Enumeration_ValueInTheEnumeration_IsValid() =>
        await IsValid(Doc("<!ATTLIST r t (a|b) #IMPLIED>", "<r t=\"a\"/>"));

    // Section 3.3.2, production 60: a #REQUIRED attribute is specified.
    [Test]
    public async Task RequiredAttribute_AttributeLeftOut_ReportsXml5020() =>
        await ReportsValidity(Doc("<!ATTLIST r t CDATA #REQUIRED>", "<r/>"), "XML5020");

    [Test]
    public async Task RequiredAttribute_AttributeSpecified_IsValid() =>
        await IsValid(Doc("<!ATTLIST r t CDATA #REQUIRED>", "<r t=\"x\"/>"));

    // Section 3.3.2, production 60: the default value matches the declared type.
    [Test]
    public async Task AttributeDefaultValueSyntacticallyCorrect_DefaultThatIsNotANameToken_ReportsXml5021() =>
        await ReportsValidity(Doc("<!ATTLIST r t NMTOKEN \"a b\">", "<r/>"), "XML5021");

    [Test]
    public async Task AttributeDefaultValueSyntacticallyCorrect_DefaultOutsideTheEnumeration_ReportsXml5021() =>
        await ReportsValidity(Doc("<!ATTLIST r t (a|b) \"c\">", "<r/>"), "XML5021");

    [Test]
    public async Task AttributeDefaultValueSyntacticallyCorrect_DefaultThatIsANameToken_IsValid() =>
        await IsValid(Doc("<!ATTLIST r t NMTOKEN \"a\">", "<r/>"));

    // Section 3.3.2, production 60: a #FIXED attribute has the default value.
    [Test]
    public async Task FixedAttributeDefault_ValueOtherThanTheDefault_ReportsXml5022() =>
        await ReportsValidity(Doc("<!ATTLIST r t CDATA #FIXED \"x\">", "<r t=\"y\"/>"), "XML5022");

    [Test]
    public async Task FixedAttributeDefault_ValueEqualToTheDefault_IsValid() =>
        await IsValid(Doc("<!ATTLIST r t CDATA #FIXED \"x\">", "<r t=\"x\"/>"));
}
