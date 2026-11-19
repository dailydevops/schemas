namespace NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// Checks that the default or fixed value of an element or attribute is valid for its type (Structures sections 3.2.6 and 3.3.6):
/// the value is assessed against the simple type, or, for an element of a complex type, against its simple content; a complex type
/// with element-only or empty content, or with mixed content whose particle cannot be empty, cannot have a value constraint.
/// </summary>
/// <remarks>
/// Limitations: the value is assessed through the assessor, so the own facets of a user-defined simple type are not evaluated (#600);
/// the rule that an ID type may not carry a value constraint is not checked; the lexical value is not converted to its canonical
/// form, as the specification does not need it for the check. An attribute use is checked against the type of its declaration; the
/// agreement of a use with the value constraint of its declaration is checked when the use is built.
/// </remarks>
internal static class ValueConstraintCheck
{
    /// <summary>Checks the value constraint of an element declaration.</summary>
    /// <param name="element">The declaration, after its type is resolved.</param>
    /// <param name="assessor">The assessor that checks the value against the type.</param>
    /// <param name="version">The XSD version.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns><see langword="true"/> when the declaration has no constraint, or when the constraint is valid.</returns>
    internal static bool Check(
        ElementDeclaration element,
        ISimpleValueAssessor assessor,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        Argument.ThrowIfNull(element);
        Argument.ThrowIfNull(assessor);
        Argument.ThrowIfNull(reporter);
        GuardVersion(version);

        var constraint = element.ValueConstraint;
        if (constraint.Kind == ElementValueConstraintKind.None)
        {
            return true;
        }

        switch (element.TypeDefinition)
        {
            case null:
                // An unresolved type is reported by the resolver; a second error would only repeat it.
                return true;
            case ComplexTypeDefinition complex:
                return CheckComplexType(element, complex, constraint.Value, assessor, reporter);
            case { Kind: XsdComponentKind.SimpleType } simple:
                return CheckValue(
                    simple,
                    constraint.Value,
                    assessor,
                    XsdRuleCodes.CosValidDefault,
                    element.Location,
                    reporter
                );
            default:
                // A built-in complex type is anyType, which is mixed with a particle that can be empty.
                return true;
        }
    }

    /// <summary>Checks the value constraint of an attribute declaration.</summary>
    /// <param name="attribute">The declaration.</param>
    /// <param name="assessor">The assessor that checks the value against the type.</param>
    /// <param name="version">The XSD version.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns><see langword="true"/> when the declaration has no constraint, or when the constraint is valid.</returns>
    internal static bool Check(
        AttributeDeclaration attribute,
        ISimpleValueAssessor assessor,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        Argument.ThrowIfNull(attribute);
        Argument.ThrowIfNull(assessor);
        Argument.ThrowIfNull(reporter);
        GuardVersion(version);

        return CheckAttributeValue(
            attribute.ValueConstraint,
            attribute.TypeDefinition,
            attribute.Location,
            assessor,
            AttributeRule(version),
            reporter
        );
    }

    /// <summary>Checks the value constraint of an attribute use.</summary>
    /// <param name="use">The use.</param>
    /// <param name="assessor">The assessor that checks the value against the type of the declaration.</param>
    /// <param name="version">The XSD version.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns><see langword="true"/> when the use has no constraint, or when the constraint is valid.</returns>
    internal static bool Check(
        AttributeUse use,
        ISimpleValueAssessor assessor,
        XsdVersion version,
        XsdIssueReporter reporter
    )
    {
        Argument.ThrowIfNull(use);
        Argument.ThrowIfNull(assessor);
        Argument.ThrowIfNull(reporter);
        GuardVersion(version);

        // A use without an own constraint inherits the one of its declaration, which is checked with the declaration.
        var inherited = use.Declaration.ValueConstraint;
        if (
            use.ValueConstraint.Kind == inherited.Kind
            && string.Equals(use.ValueConstraint.Value, inherited.Value, StringComparison.Ordinal)
        )
        {
            return true;
        }

        return CheckAttributeValue(
            use.ValueConstraint,
            use.Declaration.TypeDefinition,
            use.Location,
            assessor,
            XsdRuleCodes.AuPropsCorrect,
            reporter
        );
    }

    private static void GuardVersion(XsdVersion version)
    {
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);
    }

    // XSD 1.1 names the value check of an attribute 'Simple Default Valid'; XSD 1.0 states it in a-props-correct.
    private static DiagnosticCode AttributeRule(XsdVersion version) =>
        version == XsdVersion.V11 ? XsdRuleCodes.CosValidSimpleDefault : XsdRuleCodes.APropsCorrect;

    private static bool CheckAttributeValue(
        AttributeValueConstraint constraint,
        IXsdComponent type,
        DocumentLocation location,
        ISimpleValueAssessor assessor,
        DiagnosticCode rule,
        XsdIssueReporter reporter
    ) =>
        constraint.Kind == AttributeValueConstraintKind.None
        || constraint.Value is null
        || CheckValue(type, constraint.Value, assessor, rule, location, reporter);

    private static bool CheckComplexType(
        ElementDeclaration element,
        ComplexTypeDefinition type,
        string value,
        ISimpleValueAssessor assessor,
        XsdIssueReporter reporter
    )
    {
        switch (type.ContentKind)
        {
            case ComplexContentKind.Simple:
                return type.SimpleContentType is null
                    || CheckValue(
                        type.SimpleContentType,
                        value,
                        assessor,
                        XsdRuleCodes.CosValidDefault,
                        element.Location,
                        reporter
                    );
            case ComplexContentKind.Mixed
                when type.ContentParticle is null || ParticleRules.IsEmptiable(type.ContentParticle):
                return true;
            default:
                reporter.Report(
                    XsdRuleCodes.CosValidDefault,
                    element.Location,
                    "A value constraint needs a simple type, or a complex type with simple content or with mixed content that can be empty."
                );
                return false;
        }
    }

    private static bool CheckValue(
        IXsdComponent type,
        string value,
        ISimpleValueAssessor assessor,
        DiagnosticCode rule,
        DocumentLocation location,
        XsdIssueReporter reporter
    )
    {
        if (assessor.Assess(type, value.AsSpan(), out _).IsValid)
        {
            return true;
        }

        reporter.Report(rule, location, "The default or fixed value is not valid for the type.");
        return false;
    }
}
