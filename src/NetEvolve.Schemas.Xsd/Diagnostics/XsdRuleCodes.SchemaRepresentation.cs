#pragma warning disable NE0002 // Approved public namespace NetEvolve.Schemas.Xsd for the rule catalog (design #146 DP-1)

namespace NetEvolve.Schemas.Xsd;

/// <summary>The rule codes of the SchemaRepresentation family, NES4100 to NES4299, in specification order.</summary>
public static partial class XsdRuleCodes
{
    /// <summary>Attribute Declaration Representation OK (<c>src-attribute</c>), Structures 1.0, 3.2.3.</summary>
    public static readonly DiagnosticCode SrcAttribute = new(4100);

    /// <summary>Element Declaration Representation OK (<c>src-element</c>), Structures 1.0, 3.3.3.</summary>
    public static readonly DiagnosticCode SrcElement = new(4101);

    /// <summary>Complex Type Definition Representation OK (<c>src-ct</c>), Structures 1.0, 3.4.3.</summary>
    public static readonly DiagnosticCode SrcCt = new(4102);

    /// <summary>Attribute Group Definition Representation OK (<c>src-attribute_group</c>), Structures 1.0, 3.6.3.</summary>
    public static readonly DiagnosticCode SrcAttributeGroup = new(4103);

    /// <summary>Model Group Definition Representation OK (<c>src-model_group_defn</c>), Structures 1.0, 3.7.3.</summary>
    public static readonly DiagnosticCode SrcModelGroupDefn = new(4104);

    /// <summary>Model Group Representation OK (<c>src-model_group</c>), Structures 1.0, 3.8.3.</summary>
    public static readonly DiagnosticCode SrcModelGroup = new(4105);

    /// <summary>Wildcard Representation OK (<c>src-wildcard</c>), Structures 1.0, 3.10.3.</summary>
    public static readonly DiagnosticCode SrcWildcard = new(4106);

    /// <summary>Identity-constraint Definition Representation OK (<c>src-identity-constraint</c>), Structures 1.0, 3.11.3.</summary>
    public static readonly DiagnosticCode SrcIdentityConstraint = new(4107);

    /// <summary>Notation Definition Representation OK (<c>src-notation</c>), Structures 1.0, 3.12.3.</summary>
    public static readonly DiagnosticCode SrcNotation = new(4108);

    /// <summary>Annotation Definition Representation OK (<c>src-annotation</c>), Structures 1.0, 3.13.3.</summary>
    public static readonly DiagnosticCode SrcAnnotation = new(4109);

    /// <summary>Simple Type Definition Representation OK (<c>src-simple-type</c>), Structures 1.0, 3.14.3.</summary>
    public static readonly DiagnosticCode SrcSimpleType = new(4110);

    /// <summary>QName Interpretation (<c>src-qname</c>), Structures 1.0, 3.15.3.</summary>
    public static readonly DiagnosticCode SrcQname = new(4111);

    /// <summary>QName resolution (Schema Document) (<c>src-resolve</c>), Structures 1.0, 3.15.3.</summary>
    public static readonly DiagnosticCode SrcResolve = new(4112);

    /// <summary>Inclusion Constraints and Semantics (<c>src-include</c>), Structures 1.0, 4.2.1.</summary>
    public static readonly DiagnosticCode SrcInclude = new(4113);

    /// <summary>Redefinition Constraints and Semantics (<c>src-redefine</c>), Structures 1.0, 4.2.2.</summary>
    public static readonly DiagnosticCode SrcRedefine = new(4114);

    /// <summary>Individual Component Redefinition (<c>src-expredef</c>), Structures 1.0, 4.2.2.</summary>
    public static readonly DiagnosticCode SrcExpredef = new(4115);

    /// <summary>Import Constraints and Semantics (<c>src-import</c>), Structures 1.0, 4.2.3.</summary>
    public static readonly DiagnosticCode SrcImport = new(4116);

    /// <summary>Schema Document Location Strategy (<c>schema_reference</c>), Structures 1.0, 4.3.2.</summary>
    public static readonly DiagnosticCode SchemaReference = new(4117);

    /// <summary>Single Facet Value (<c>src-single-facet-value</c>), Datatypes 1.0, 4.1.3.</summary>
    public static readonly DiagnosticCode SrcSingleFacetValue = new(4118);

    /// <summary>itemType attribute or simpleType child (<c>src-list-itemType-or-simpleType</c>), Datatypes 1.0, 4.1.3.</summary>
    public static readonly DiagnosticCode SrcListItemTypeOrSimpleType = new(4119);

    /// <summary>base attribute or simpleType child (<c>src-restriction-base-or-simpleType</c>), Datatypes 1.0, 4.1.3.</summary>
    public static readonly DiagnosticCode SrcRestrictionBaseOrSimpleType = new(4120);

    /// <summary>memberTypes attribute or simpleType children (<c>src-union-memberTypes-or-simpleTypes</c>), Datatypes 1.0, 4.1.3.</summary>
    public static readonly DiagnosticCode SrcUnionMemberTypesOrSimpleTypes = new(4121);

    /// <summary>Multiple patterns (<c>src-multiple-patterns</c>), Datatypes 1.0, 4.3.4.3.</summary>
    public static readonly DiagnosticCode SrcMultiplePatterns = new(4122);

    /// <summary>Multiple enumerations (<c>src-multiple-enumerations</c>), Datatypes 1.0, 4.3.5.3.</summary>
    public static readonly DiagnosticCode SrcMultipleEnumerations = new(4123);

    /// <summary>Type Alternative Representation OK (<c>src-ta</c>), Structures 1.1, 3.12.3.</summary>
    public static readonly DiagnosticCode SrcTa = new(4124);

    /// <summary>Conditional Inclusion Constraints (<c>src-cip</c>), Structures 1.1, 4.2.2.</summary>
    public static readonly DiagnosticCode SrcCip = new(4125);

    /// <summary>Override Constraints and Semantics (<c>src-override</c>), Structures 1.1, 4.2.5.</summary>
    public static readonly DiagnosticCode SrcOverride = new(4126);

    /// <summary>Pattern value (<c>src-pattern-value</c>), Datatypes 1.1, 4.3.4.3.</summary>
    public static readonly DiagnosticCode SrcPatternValue = new(4127);

    /// <summary>Enumeration value (<c>src-enumeration-value</c>), Datatypes 1.1, 4.3.5.3.</summary>
    public static readonly DiagnosticCode SrcEnumerationValue = new(4128);

    /// <summary>Unknown version control attribute (<c>vc-unknown-attribute</c>), Structures 1.1, 4.2.2.</summary>
    public static readonly DiagnosticCode UnknownVersionControlAttribute = new(4129);

    internal static XsdRule[] SchemaRepresentationRules { get; } =
    [
        new(
            SrcAttribute,
            "src-attribute",
            "Attribute Declaration Representation OK",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Structures 1.0, 3.2.3"
        ),
        new(
            SrcElement,
            "src-element",
            "Element Declaration Representation OK",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Structures 1.0, 3.3.3"
        ),
        new(
            SrcCt,
            "src-ct",
            "Complex Type Definition Representation OK",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Structures 1.0, 3.4.3"
        ),
        new(
            SrcAttributeGroup,
            "src-attribute_group",
            "Attribute Group Definition Representation OK",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Structures 1.0, 3.6.3"
        ),
        new(
            SrcModelGroupDefn,
            "src-model_group_defn",
            "Model Group Definition Representation OK",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V10,
            "Structures 1.0, 3.7.3"
        ),
        new(
            SrcModelGroup,
            "src-model_group",
            "Model Group Representation OK",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V10,
            "Structures 1.0, 3.8.3"
        ),
        new(
            SrcWildcard,
            "src-wildcard",
            "Wildcard Representation OK",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Structures 1.0, 3.10.3"
        ),
        new(
            SrcIdentityConstraint,
            "src-identity-constraint",
            "Identity-constraint Definition Representation OK",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Structures 1.0, 3.11.3"
        ),
        new(
            SrcNotation,
            "src-notation",
            "Notation Definition Representation OK",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V10,
            "Structures 1.0, 3.12.3"
        ),
        new(
            SrcAnnotation,
            "src-annotation",
            "Annotation Definition Representation OK",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V10,
            "Structures 1.0, 3.13.3"
        ),
        new(
            SrcSimpleType,
            "src-simple-type",
            "Simple Type Definition Representation OK",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Structures 1.0, 3.14.3"
        ),
        new(
            SrcQname,
            "src-qname",
            "QName Interpretation",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V10,
            "Structures 1.0, 3.15.3"
        ),
        new(
            SrcResolve,
            "src-resolve",
            "QName resolution (Schema Document)",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Structures 1.0, 3.15.3"
        ),
        new(
            SrcInclude,
            "src-include",
            "Inclusion Constraints and Semantics",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Structures 1.0, 4.2.1"
        ),
        new(
            SrcRedefine,
            "src-redefine",
            "Redefinition Constraints and Semantics",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Structures 1.0, 4.2.2"
        ),
        new(
            SrcExpredef,
            "src-expredef",
            "Individual Component Redefinition",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Structures 1.0, 4.2.2"
        ),
        new(
            SrcImport,
            "src-import",
            "Import Constraints and Semantics",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Structures 1.0, 4.2.3"
        ),
        new(
            SchemaReference,
            "schema_reference",
            "Schema Document Location Strategy",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V10,
            "Structures 1.0, 4.3.2"
        ),
        new(
            SrcSingleFacetValue,
            "src-single-facet-value",
            "Single Facet Value",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V10,
            "Datatypes 1.0, 4.1.3"
        ),
        new(
            SrcListItemTypeOrSimpleType,
            "src-list-itemType-or-simpleType",
            "itemType attribute or simpleType child",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.1.3"
        ),
        new(
            SrcRestrictionBaseOrSimpleType,
            "src-restriction-base-or-simpleType",
            "base attribute or simpleType child",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.1.3"
        ),
        new(
            SrcUnionMemberTypesOrSimpleTypes,
            "src-union-memberTypes-or-simpleTypes",
            "memberTypes attribute or simpleType children",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.1.3"
        ),
        new(
            SrcMultiplePatterns,
            "src-multiple-patterns",
            "Multiple patterns",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V10,
            "Datatypes 1.0, 4.3.4.3"
        ),
        new(
            SrcMultipleEnumerations,
            "src-multiple-enumerations",
            "Multiple enumerations",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V10,
            "Datatypes 1.0, 4.3.5.3"
        ),
        new(
            SrcTa,
            "src-ta",
            "Type Alternative Representation OK",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V11,
            "Structures 1.1, 3.12.3"
        ),
        new(
            SrcCip,
            "src-cip",
            "Conditional Inclusion Constraints",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V11,
            "Structures 1.1, 4.2.2"
        ),
        new(
            SrcOverride,
            "src-override",
            "Override Constraints and Semantics",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V11,
            "Structures 1.1, 4.2.5"
        ),
        new(
            SrcPatternValue,
            "src-pattern-value",
            "Pattern value",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V11,
            "Datatypes 1.1, 4.3.4.3"
        ),
        new(
            SrcEnumerationValue,
            "src-enumeration-value",
            "Enumeration value",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V11,
            "Datatypes 1.1, 4.3.5.3"
        ),
        new(
            UnknownVersionControlAttribute,
            "vc-unknown-attribute",
            "Unknown version control attribute",
            XsdRuleKind.SchemaRepresentation,
            XsdVersionMask.V11,
            "Structures 1.1, 4.2.2",
            IssueSeverity.Warning
        ),
    ];
}
