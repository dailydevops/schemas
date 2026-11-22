namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using static NetEvolve.Schemas.Xml.Tests.Unit.Constraints.ConstraintHarness;

/// <summary>
/// One violation and one conforming document for each well-formedness constraint of XML 1.0 (Fifth Edition), read through the
/// validating reader mode. A violation is a fatal status; a constraint that is not enforced turns its violation test red.
/// </summary>
public class WellFormednessConstraintTests
{
    // Section 2.8, production 28: the external subset must match extSubset.
    [Test]
    public async Task ExternalSubset_MalformedSubset_IsFatalXml0011() =>
        await ReportsFatal(ExternalSubsetDocument, "XML0011", ("ext.dtd", "garbage <<"));

    [Test]
    public async Task ExternalSubset_ConformingSubset_IsValid() =>
        await IsValid(ExternalSubsetDocument, ("ext.dtd", "<!ELEMENT r EMPTY>"));

    // Section 2.8, production 28a: the replacement text of a parameter-entity reference between declarations must be complete declarations.
    [Test]
    public async Task PeBetweenDeclarations_PartialDeclarationInTheReplacementText_IsFatalXml0011() =>
        await ReportsFatal(Doc("<!ENTITY % p \"<!ELEMENT r\"> %p; ANY>", "<r/>"), "XML0011");

    [Test]
    public async Task PeBetweenDeclarations_PartialDeclarationInTheExternalSubset_IsFatalXml0011() =>
        await ReportsFatal(ExternalSubsetDocument, "XML0011", ("ext.dtd", "<!ENTITY % p \"<!ELEMENT r\"> %p; ANY>"));

    [Test]
    public async Task PeBetweenDeclarations_CompleteDeclarationInTheReplacementText_IsWellFormed() =>
        await IsWellFormed(Doc("<!ENTITY % p \"<!ELEMENT r ANY>\"> %p;", "<r/>"));

    // Section 2.8, production 29: no parameter-entity reference inside a markup declaration of the internal subset.
    [Test]
    public async Task PesInInternalSubset_ReferenceInsideAMarkupDeclaration_IsFatalXml0019() =>
        await ReportsFatal(Doc("<!ENTITY % p \"ANY\"><!ELEMENT r %p;>", "<r/>"), "XML0019");

    [Test]
    public async Task PesInInternalSubset_ReferenceInsideADeclarationOfTheExternalSubset_IsWellFormed() =>
        await IsWellFormed(ExternalSubsetDocument, ("ext.dtd", "<!ENTITY % p \"ANY\"><!ELEMENT r %p;>"));

    [Test]
    public async Task PesInInternalSubset_ReferenceBetweenDeclarations_IsWellFormed() =>
        await IsWellFormed(Doc("<!ENTITY % p \"<!ELEMENT r ANY>\"> %p;", "<r/>"));

    // Section 3, production 39: the end tag names the open element.
    [Test]
    public async Task ElementTypeMatch_EndTagOfAnotherElement_IsFatalXml0005() =>
        await ReportsFatal("<r></s>", "XML0005");

    [Test]
    public async Task ElementTypeMatch_NestedEndTagOutOfOrder_IsFatalXml0005() =>
        await ReportsFatal("<r><a><b></a></b></r>", "XML0005");

    [Test]
    public async Task ElementTypeMatch_EndTagOfTheOpenElement_IsWellFormed() =>
        await IsWellFormed("<r><a><b></b></a></r>");

    // Section 3.1, productions 40 and 44: no attribute name twice in a start tag or an empty-element tag.
    [Test]
    public async Task UniqueAttSpec_NameTwiceInAStartTag_IsFatalXml0006() =>
        await ReportsFatal("<r a=\"1\" a=\"2\"></r>", "XML0006");

    [Test]
    public async Task UniqueAttSpec_NameTwiceInAnEmptyElementTag_IsFatalXml0006() =>
        await ReportsFatal("<r a=\"1\" a=\"2\"/>", "XML0006");

    [Test]
    public async Task UniqueAttSpec_DistinctNames_IsWellFormed() => await IsWellFormed("<r a=\"1\" b=\"2\" A=\"3\"/>");

    // Section 3.1, production 41, and section 3.3.2, production 60: no reference to an external entity in an attribute value.
    [Test]
    public async Task NoExternalEntityReferences_ReferenceInAnAttributeValue_IsFatalXml0022() =>
        await ReportsFatal(Doc("<!ENTITY e SYSTEM \"e.ent\">", "<r a=\"&e;\"/>"), "XML0022");

    [Test]
    public async Task NoExternalEntityReferences_ReferenceInAnAttributeDefault_IsFatalXml0022() =>
        await ReportsFatal(Doc("<!ENTITY e SYSTEM \"e.ent\"><!ATTLIST r a CDATA \"&e;\">", "<r/>"), "XML0022");

    [Test]
    public async Task NoExternalEntityReferences_InternalEntityInAnAttributeValue_IsWellFormed() =>
        await IsWellFormed(Doc("<!ENTITY e \"x\">", "<r a=\"&e;\"/>"));

    // Section 3.1, production 41, and section 3.3.2, production 60: no less-than sign in an attribute value.
    [Test]
    public async Task NoLessThanInAttributeValues_LiteralInAnAttributeValue_IsFatalXml0013() =>
        await ReportsFatal("<r a=\"<\"/>", "XML0013");

    [Test]
    public async Task NoLessThanInAttributeValues_ReplacementTextOfAnEntity_IsFatalXml0013() =>
        await ReportsFatal(Doc("<!ENTITY e \"<\">", "<r a=\"&e;\"/>"), "XML0013");

    [Test]
    public async Task NoLessThanInAttributeValues_LiteralInAnAttributeDefault_IsFatalXml0013() =>
        await ReportsFatal(Doc("<!ATTLIST r a CDATA \"<\">", "<r/>"), "XML0013");

    [Test]
    public async Task NoLessThanInAttributeValues_PredefinedEntityReference_IsWellFormed() =>
        await IsWellFormed("<r a=\"&lt;\"/>");

    // Section 4.1, production 66: a character reference names a legal character.
    [Test]
    public async Task LegalCharacter_ReferenceToTheNullCharacter_IsFatalXml0009() =>
        await ReportsFatal("<r>&#0;</r>", "XML0009");

    [Test]
    public async Task LegalCharacter_ReferenceToALegalCharacter_IsWellFormed() =>
        await IsWellFormed("<r>&#65;&#x1F600;</r>");

    // Section 4.1, production 68: a referenced entity is declared (no DTD, standalone, or an internal subset only).
    [Test]
    public async Task EntityDeclared_ReferenceWithoutADeclaration_IsFatalXml0008() =>
        await ReportsFatal("<r>&e;</r>", "XML0008");

    [Test]
    public async Task EntityDeclared_ReferenceInAStandaloneDocument_IsFatalXml0008() =>
        await ReportsFatal(
            "<?xml version=\"1.0\" standalone=\"yes\"?><!DOCTYPE r [<!ELEMENT r ANY>]><r>&e;</r>",
            "XML0008"
        );

    [Test]
    public async Task EntityDeclared_ReferenceToADeclaredEntity_IsWellFormed() =>
        await IsWellFormed(Doc("<!ENTITY e \"x\">", "<r>&e;</r>"));

    // Section 4.1, production 68: an entity reference does not name an unparsed entity.
    [Test]
    public async Task ParsedEntity_ReferenceToAnUnparsedEntity_IsFatalXml0021() =>
        await ReportsFatal(Doc("<!NOTATION n SYSTEM \"n\"><!ENTITY e SYSTEM \"e\" NDATA n>", "<r>&e;</r>"), "XML0021");

    [Test]
    public async Task ParsedEntity_ReferenceToAParsedEntity_IsWellFormed() =>
        await IsWellFormed(Doc("<!ENTITY e \"x\">", "<r>&e;</r>"));

    // Section 4.1, productions 68 and 69: a parsed entity does not contain a reference to itself, directly or indirectly.
    [Test]
    public async Task NoRecursion_GeneralEntityThatReferencesItselfThroughAnother_IsFatalXml0020() =>
        await ReportsFatal(Doc("<!ENTITY a \"&b;\"><!ENTITY b \"&a;\">", "<r>&a;</r>"), "XML0020");

    [Test]
    public async Task NoRecursion_ParameterEntityThatReferencesItself_IsFatalXml0020() =>
        await ReportsFatal(Doc("<!ENTITY % a \"&#37;a;\">%a;", "<r/>"), "XML0020");

    [Test]
    public async Task NoRecursion_ParameterEntitiesThatReferenceEachOther_IsFatalXml0020() =>
        await ReportsFatal(Doc("<!ENTITY % a \"&#37;b;\"><!ENTITY % b \"&#37;a;\">%a;", "<r/>"), "XML0020");

    [Test]
    public async Task NoRecursion_NestedEntityWithoutACycle_IsWellFormed() =>
        await IsWellFormed(Doc("<!ENTITY a \"&b;&b;\"><!ENTITY b \"x\">", "<r>&a;</r>"));

    // Section 4.1, production 69: a parameter-entity reference appears only in the DTD.
    [Test]
    public async Task InDtd_ReferenceInsideAStartTag_IsFatalXml0011() =>
        await ReportsFatal(Doc("<!ENTITY % p \"x\">", "<r %p;/>"), "XML0011");

    [Test]
    public async Task InDtd_PercentSignInContentAndAttributeValue_IsWellFormedCharacterData() =>
        await IsWellFormed(Doc("<!ENTITY % p \"x\">", "<r a=\"%p;\">%p; and %undeclared;</r>"));
}
