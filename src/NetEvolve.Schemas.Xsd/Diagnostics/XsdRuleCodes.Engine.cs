#pragma warning disable NE0002 // Approved public namespace NetEvolve.Schemas.Xsd for the rule catalog (design #146 DP-1)

namespace NetEvolve.Schemas.Xsd;

/// <summary>The rule codes of the Engine family: engine limits (NES4000 to NES4019) and 1.1 construct reporting (NES4020 to NES4099).</summary>
/// <remarks>The strict 1.0 and the not-yet-supported code of a construct share its offset within their blocks (NES4020 and NES4060 for the first one).</remarks>
public static partial class XsdRuleCodes
{
    /// <summary>A content model exceeds the internal ceiling of 65,536 positions.</summary>
    public static readonly DiagnosticCode ContentModelPositionCeilingExceeded = new(4000);

    /// <summary>A content model exceeds the internal ceiling of 1,048,576 transitions.</summary>
    public static readonly DiagnosticCode ContentModelTransitionCeilingExceeded = new(4001);

    /// <summary>The unique particle attribution analysis of a content model exceeds its internal state ceiling.</summary>
    public static readonly DiagnosticCode DeterminismAnalysisCeilingExceeded = new(4002);

    /// <summary>XSD 1.1 construct xs:assert is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10Assert = new(4020);

    /// <summary>XSD 1.1 construct the xs:assertion facet is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10AssertionFacet = new(4021);

    /// <summary>XSD 1.1 construct xs:alternative is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10Alternative = new(4022);

    /// <summary>XSD 1.1 construct xs:openContent is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10OpenContent = new(4023);

    /// <summary>XSD 1.1 construct xs:defaultOpenContent is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10DefaultOpenContent = new(4024);

    /// <summary>XSD 1.1 construct xs:override is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10Override = new(4025);

    /// <summary>XSD 1.1 construct the defaultAttributes attribute is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10DefaultAttributes = new(4026);

    /// <summary>XSD 1.1 construct the defaultAttributesApply attribute is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10DefaultAttributesApply = new(4027);

    /// <summary>XSD 1.1 construct the notNamespace attribute is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10NotNamespace = new(4028);

    /// <summary>XSD 1.1 construct the notQName attribute is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10NotQName = new(4029);

    /// <summary>XSD 1.1 construct the xpathDefaultNamespace attribute is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10XPathDefaultNamespace = new(4030);

    /// <summary>XSD 1.1 construct the inheritable attribute is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10Inheritable = new(4031);

    /// <summary>XSD 1.1 construct the targetNamespace attribute on a local declaration is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10LocalTargetNamespace = new(4032);

    /// <summary>XSD 1.1 construct the explicitTimezone facet is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10ExplicitTimezone = new(4033);

    /// <summary>XSD 1.1 construct the ref attribute on an identity constraint is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10IdentityConstraintRef = new(4034);

    /// <summary>XSD 1.1 construct several substitution group heads is not allowed in strict 1.0 mode.</summary>
    public static readonly DiagnosticCode Strict10SubstitutionGroupList = new(4035);

    /// <summary>XSD 1.1 construct xs:assert is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedAssert = new(4060);

    /// <summary>XSD 1.1 construct the xs:assertion facet is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedAssertionFacet = new(4061);

    /// <summary>XSD 1.1 construct xs:alternative is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedAlternative = new(4062);

    /// <summary>XSD 1.1 construct xs:openContent is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedOpenContent = new(4063);

    /// <summary>XSD 1.1 construct xs:defaultOpenContent is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedDefaultOpenContent = new(4064);

    /// <summary>XSD 1.1 construct xs:override is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedOverride = new(4065);

    /// <summary>XSD 1.1 construct the defaultAttributes attribute is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedDefaultAttributes = new(4066);

    /// <summary>XSD 1.1 construct the defaultAttributesApply attribute is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedDefaultAttributesApply = new(4067);

    /// <summary>XSD 1.1 construct the notNamespace attribute is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedNotNamespace = new(4068);

    /// <summary>XSD 1.1 construct the notQName attribute is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedNotQName = new(4069);

    /// <summary>XSD 1.1 construct the xpathDefaultNamespace attribute is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedXPathDefaultNamespace = new(4070);

    /// <summary>XSD 1.1 construct the inheritable attribute is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedInheritable = new(4071);

    /// <summary>XSD 1.1 construct the targetNamespace attribute on a local declaration is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedLocalTargetNamespace = new(4072);

    /// <summary>XSD 1.1 construct the explicitTimezone facet is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedExplicitTimezone = new(4073);

    /// <summary>XSD 1.1 construct the ref attribute on an identity constraint is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedIdentityConstraintRef = new(4074);

    /// <summary>XSD 1.1 construct several substitution group heads is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedSubstitutionGroupList = new(4075);

    /// <summary>XSD 1.1 construct the xs:precisionDecimal datatype is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedPrecisionDecimal = new(4076);

    /// <summary>XSD 1.1 construct the xs:error datatype is not supported yet.</summary>
    public static readonly DiagnosticCode NotYetSupportedError = new(4077);

    internal static XsdRule[] EngineRules { get; } =
    [
        new(
            ContentModelPositionCeilingExceeded,
            "engine-content-model-positions",
            "A content model exceeds the internal ceiling of 65,536 positions",
            XsdRuleKind.Engine,
            XsdVersionMask.All,
            ""
        ),
        new(
            ContentModelTransitionCeilingExceeded,
            "engine-content-model-transitions",
            "A content model exceeds the internal ceiling of 1,048,576 transitions",
            XsdRuleKind.Engine,
            XsdVersionMask.All,
            ""
        ),
        new(
            DeterminismAnalysisCeilingExceeded,
            "engine-determinism-states",
            "The unique particle attribution analysis of a content model exceeds its internal state ceiling",
            XsdRuleKind.Engine,
            XsdVersionMask.All,
            ""
        ),
        new(
            Strict10Assert,
            "strict10-assert",
            "XSD 1.1 construct xs:assert is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.13.2"
        ),
        new(
            Strict10AssertionFacet,
            "strict10-assertion-facet",
            "XSD 1.1 construct the xs:assertion facet is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Datatypes 1.1, 4.3.13"
        ),
        new(
            Strict10Alternative,
            "strict10-alternative",
            "XSD 1.1 construct xs:alternative is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.12.2"
        ),
        new(
            Strict10OpenContent,
            "strict10-open-content",
            "XSD 1.1 construct xs:openContent is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.4.2"
        ),
        new(
            Strict10DefaultOpenContent,
            "strict10-default-open-content",
            "XSD 1.1 construct xs:defaultOpenContent is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.17.2"
        ),
        new(
            Strict10Override,
            "strict10-override",
            "XSD 1.1 construct xs:override is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 4.2.5"
        ),
        new(
            Strict10DefaultAttributes,
            "strict10-default-attributes",
            "XSD 1.1 construct the defaultAttributes attribute is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.17.2"
        ),
        new(
            Strict10DefaultAttributesApply,
            "strict10-default-attributes-apply",
            "XSD 1.1 construct the defaultAttributesApply attribute is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.4.2"
        ),
        new(
            Strict10NotNamespace,
            "strict10-not-namespace",
            "XSD 1.1 construct the notNamespace attribute is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.10.2"
        ),
        new(
            Strict10NotQName,
            "strict10-not-qname",
            "XSD 1.1 construct the notQName attribute is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.10.2"
        ),
        new(
            Strict10XPathDefaultNamespace,
            "strict10-xpath-default-namespace",
            "XSD 1.1 construct the xpathDefaultNamespace attribute is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.17.2"
        ),
        new(
            Strict10Inheritable,
            "strict10-inheritable",
            "XSD 1.1 construct the inheritable attribute is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.2.2"
        ),
        new(
            Strict10LocalTargetNamespace,
            "strict10-local-target-namespace",
            "XSD 1.1 construct the targetNamespace attribute on a local declaration is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.2.2"
        ),
        new(
            Strict10ExplicitTimezone,
            "strict10-explicit-timezone",
            "XSD 1.1 construct the explicitTimezone facet is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Datatypes 1.1, 4.3.14"
        ),
        new(
            Strict10IdentityConstraintRef,
            "strict10-identity-constraint-ref",
            "XSD 1.1 construct the ref attribute on an identity constraint is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.11.2"
        ),
        new(
            Strict10SubstitutionGroupList,
            "strict10-substitution-group-list",
            "XSD 1.1 construct several substitution group heads is not allowed in strict 1.0 mode",
            XsdRuleKind.Strict10Rejection,
            XsdVersionMask.V10,
            "Structures 1.1, 3.3.2"
        ),
        new(
            NotYetSupportedAssert,
            "not-yet-supported-assert",
            "XSD 1.1 construct xs:assert is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.13.2"
        ),
        new(
            NotYetSupportedAssertionFacet,
            "not-yet-supported-assertion-facet",
            "XSD 1.1 construct the xs:assertion facet is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Datatypes 1.1, 4.3.13"
        ),
        new(
            NotYetSupportedAlternative,
            "not-yet-supported-alternative",
            "XSD 1.1 construct xs:alternative is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.12.2"
        ),
        new(
            NotYetSupportedOpenContent,
            "not-yet-supported-open-content",
            "XSD 1.1 construct xs:openContent is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.4.2"
        ),
        new(
            NotYetSupportedDefaultOpenContent,
            "not-yet-supported-default-open-content",
            "XSD 1.1 construct xs:defaultOpenContent is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.17.2"
        ),
        new(
            NotYetSupportedOverride,
            "not-yet-supported-override",
            "XSD 1.1 construct xs:override is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 4.2.5"
        ),
        new(
            NotYetSupportedDefaultAttributes,
            "not-yet-supported-default-attributes",
            "XSD 1.1 construct the defaultAttributes attribute is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.17.2"
        ),
        new(
            NotYetSupportedDefaultAttributesApply,
            "not-yet-supported-default-attributes-apply",
            "XSD 1.1 construct the defaultAttributesApply attribute is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.4.2"
        ),
        new(
            NotYetSupportedNotNamespace,
            "not-yet-supported-not-namespace",
            "XSD 1.1 construct the notNamespace attribute is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.10.2"
        ),
        new(
            NotYetSupportedNotQName,
            "not-yet-supported-not-qname",
            "XSD 1.1 construct the notQName attribute is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.10.2"
        ),
        new(
            NotYetSupportedXPathDefaultNamespace,
            "not-yet-supported-xpath-default-namespace",
            "XSD 1.1 construct the xpathDefaultNamespace attribute is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.17.2"
        ),
        new(
            NotYetSupportedInheritable,
            "not-yet-supported-inheritable",
            "XSD 1.1 construct the inheritable attribute is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.2.2"
        ),
        new(
            NotYetSupportedLocalTargetNamespace,
            "not-yet-supported-local-target-namespace",
            "XSD 1.1 construct the targetNamespace attribute on a local declaration is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.2.2"
        ),
        new(
            NotYetSupportedExplicitTimezone,
            "not-yet-supported-explicit-timezone",
            "XSD 1.1 construct the explicitTimezone facet is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Datatypes 1.1, 4.3.14"
        ),
        new(
            NotYetSupportedIdentityConstraintRef,
            "not-yet-supported-identity-constraint-ref",
            "XSD 1.1 construct the ref attribute on an identity constraint is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.11.2"
        ),
        new(
            NotYetSupportedSubstitutionGroupList,
            "not-yet-supported-substitution-group-list",
            "XSD 1.1 construct several substitution group heads is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            "Structures 1.1, 3.3.2"
        ),
        new(
            NotYetSupportedPrecisionDecimal,
            "not-yet-supported-precision-decimal",
            "XSD 1.1 construct the xs:precisionDecimal datatype is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            ""
        ),
        new(
            NotYetSupportedError,
            "not-yet-supported-error",
            "XSD 1.1 construct the xs:error datatype is not supported yet",
            XsdRuleKind.NotYetSupported,
            XsdVersionMask.V11,
            ""
        ),
    ];
}
