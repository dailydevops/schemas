#pragma warning disable NE0002 // Approved public namespace NetEvolve.Schemas.Xsd for the rule catalog (design #146 DP-1)

namespace NetEvolve.Schemas.Xsd;

/// <summary>The rule codes of the Validation family, NES4600 to NES4899, in specification order.</summary>
public static partial class XsdRuleCodes
{
    /// <summary>Attribute Locally Valid (<c>cvc-attribute</c>), Structures 1.0, 3.2.4.</summary>
    public static readonly DiagnosticCode CvcAttribute = new(4600);

    /// <summary>Schema-Validity Assessment (Attribute) (<c>cvc-assess-attr</c>), Structures 1.0, 3.2.4.</summary>
    public static readonly DiagnosticCode CvcAssessAttr = new(4601);

    /// <summary>Element Locally Valid (Element) (<c>cvc-elt</c>), Structures 1.0, 3.3.4.</summary>
    public static readonly DiagnosticCode CvcElt = new(4602);

    /// <summary>Element Locally Valid (Type) (<c>cvc-type</c>), Structures 1.0, 3.3.4.</summary>
    public static readonly DiagnosticCode CvcType = new(4603);

    /// <summary>Validation Root Valid (ID/IDREF) (<c>cvc-id</c>), Structures 1.0, 3.3.4.</summary>
    public static readonly DiagnosticCode CvcId = new(4604);

    /// <summary>Schema-Validity Assessment (Element) (<c>cvc-assess-elt</c>), Structures 1.0, 3.3.4.</summary>
    public static readonly DiagnosticCode CvcAssessElt = new(4605);

    /// <summary>Element Locally Valid (Complex Type) (<c>cvc-complex-type</c>), Structures 1.0, 3.4.4.</summary>
    public static readonly DiagnosticCode CvcComplexType = new(4606);

    /// <summary>Attribute Locally Valid (Use) (<c>cvc-au</c>), Structures 1.0, 3.5.4.</summary>
    public static readonly DiagnosticCode CvcAu = new(4607);

    /// <summary>Element Sequence Valid (<c>cvc-model-group</c>), Structures 1.0, 3.8.4.</summary>
    public static readonly DiagnosticCode CvcModelGroup = new(4608);

    /// <summary>Element Sequence Locally Valid (Particle) (<c>cvc-particle</c>), Structures 1.0, 3.9.4.</summary>
    public static readonly DiagnosticCode CvcParticle = new(4609);

    /// <summary>Item Valid (Wildcard) (<c>cvc-wildcard</c>), Structures 1.0, 3.10.4.</summary>
    public static readonly DiagnosticCode CvcWildcard = new(4610);

    /// <summary>Wildcard allows Namespace Name (<c>cvc-wildcard-namespace</c>), Structures 1.0, 3.10.4.</summary>
    public static readonly DiagnosticCode CvcWildcardNamespace = new(4611);

    /// <summary>Identity-constraint Satisfied (<c>cvc-identity-constraint</c>), Structures 1.0, 3.11.4.</summary>
    public static readonly DiagnosticCode CvcIdentityConstraint = new(4612);

    /// <summary>String Valid (<c>cvc-simple-type</c>), Structures 1.0, 3.14.4.</summary>
    public static readonly DiagnosticCode CvcSimpleType = new(4613);

    /// <summary>QName resolution (Instance) (<c>cvc-resolve-instance</c>), Structures 1.0, 3.15.4.</summary>
    public static readonly DiagnosticCode CvcResolveInstance = new(4614);

    /// <summary>Facet Valid (<c>cvc-facet-valid</c>), Datatypes 1.0, 4.1.4.</summary>
    public static readonly DiagnosticCode CvcFacetValid = new(4615);

    /// <summary>Datatype Valid (<c>cvc-datatype-valid</c>), Datatypes 1.0, 4.1.4.</summary>
    public static readonly DiagnosticCode CvcDatatypeValid = new(4616);

    /// <summary>Length Valid (<c>cvc-length-valid</c>), Datatypes 1.0, 4.3.1.3.</summary>
    public static readonly DiagnosticCode CvcLengthValid = new(4617);

    /// <summary>minLength Valid (<c>cvc-minLength-valid</c>), Datatypes 1.0, 4.3.2.3.</summary>
    public static readonly DiagnosticCode CvcMinLengthValid = new(4618);

    /// <summary>maxLength Valid (<c>cvc-maxLength-valid</c>), Datatypes 1.0, 4.3.3.3.</summary>
    public static readonly DiagnosticCode CvcMaxLengthValid = new(4619);

    /// <summary>pattern valid (<c>cvc-pattern-valid</c>), Datatypes 1.0, 4.3.4.4.</summary>
    public static readonly DiagnosticCode CvcPatternValid = new(4620);

    /// <summary>enumeration valid (<c>cvc-enumeration-valid</c>), Datatypes 1.0, 4.3.5.4.</summary>
    public static readonly DiagnosticCode CvcEnumerationValid = new(4621);

    /// <summary>maxInclusive Valid (<c>cvc-maxInclusive-valid</c>), Datatypes 1.0, 4.3.7.3.</summary>
    public static readonly DiagnosticCode CvcMaxInclusiveValid = new(4622);

    /// <summary>maxExclusive Valid (<c>cvc-maxExclusive-valid</c>), Datatypes 1.0, 4.3.8.3.</summary>
    public static readonly DiagnosticCode CvcMaxExclusiveValid = new(4623);

    /// <summary>minExclusive Valid (<c>cvc-minExclusive-valid</c>), Datatypes 1.0, 4.3.9.3.</summary>
    public static readonly DiagnosticCode CvcMinExclusiveValid = new(4624);

    /// <summary>minInclusive Valid (<c>cvc-minInclusive-valid</c>), Datatypes 1.0, 4.3.10.3.</summary>
    public static readonly DiagnosticCode CvcMinInclusiveValid = new(4625);

    /// <summary>totalDigits Valid (<c>cvc-totalDigits-valid</c>), Datatypes 1.0, 4.3.11.3.</summary>
    public static readonly DiagnosticCode CvcTotalDigitsValid = new(4626);

    /// <summary>fractionDigits Valid (<c>cvc-fractionDigits-valid</c>), Datatypes 1.0, 4.3.12.3.</summary>
    public static readonly DiagnosticCode CvcFractionDigitsValid = new(4627);

    /// <summary>Element Sequence Locally Valid (Complex Content) (<c>cvc-complex-content</c>), Structures 1.1, 3.4.4.3.</summary>
    public static readonly DiagnosticCode CvcComplexContent = new(4628);

    /// <summary>Element Sequence Accepted (Particle) (<c>cvc-accept</c>), Structures 1.1, 3.9.4.3.</summary>
    public static readonly DiagnosticCode CvcAccept = new(4629);

    /// <summary>Wildcard allows Expanded Name (<c>cvc-wildcard-name</c>), Structures 1.1, 3.10.4.2.</summary>
    public static readonly DiagnosticCode CvcWildcardName = new(4630);

    /// <summary>Assertion Satisfied (<c>cvc-assertion</c>), Structures 1.1, 3.13.4.1.</summary>
    public static readonly DiagnosticCode CvcAssertion = new(4631);

    /// <summary>XPath Evaluation (<c>cvc-xpath</c>), Structures 1.1, 3.13.4.2.</summary>
    public static readonly DiagnosticCode CvcXpath = new(4632);

    /// <summary>Assertions Valid (<c>cvc-assertions-valid</c>), Datatypes 1.1, 4.3.13.3.</summary>
    public static readonly DiagnosticCode CvcAssertionsValid = new(4633);

    /// <summary>explicitOffset Valid (<c>cvc-explicitTimezone-valid</c>), Datatypes 1.1, 4.3.14.3.</summary>
    public static readonly DiagnosticCode CvcExplicitTimezoneValid = new(4634);

    internal static XsdRule[] ValidationRules { get; } =
    [
        new(
            CvcAttribute,
            "cvc-attribute",
            "Attribute Locally Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.2.4"
        ),
        new(
            CvcAssessAttr,
            "cvc-assess-attr",
            "Schema-Validity Assessment (Attribute)",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.2.4"
        ),
        new(
            CvcElt,
            "cvc-elt",
            "Element Locally Valid (Element)",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.3.4"
        ),
        new(
            CvcType,
            "cvc-type",
            "Element Locally Valid (Type)",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.3.4"
        ),
        new(
            CvcId,
            "cvc-id",
            "Validation Root Valid (ID/IDREF)",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.3.4"
        ),
        new(
            CvcAssessElt,
            "cvc-assess-elt",
            "Schema-Validity Assessment (Element)",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.3.4"
        ),
        new(
            CvcComplexType,
            "cvc-complex-type",
            "Element Locally Valid (Complex Type)",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.4.4"
        ),
        new(
            CvcAu,
            "cvc-au",
            "Attribute Locally Valid (Use)",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.5.4"
        ),
        new(
            CvcModelGroup,
            "cvc-model-group",
            "Element Sequence Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.8.4"
        ),
        new(
            CvcParticle,
            "cvc-particle",
            "Element Sequence Locally Valid (Particle)",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.9.4"
        ),
        new(
            CvcWildcard,
            "cvc-wildcard",
            "Item Valid (Wildcard)",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.10.4"
        ),
        new(
            CvcWildcardNamespace,
            "cvc-wildcard-namespace",
            "Wildcard allows Namespace Name",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.10.4"
        ),
        new(
            CvcIdentityConstraint,
            "cvc-identity-constraint",
            "Identity-constraint Satisfied",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.11.4"
        ),
        new(
            CvcSimpleType,
            "cvc-simple-type",
            "String Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.14.4"
        ),
        new(
            CvcResolveInstance,
            "cvc-resolve-instance",
            "QName resolution (Instance)",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Structures 1.0, 3.15.4"
        ),
        new(
            CvcFacetValid,
            "cvc-facet-valid",
            "Facet Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.1.4"
        ),
        new(
            CvcDatatypeValid,
            "cvc-datatype-valid",
            "Datatype Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.1.4"
        ),
        new(
            CvcLengthValid,
            "cvc-length-valid",
            "Length Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.1.3"
        ),
        new(
            CvcMinLengthValid,
            "cvc-minLength-valid",
            "minLength Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.2.3"
        ),
        new(
            CvcMaxLengthValid,
            "cvc-maxLength-valid",
            "maxLength Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.3.3"
        ),
        new(
            CvcPatternValid,
            "cvc-pattern-valid",
            "pattern valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.4.4"
        ),
        new(
            CvcEnumerationValid,
            "cvc-enumeration-valid",
            "enumeration valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.5.4"
        ),
        new(
            CvcMaxInclusiveValid,
            "cvc-maxInclusive-valid",
            "maxInclusive Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.7.3"
        ),
        new(
            CvcMaxExclusiveValid,
            "cvc-maxExclusive-valid",
            "maxExclusive Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.8.3"
        ),
        new(
            CvcMinExclusiveValid,
            "cvc-minExclusive-valid",
            "minExclusive Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.9.3"
        ),
        new(
            CvcMinInclusiveValid,
            "cvc-minInclusive-valid",
            "minInclusive Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.10.3"
        ),
        new(
            CvcTotalDigitsValid,
            "cvc-totalDigits-valid",
            "totalDigits Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.11.3"
        ),
        new(
            CvcFractionDigitsValid,
            "cvc-fractionDigits-valid",
            "fractionDigits Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.All,
            "Datatypes 1.0, 4.3.12.3"
        ),
        new(
            CvcComplexContent,
            "cvc-complex-content",
            "Element Sequence Locally Valid (Complex Content)",
            XsdRuleKind.Validation,
            XsdVersionMask.V11,
            "Structures 1.1, 3.4.4.3"
        ),
        new(
            CvcAccept,
            "cvc-accept",
            "Element Sequence Accepted (Particle)",
            XsdRuleKind.Validation,
            XsdVersionMask.V11,
            "Structures 1.1, 3.9.4.3"
        ),
        new(
            CvcWildcardName,
            "cvc-wildcard-name",
            "Wildcard allows Expanded Name",
            XsdRuleKind.Validation,
            XsdVersionMask.V11,
            "Structures 1.1, 3.10.4.2"
        ),
        new(
            CvcAssertion,
            "cvc-assertion",
            "Assertion Satisfied",
            XsdRuleKind.Validation,
            XsdVersionMask.V11,
            "Structures 1.1, 3.13.4.1"
        ),
        new(
            CvcXpath,
            "cvc-xpath",
            "XPath Evaluation",
            XsdRuleKind.Validation,
            XsdVersionMask.V11,
            "Structures 1.1, 3.13.4.2"
        ),
        new(
            CvcAssertionsValid,
            "cvc-assertions-valid",
            "Assertions Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.V11,
            "Datatypes 1.1, 4.3.13.3"
        ),
        new(
            CvcExplicitTimezoneValid,
            "cvc-explicitTimezone-valid",
            "explicitOffset Valid",
            XsdRuleKind.Validation,
            XsdVersionMask.V11,
            "Datatypes 1.1, 4.3.14.3"
        ),
    ];
}
