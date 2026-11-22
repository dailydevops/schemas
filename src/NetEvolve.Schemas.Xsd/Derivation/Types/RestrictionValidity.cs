namespace NetEvolve.Schemas.Xsd.Derivation.Types;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>Derivation Valid (Restriction, Complex) (<c>derivation-ok-restriction</c>, Structures 3.4.6).</summary>
/// <remarks>
/// Limitations: the clause 5.4.2 (the particle of the type is a valid restriction of the particle of the base type, Particle
/// Valid (Restriction)) is XSD-62 for XSD 1.0 and the subsumption check of XSD 1.1 is XSD-61, so the content of element-only and
/// mixed types is compared by kind only; the clause 2.1.3 compares fixed values by their lexical form, not by the value of the
/// attribute type; the XSD 1.0 constraint applies in both versions (open content and assertions are not considered); the first
/// violated clause is the one reported.
/// </remarks>
internal static class RestrictionValidity
{
    /// <summary>Checks a type that is derived by restriction against its base type.</summary>
    /// <param name="type">The derived type.</param>
    /// <param name="baseType">The base type to check against.</param>
    /// <param name="derivation">Type Derivation OK, for the types of attributes and of simple content.</param>
    /// <returns>The first violated clause, none when the type is a valid restriction.</returns>
    internal static TypeViolation Check(ComplexTypeDefinition type, IXsdComponent baseType, TypeDerivationOk derivation)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(baseType);
        ArgumentNullException.ThrowIfNull(derivation);

        var complexBase = TypeComponents.AsComplex(baseType);
        if (complexBase is null || (complexBase.Final & XsdDerivationMethod.Restriction) != XsdDerivationMethod.None)
        {
            return Violation(
                "1",
                "The base type of a restriction must be a complex type that is not final for restriction."
            );
        }

        var violation = CheckAttributeUses(type, complexBase, derivation);
        if (violation.IsViolation)
        {
            return violation;
        }

        violation = CheckAttributeWildcard(type, complexBase);
        return violation.IsViolation ? violation : CheckContent(type, complexBase, derivation);
    }

    private static TypeViolation CheckAttributeUses(
        ComplexTypeDefinition type,
        ComplexTypeDefinition baseType,
        TypeDerivationOk derivation
    )
    {
        var baseUses = TypeComponents.IndexUses(baseType.AttributeUses);
        for (var i = 0; i < type.AttributeUses.Count; i++)
        {
            var use = type.AttributeUses[i];
            var violation = baseUses.TryGetValue(use.Name, out var baseUse)
                ? CheckRestatedUse(use, baseUse, derivation)
                : CheckNewUse(use, baseType);
            if (violation.IsViolation)
            {
                return violation;
            }
        }

        return CheckRequiredUses(type, baseType);
    }

    // The clause 2.2: a use that the base type does not have must be allowed by its attribute wildcard.
    private static TypeViolation CheckNewUse(AttributeUse use, ComplexTypeDefinition baseType) =>
        baseType.AttributeWildcard is { } wildcard && wildcard.Allows(use.Name)
            ? TypeViolation.None
            : Violation(
                "2.2",
                "The attribute '"
                    + use.Name.LocalName
                    + "' is neither an attribute of the base type nor allowed by its attribute wildcard."
            );

    // The clause 2.1: a use that restates one of the base type.
    private static TypeViolation CheckRestatedUse(AttributeUse use, AttributeUse baseUse, TypeDerivationOk derivation)
    {
        var name = use.Name.LocalName;
        if (baseUse.Required && !use.Required)
        {
            return Violation(
                "2.1.1",
                "The attribute '" + name + "' is required in the base type and must stay required."
            );
        }

        if (
            derivation
                .Check(use.Declaration.TypeDefinition, baseUse.Declaration.TypeDefinition, XsdDerivationMethod.None)
                .IsViolation
        )
        {
            return Violation(
                "2.1.2",
                "The type of the attribute '" + name + "' must be derived from the type in the base type."
            );
        }

        return
            baseUse.ValueConstraint.Kind == AttributeValueConstraintKind.Fixed
            && (
                use.ValueConstraint.Kind != AttributeValueConstraintKind.Fixed
                || !string.Equals(use.ValueConstraint.Value, baseUse.ValueConstraint.Value, StringComparison.Ordinal)
            )
            ? Violation(
                "2.1.3",
                "The attribute '" + name + "' is fixed in the base type and must keep the same fixed value."
            )
            : TypeViolation.None;
    }

    private static TypeViolation CheckRequiredUses(ComplexTypeDefinition type, ComplexTypeDefinition baseType)
    {
        var uses = TypeComponents.IndexUses(type.AttributeUses);
        for (var i = 0; i < baseType.AttributeUses.Count; i++)
        {
            var baseUse = baseType.AttributeUses[i];
            if (baseUse.Required && (!uses.TryGetValue(baseUse.Name, out var use) || !use.Required))
            {
                return Violation(
                    "3",
                    "The attribute '" + baseUse.Name.LocalName + "' is required in the base type and must be required."
                );
            }
        }

        return TypeViolation.None;
    }

    private static TypeViolation CheckAttributeWildcard(ComplexTypeDefinition type, ComplexTypeDefinition baseType)
    {
        if (type.AttributeWildcard is not { } wildcard)
        {
            return TypeViolation.None;
        }

        if (baseType.AttributeWildcard is not { } baseWildcard)
        {
            return Violation("4.1", "The base type must have an attribute wildcard when the type has one.");
        }

        if (!wildcard.IsSubsetOf(baseWildcard))
        {
            return Violation(
                "4.2",
                "The attribute wildcard of the type must not allow a namespace that the one of the base type does not."
            );
        }

        // Strict, lax, skip are ordered from the strongest to the weakest. The ur-type has no process contents to keep.
        return !TypeComponents.IsUrType(baseType) && wildcard.ProcessContents > baseWildcard.ProcessContents
            ? Violation(
                "4.3",
                "The process contents of the attribute wildcard must be identical to or stronger than that of the base type."
            )
            : TypeViolation.None;
    }

    private static TypeViolation CheckContent(
        ComplexTypeDefinition type,
        ComplexTypeDefinition baseType,
        TypeDerivationOk derivation
    )
    {
        if (TypeComponents.IsUrType(baseType))
        {
            return TypeViolation.None;
        }

        var kind = type.ContentKind;
        var baseKind = baseType.ContentKind;
        switch (kind)
        {
            case ComplexContentKind.Simple:
                return CheckSimpleContent(type, baseType, derivation);
            case ComplexContentKind.Empty:
                return
                    baseKind == ComplexContentKind.Empty
                    || (
                        baseKind is ComplexContentKind.ElementOnly or ComplexContentKind.Mixed
                        && baseType.ContentParticle is { } particle
                        && ParticleRules.IsEmptiable(particle)
                    )
                    ? TypeViolation.None
                    : Violation("5.3.2", "The base type must be empty or have content that may be empty.");
            default:
                // Mixed content restricts mixed content only, element-only content may restrict either. Whether the particle
                // is a valid restriction (5.4.2) is not decided here.
                return kind == ComplexContentKind.ElementOnly || baseKind == ComplexContentKind.Mixed
                    ? TypeViolation.None
                    : Violation(
                        "5.4.1",
                        "Mixed content can only restrict mixed content, element-only content is the alternative."
                    );
        }
    }

    private static TypeViolation CheckSimpleContent(
        ComplexTypeDefinition type,
        ComplexTypeDefinition baseType,
        TypeDerivationOk derivation
    )
    {
        switch (baseType.ContentKind)
        {
            case ComplexContentKind.Simple:
                return
                    type.SimpleContentType is { } simple
                    && baseType.SimpleContentType is { } baseSimple
                    && !derivation.Check(simple, baseSimple, XsdDerivationMethod.None).IsViolation
                    ? TypeViolation.None
                    : Violation("5.2.2.1", "The simple content type must be derived from that of the base type.");
            case ComplexContentKind.Mixed:
                return baseType.ContentParticle is { } particle && ParticleRules.IsEmptiable(particle)
                    ? TypeViolation.None
                    : Violation("5.2.2.2", "The mixed content of the base type must be able to be empty.");
            default:
                return Violation(
                    "5.2.2",
                    "The base type of a type with simple content must have simple content, or mixed content that may be empty."
                );
        }
    }

    private static TypeViolation Violation(string clause, string message) =>
        new(XsdRuleCodes.DerivationOkRestriction, "derivation-ok-restriction." + clause, message);
}
