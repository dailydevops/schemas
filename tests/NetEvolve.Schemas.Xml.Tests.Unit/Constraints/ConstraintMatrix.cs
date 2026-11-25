namespace NetEvolve.Schemas.Xml.Tests.Unit.Constraints;

using System.Collections.Generic;

/// <summary>
/// The coverage matrix: every well-formedness and validity constraint of XML 1.0 (Fifth Edition) and every namespace
/// constraint of Namespaces in XML 1.0 (Third Edition) by its official name, the error code a violation reports, and the
/// tests that prove it.
/// </summary>
/// <remarks>
/// <para>
/// A constraint that is enforced has at least one positive test (a conforming document is accepted) and one negative test (a
/// violating document is rejected with the code); the tests read the document through the validating reader mode, so a
/// constraint that is not enforced turns its negative test red. A constraint that is not enforced yet is a recorded gap: it
/// names the issues that track it and the probe tests that fail the day the constraint is enforced, and it still lists the
/// positive and the negative test of the part that works (the validation step with a hand-built model).
/// </para>
/// <para>
/// Limitations: XML 1.1 and its constraints are out of scope (DEF-50, DEF-51). The conformance suite is not mapped: the unit
/// leg reads no catalog of it, so every mapping is a unit test; <c>xmlconf:</c> references are supported by the check and
/// verified against a supplied catalog. Three well-formedness constraints (External Subset, PE Between Declarations, In DTD)
/// have no code of their own; a violation is the syntax error <c>XML0011</c>, and the entry names that code.
/// </para>
/// </remarks>
internal static class ConstraintMatrix
{
    private const string Wf = "Constraints.WellFormednessConstraintTests.";
    private const string Ns = "Constraints.NamespaceConstraintTests.";
    private const string Vc = "Constraints.ValidityConstraintTests.";
    private const string At = "Constraints.AttributeValidityConstraintTests.";
    private const string Gp = "Constraints.ConstraintGapTests.";
    private const string DocumentStep = "Dtd.Validation.Document.";

    private const string NoCodeNote =
        "The specification gives no production error of its own; the reader reports the syntax error XML0011.";

    private static readonly ConstraintEntry[] WellFormednessEntries =
    [
        Wfc(
            "External Subset",
            "2.8",
            "XML0011",
            [Wf + "ExternalSubset_ConformingSubset_IsValid"],
            [Wf + "ExternalSubset_MalformedSubset_IsFatalXml0011"],
            NoCodeNote
        ),
        Wfc(
            "PE Between Declarations",
            "2.8",
            "XML0011",
            [Wf + "PeBetweenDeclarations_CompleteDeclarationInTheReplacementText_IsWellFormed"],
            [
                Wf + "PeBetweenDeclarations_PartialDeclarationInTheReplacementText_IsFatalXml0011",
                Wf + "PeBetweenDeclarations_PartialDeclarationInTheExternalSubset_IsFatalXml0011",
            ],
            NoCodeNote
        ),
        Wfc(
            "PEs in Internal Subset",
            "2.8",
            "XML0019",
            [
                Wf + "PesInInternalSubset_ReferenceInsideADeclarationOfTheExternalSubset_IsWellFormed",
                Wf + "PesInInternalSubset_ReferenceBetweenDeclarations_IsWellFormed",
            ],
            [Wf + "PesInInternalSubset_ReferenceInsideAMarkupDeclaration_IsFatalXml0019"]
        ),
        Wfc(
            "Element Type Match",
            "3",
            "XML0005",
            [Wf + "ElementTypeMatch_EndTagOfTheOpenElement_IsWellFormed"],
            [
                Wf + "ElementTypeMatch_EndTagOfAnotherElement_IsFatalXml0005",
                Wf + "ElementTypeMatch_NestedEndTagOutOfOrder_IsFatalXml0005",
            ]
        ),
        Wfc(
            "Unique Att Spec",
            "3.1",
            "XML0006",
            [Wf + "UniqueAttSpec_DistinctNames_IsWellFormed"],
            [
                Wf + "UniqueAttSpec_NameTwiceInAStartTag_IsFatalXml0006",
                Wf + "UniqueAttSpec_NameTwiceInAnEmptyElementTag_IsFatalXml0006",
            ]
        ),
        Wfc(
            "No External Entity References",
            "3.1, 3.3.2",
            "XML0022",
            [Wf + "NoExternalEntityReferences_InternalEntityInAnAttributeValue_IsWellFormed"],
            [
                Wf + "NoExternalEntityReferences_ReferenceInAnAttributeValue_IsFatalXml0022",
                Wf + "NoExternalEntityReferences_ReferenceInAnAttributeDefault_IsFatalXml0022",
            ]
        ),
        Wfc(
            "No < in Attribute Values",
            "3.1, 3.3.2",
            "XML0013",
            [Wf + "NoLessThanInAttributeValues_PredefinedEntityReference_IsWellFormed"],
            [
                Wf + "NoLessThanInAttributeValues_LiteralInAnAttributeValue_IsFatalXml0013",
                Wf + "NoLessThanInAttributeValues_ReplacementTextOfAnEntity_IsFatalXml0013",
                Wf + "NoLessThanInAttributeValues_LiteralInAnAttributeDefault_IsFatalXml0013",
            ]
        ),
        Wfc(
            "Legal Character",
            "4.1",
            "XML0009",
            [Wf + "LegalCharacter_ReferenceToALegalCharacter_IsWellFormed"],
            [Wf + "LegalCharacter_ReferenceToTheNullCharacter_IsFatalXml0009"]
        ),
        Wfc(
            "Entity Declared",
            "4.1",
            "XML0008",
            [Wf + "EntityDeclared_ReferenceToADeclaredEntity_IsWellFormed"],
            [
                Wf + "EntityDeclared_ReferenceWithoutADeclaration_IsFatalXml0008",
                Wf + "EntityDeclared_ReferenceInAStandaloneDocument_IsFatalXml0008",
            ]
        ),
        Wfc(
            "Parsed Entity",
            "4.1",
            "XML0021",
            [Wf + "ParsedEntity_ReferenceToAParsedEntity_IsWellFormed"],
            [Wf + "ParsedEntity_ReferenceToAnUnparsedEntity_IsFatalXml0021"]
        ),
        Wfc(
            "No Recursion",
            "4.1",
            "XML0020",
            [Wf + "NoRecursion_NestedEntityWithoutACycle_IsWellFormed"],
            [
                Wf + "NoRecursion_GeneralEntityThatReferencesItselfThroughAnother_IsFatalXml0020",
                Wf + "NoRecursion_ParameterEntityThatReferencesItself_IsFatalXml0020",
                Wf + "NoRecursion_ParameterEntitiesThatReferenceEachOther_IsFatalXml0020",
            ]
        ),
        Wfc(
            "In DTD",
            "4.1",
            "XML0011",
            [Wf + "InDtd_PercentSignInContentAndAttributeValue_IsWellFormedCharacterData"],
            [Wf + "InDtd_ReferenceInsideAStartTag_IsFatalXml0011"],
            NoCodeNote
        ),
    ];

    private static readonly ConstraintEntry[] ValidityEntries =
    [
        Valid(
            "Root Element Type",
            "2.8",
            "XML5001",
            [Vc + "RootElementType_RootNamedLikeTheDocumentType_IsValid"],
            [Vc + "RootElementType_RootNamedLikeAnotherElementType_ReportsXml5001"]
        ),
        Gap(
            "Proper Declaration/PE Nesting",
            "2.8",
            "XML5002",
            ["#608"],
            [Gp + "Gap_ProperDeclarationPeNesting_IsNotReported_Issue608"],
            [DocumentStep + "DeclarationConstraintTests.ProperDeclarationNesting_ModelWithoutFacts_ReportsNothing"],
            [
                DocumentStep
                    + "DeclarationConstraintTests.ProperDeclarationNesting_DeclarationAcrossReplacementTexts_ReportsXml5002AtItsLocation",
            ]
        ),
        Valid(
            "Standalone Document Declaration",
            "2.9",
            "XML5003",
            [Vc + "StandaloneDocumentDeclaration_ExternallyDeclaredDefaultSpecified_IsValid"],
            [Vc + "StandaloneDocumentDeclaration_ExternallyDeclaredDefaultApplied_ReportsXml5003"],
            "One code for the four clauses of the constraint; the first clause is the one tested here, the others are tested by the document step."
        ),
        Valid(
            "Element Valid",
            "3",
            "XML5004",
            [
                Vc + "ElementValid_EmptyElementWithoutContent_IsValid",
                Vc + "ElementValid_ChildInsideTheContentModel_IsValid",
            ],
            [
                Vc + "ElementValid_CharacterDataInAnEmptyElement_ReportsXml5004",
                Vc + "ElementValid_ElementWithoutADeclaration_ReportsXml5004",
                Vc + "ElementValid_ChildOutsideTheContentModel_ReportsXml5004",
            ]
        ),
        Valid(
            "Attribute Value Type",
            "3.1",
            "XML5005",
            [Vc + "AttributeValueType_DeclaredAttribute_IsValid"],
            [Vc + "AttributeValueType_AttributeWithoutADeclaration_ReportsXml5005"]
        ),
        Gap(
            "Unique Element Type Declaration",
            "3.2",
            "XML5006",
            ["#608"],
            [Gp + "Gap_UniqueElementTypeDeclaration_IsNotReported_Issue608"],
            [DocumentStep + "DeclarationConstraintTests.UniqueElementType_EachElementTypeDeclaredOnce_ReportsNothing"],
            [
                DocumentStep
                    + "DeclarationConstraintTests.UniqueElementType_SecondDeclarationOfAnElementType_ReportsXml5006AtItsLocation",
            ]
        ),
        Gap(
            "Proper Group/PE Nesting",
            "3.2.1",
            "XML5007",
            ["#608"],
            [Gp + "Gap_ProperGroupPeNesting_IsNotReported_Issue608"],
            [DocumentStep + "DeclarationConstraintTests.ProperGroupNesting_OnlyOtherFacts_ReportsNothingForGroups"],
            [
                DocumentStep
                    + "DeclarationConstraintTests.ProperGroupNesting_GroupAcrossReplacementTexts_ReportsXml5007AtItsLocation",
            ]
        ),
        Valid(
            "No Duplicate Types",
            "3.2.2",
            "XML5008",
            [Vc + "NoDuplicateTypes_DistinctNamesInMixedContent_IsValid"],
            [Vc + "NoDuplicateTypes_NameTwiceInMixedContent_ReportsXml5008"]
        ),
        Valid(
            "ID",
            "3.3.1",
            "XML5009",
            [At + "Id_UniqueNameValue_IsValid"],
            [At + "Id_ValueThatIsNotAName_ReportsXml5009", At + "Id_ValueUsedTwice_ReportsXml5009"]
        ),
        Valid(
            "One ID per Element Type",
            "3.3.1",
            "XML5010",
            [At + "OneIdPerElementType_OneIdAttribute_IsValid"],
            [At + "OneIdPerElementType_TwoIdAttributes_ReportsXml5010"]
        ),
        Valid(
            "ID Attribute Default",
            "3.3.1",
            "XML5011",
            [At + "IdAttributeDefault_Required_IsValid"],
            [At + "IdAttributeDefault_ADefaultValue_ReportsXml5011"]
        ),
        Valid(
            "IDREF",
            "3.3.1",
            "XML5012",
            [At + "IdRef_ValueOfAnId_IsValid"],
            [At + "IdRef_ValueWithoutAnId_ReportsXml5012"]
        ),
        Valid(
            "Entity Name",
            "3.3.1",
            "XML5013",
            [At + "EntityName_ValueOfAnUnparsedEntity_IsValid"],
            [At + "EntityName_ValueOfAnUndeclaredEntity_ReportsXml5013"]
        ),
        Valid(
            "Name Token",
            "3.3.1",
            "XML5014",
            [At + "NameToken_ValueThatIsANameToken_IsValid"],
            [At + "NameToken_ValueWithASpace_ReportsXml5014"]
        ),
        Valid(
            "Notation Attributes",
            "3.3.1",
            "XML5015",
            [At + "NotationAttributes_ValueInTheList_IsValid"],
            [At + "NotationAttributes_ValueOutsideTheList_ReportsXml5015"],
            "Limitation: a notation of the list that is not declared is reported as Notation Declared (XML5025), as the code table of design #122 gives that message to the notations of an attribute type."
        ),
        Valid(
            "One Notation Per Element Type",
            "3.3.1",
            "XML5016",
            [At + "OneNotationPerElementType_OneNotationAttribute_IsValid"],
            [At + "OneNotationPerElementType_TwoNotationAttributes_ReportsXml5016"]
        ),
        Valid(
            "No Notation on Empty Element",
            "3.3.1",
            "XML5017",
            [At + "NoNotationOnEmptyElement_NotationAttributeOnAnElementWithContent_IsValid"],
            [At + "NoNotationOnEmptyElement_NotationAttributeOnAnEmptyElement_ReportsXml5017"]
        ),
        Valid(
            "No Duplicate Tokens",
            "3.3.1",
            "XML5018",
            [At + "NoDuplicateTokens_DistinctTokens_IsValid"],
            [
                At + "NoDuplicateTokens_TokenTwiceInAnEnumeration_ReportsXml5018",
                At + "NoDuplicateTokens_NotationTwiceInAList_ReportsXml5018",
            ]
        ),
        Valid(
            "Enumeration",
            "3.3.1",
            "XML5019",
            [At + "Enumeration_ValueInTheEnumeration_IsValid"],
            [At + "Enumeration_ValueOutsideTheEnumeration_ReportsXml5019"]
        ),
        Valid(
            "Required Attribute",
            "3.3.2",
            "XML5020",
            [At + "RequiredAttribute_AttributeSpecified_IsValid"],
            [At + "RequiredAttribute_AttributeLeftOut_ReportsXml5020"]
        ),
        Valid(
            "Attribute Default Value Syntactically Correct",
            "3.3.2",
            "XML5021",
            [At + "AttributeDefaultValueSyntacticallyCorrect_DefaultThatIsANameToken_IsValid"],
            [
                At + "AttributeDefaultValueSyntacticallyCorrect_DefaultThatIsNotANameToken_ReportsXml5021",
                At + "AttributeDefaultValueSyntacticallyCorrect_DefaultOutsideTheEnumeration_ReportsXml5021",
            ]
        ),
        Valid(
            "Fixed Attribute Default",
            "3.3.2",
            "XML5022",
            [At + "FixedAttributeDefault_ValueEqualToTheDefault_IsValid"],
            [At + "FixedAttributeDefault_ValueOtherThanTheDefault_ReportsXml5022"]
        ),
        Gap(
            "Proper Conditional Section/PE Nesting",
            "3.4",
            "XML5023",
            ["#608"],
            [Gp + "Gap_ProperConditionalSectionPeNesting_IsNotReported_Issue608"],
            [
                DocumentStep
                    + "DeclarationConstraintTests.ProperConditionalSectionNesting_ModelWithoutFacts_ReportsNothing",
            ],
            [
                DocumentStep
                    + "DeclarationConstraintTests.ProperConditionalSectionNesting_SectionAcrossReplacementTexts_ReportsXml5023AtItsLocation",
            ]
        ),
        Gap(
            "Entity Declared",
            "4.1",
            "XML5024",
            ["#647", "#935"],
            [
                Gp + "Gap_EntityDeclaredValidityForm_IsNotReported_Issue647",
                Gp + "Gap_EntityDeclaredForParameterEntityReference_InTheExternalSubset_IsNotReported_Issue935",
                Gp + "Gap_EntityDeclaredForParameterEntityReference_InAnEntityValue_IsNotReported_Issue935",
                Gp + "Gap_EntityDeclaredForParameterEntityReference_InTheInternalSubset_IsNotReported_Issue935",
            ],
            [DocumentStep + "DocumentReportTests.EntityDeclared_ReferenceToADeclaredEntity_ReportsNothing"],
            [
                DocumentStep
                    + "DocumentReportTests.EntityDeclared_ReferenceToAnUndeclaredEntity_ReportsXml5024AtTheReference_AndContinues",
            ],
            "The well-formedness form of the constraint is enforced (XML0008). The validity form for a general entity needs the entity reference event of #647; for a parameter entity (production 69) it needs a parse-time fact (#935)."
        ),
        Valid(
            "Notation Declared",
            "4.2.2",
            "XML5025",
            [Vc + "NotationDeclared_UnparsedEntityWithADeclaredNotation_IsValid"],
            [Vc + "NotationDeclared_UnparsedEntityWithAnUndeclaredNotation_ReportsXml5025"]
        ),
        Valid(
            "Unique Notation Name",
            "4.7",
            "XML5026",
            [Vc + "UniqueNotationName_DistinctNotationNames_IsValid"],
            [Vc + "UniqueNotationName_NotationDeclaredTwice_ReportsXml5026"]
        ),
    ];

    private static readonly ConstraintEntry[] NamespacesEntries =
    [
        Nsc(
            "Reserved Prefixes and Namespace Names",
            "3",
            "XML3002",
            [Ns + "ReservedPrefixesAndNamespaceNames_BindingThePrefixXmlToItsOwnName_IsWellFormed"],
            [
                Ns + "ReservedPrefixesAndNamespaceNames_BindingThePrefixXmlToAnotherName_IsFatalXml3002",
                Ns + "ReservedPrefixesAndNamespaceNames_BindingTheXmlNamespaceToAnotherPrefix_IsFatalXml3002",
            ]
        ),
        Nsc(
            "No Prefix Undeclaring",
            "5",
            "XML3005",
            [Ns + "NoPrefixUndeclaring_EmptyNameForTheDefaultNamespace_IsWellFormed"],
            [Ns + "NoPrefixUndeclaring_EmptyNameForAPrefix_IsFatalXml3005"],
            "The code table labels XML3005 with the production NSAttName; it reports this constraint."
        ),
        Nsc(
            "Prefix Declared",
            "5",
            "XML3001",
            [Ns + "PrefixDeclared_PrefixDeclaredOnTheElement_IsWellFormed"],
            [
                Ns + "PrefixDeclared_ElementPrefixWithoutADeclaration_IsFatalXml3001",
                Ns + "PrefixDeclared_AttributePrefixWithoutADeclaration_IsFatalXml3001",
            ]
        ),
        Nsc(
            "Attributes Unique",
            "6.3",
            "XML3003",
            [Ns + "AttributesUnique_TwoPrefixesForDifferentNames_IsWellFormed"],
            [Ns + "AttributesUnique_TwoPrefixesForTheSameName_IsFatalXml3003"]
        ),
    ];

    /// <summary>Gets the entries, in the order of the specifications.</summary>
    internal static IReadOnlyList<ConstraintEntry> Entries { get; } =
    [.. WellFormednessEntries, .. ValidityEntries, .. NamespacesEntries];

    private static ConstraintEntry Wfc(
        string name,
        string section,
        string code,
        string[] positive,
        string[] negative,
        string note = ""
    ) => new(ConstraintKind.WellFormedness, name, section, code, positive, negative, Note: note);

    private static ConstraintEntry Valid(
        string name,
        string section,
        string code,
        string[] positive,
        string[] negative,
        string note = ""
    ) => new(ConstraintKind.Validity, name, section, code, positive, negative, Note: note);

    private static ConstraintEntry Gap(
        string name,
        string section,
        string code,
        string[] issues,
        string[] probes,
        string[] positive,
        string[] negative,
        string note = ""
    ) => new(ConstraintKind.Validity, name, section, code, positive, negative, new ConstraintGap(issues, probes), note);

    private static ConstraintEntry Nsc(
        string name,
        string section,
        string code,
        string[] positive,
        string[] negative,
        string note = ""
    ) => new(ConstraintKind.Namespace, name, section, code, positive, negative, Note: note);
}
