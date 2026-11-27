namespace NetEvolve.Schemas.Xsd.Derivation.Types;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.OpenContent;

/// <summary>Derivation Valid (Restriction, Complex) (<c>derivation-ok-restriction</c>, Structures 3.4.6).</summary>
/// <remarks>
/// Limitations: the clause 5.4.2 (the particle of the type is a valid restriction of the particle of the base type) is not part
/// of this check; the compilation run composes it after this check, with Particle Valid (Restriction) for XSD 1.0 and the
/// subsumption check of XSD 1.1, so this check compares the content of element-only and mixed types by kind only; the XSD 1.0 constraint applies in both versions (assertions are not considered), plus a check of open content that carries no clause number; the first
/// violated clause is the one reported.
/// </remarks>
internal static class RestrictionValidity
{
    /// <summary>Checks a type that is derived by restriction against its base type.</summary>
    /// <param name="type">The derived type.</param>
    /// <param name="baseType">The base type to check against.</param>
    /// <param name="derivation">Type Derivation OK, for the types of attributes and of simple content.</param>
    /// <param name="values">The value space, which compares the fixed values of attribute uses.</param>
    /// <returns>The first violated clause, none when the type is a valid restriction.</returns>
    internal static TypeViolation Check(
        ComplexTypeDefinition type,
        IXsdComponent baseType,
        TypeDerivationOk derivation,
        ValueSpace values
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(baseType);
        ArgumentNullException.ThrowIfNull(derivation);
        ArgumentNullException.ThrowIfNull(values);

        var complexBase = TypeComponents.AsComplex(baseType);
        if (complexBase is null || (complexBase.Final & XsdDerivationMethod.Restriction) != XsdDerivationMethod.None)
        {
            return Violation(
                "1",
                "The base type of a restriction must be a complex type that is not final for restriction."
            );
        }

        var violation = CheckAttributes(
            new AttributeSide(type.AttributeUses, type.AttributeWildcard),
            new AttributeSide(complexBase.AttributeUses, complexBase.AttributeWildcard),
            TypeComponents.IsUrType(complexBase),
            derivation,
            values
        );
        return violation.IsViolation ? violation : CheckContent(type, complexBase, derivation);
    }

    /// <summary>
    /// Checks the attribute uses and the attribute wildcard of a restriction against those of its base: the clauses 2 to 4 of
    /// <c>derivation-ok-restriction</c>, which the redefinition of an attribute group (<c>src-redefine</c> 7.2.2) applies as well.
    /// </summary>
    /// <param name="restriction">The attributes of the restriction.</param>
    /// <param name="baseSide">The attributes of the base.</param>
    /// <param name="baseIsUrType">Whether the base is the ur-type, which has no process contents to keep.</param>
    /// <param name="derivation">Type Derivation OK, for the types of attributes.</param>
    /// <param name="values">The value space, which compares the fixed values of attribute uses.</param>
    /// <returns>The first violated clause, none when the restriction is valid.</returns>
    internal static TypeViolation CheckAttributes(
        AttributeSide restriction,
        AttributeSide baseSide,
        bool baseIsUrType,
        TypeDerivationOk derivation,
        ValueSpace values
    )
    {
        var violation = CheckAttributeUses(restriction, baseSide, derivation, values);
        return violation.IsViolation ? violation : CheckAttributeWildcard(restriction, baseSide, baseIsUrType);
    }

    private static TypeViolation CheckAttributeUses(
        AttributeSide restriction,
        AttributeSide baseSide,
        TypeDerivationOk derivation,
        ValueSpace values
    )
    {
        var baseUses = TypeComponents.IndexUses(baseSide.Uses);
        for (var i = 0; i < restriction.Uses.Count; i++)
        {
            var use = restriction.Uses[i];
            var violation = baseUses.TryGetValue(use.Name, out var baseUse)
                ? CheckRestatedUse(use, baseUse, derivation, values)
                : CheckNewUse(use, baseSide.Wildcard);
            if (violation.IsViolation)
            {
                return violation;
            }
        }

        return CheckRequiredUses(restriction, baseSide);
    }

    // The clause 2.2: a use that the base does not have must be allowed by its attribute wildcard.
    private static TypeViolation CheckNewUse(AttributeUse use, IWildcard? baseWildcard) =>
        baseWildcard is { } wildcard && wildcard.Allows(use.Name)
            ? TypeViolation.None
            : Violation(
                "2.2",
                "The attribute '"
                    + use.Name.LocalName
                    + "' is neither an attribute of the base nor allowed by its attribute wildcard."
            );

    // The clause 2.1: a use that restates one of the base type.
    private static TypeViolation CheckRestatedUse(
        AttributeUse use,
        AttributeUse baseUse,
        TypeDerivationOk derivation,
        ValueSpace values
    )
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
                || !values.AreEqual(
                    use.Declaration.TypeDefinition,
                    (use.ValueConstraint.Value ?? string.Empty).AsSpan(),
                    baseUse.Declaration.TypeDefinition,
                    (baseUse.ValueConstraint.Value ?? string.Empty).AsSpan()
                )
            )
            ? Violation(
                "2.1.3",
                "The attribute '" + name + "' is fixed in the base type and must keep the same fixed value."
            )
            : TypeViolation.None;
    }

    private static TypeViolation CheckRequiredUses(AttributeSide restriction, AttributeSide baseSide)
    {
        var uses = TypeComponents.IndexUses(restriction.Uses);
        for (var i = 0; i < baseSide.Uses.Count; i++)
        {
            var baseUse = baseSide.Uses[i];
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

    private static TypeViolation CheckAttributeWildcard(
        AttributeSide restriction,
        AttributeSide baseSide,
        bool baseIsUrType
    )
    {
        if (restriction.Wildcard is not { } wildcard)
        {
            return TypeViolation.None;
        }

        if (baseSide.Wildcard is not { } baseWildcard)
        {
            return Violation("4.1", "The base must have an attribute wildcard when the restriction has one.");
        }

        if (!wildcard.IsSubsetOf(baseWildcard))
        {
            return Violation(
                "4.2",
                "The attribute wildcard must not allow a namespace that the one of the base does not."
            );
        }

        // Strict, lax, skip are ordered from the strongest to the weakest. The ur-type has no process contents to keep.
        return !baseIsUrType && wildcard.ProcessContents > baseWildcard.ProcessContents
            ? Violation(
                "4.3",
                "The process contents of the attribute wildcard must be identical to or stronger than that of the base."
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
                // is a valid restriction (5.4.2) is decided by the particle check the compilation run composes.
                return kind != ComplexContentKind.ElementOnly && baseKind != ComplexContentKind.Mixed
                    ? Violation(
                        "5.4.1",
                        "Mixed content can only restrict mixed content, element-only content is the alternative."
                    )
                    : CheckOpenContent(type.OpenContent, baseType.OpenContent);
        }
    }

    // XSD 1.1: open content of a restriction must not allow more than that of its base (the clause is not numbered here).
    private static TypeViolation CheckOpenContent(OpenContentProperty? open, OpenContentProperty? baseOpen) =>
        open is null
        || (
            baseOpen is not null
            && !(open.Mode == OpenContentMode.Interleave && baseOpen.Mode == OpenContentMode.Suffix)
            && open.Wildcard.IsSubsetOf(baseOpen.Wildcard)
        )
            ? TypeViolation.None
            : Violation(
                "open-content",
                "The open content of a restriction must not allow more than the open content of its base type."
            );

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
