namespace NetEvolve.Schemas.Xsd.Derivation.Types;

using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>Derivation Valid (Extension) (<c>cos-ct-extends</c>, Structures 3.4.6).</summary>
/// <remarks>
/// Limitations: the clause 1.5 (it must be possible to derive the type in two steps, an extension and then a restriction) is
/// not checked (#675); assertions of XSD 1.1 are not considered, so the XSD 1.0 constraint applies in both versions; the clauses
/// 1.4.3.2.2.3 and 1.4.3.2.2.4 on open content are checked when the complex type is constructed (<c>OpenContentRules</c>), not here;
/// the first violated clause is the one reported.
/// </remarks>
internal static class ExtensionValidity
{
    /// <summary>Checks a type that is derived by extension against its base type.</summary>
    /// <param name="type">The derived type.</param>
    /// <param name="baseType">The base type to check against.</param>
    /// <returns>The first violated clause, none when the type is a valid extension.</returns>
    internal static TypeViolation Check(ComplexTypeDefinition type, IXsdComponent baseType)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(baseType);

        var complexBase = TypeComponents.AsComplex(baseType);
        if (complexBase is not null)
        {
            return CheckComplexBase(type, complexBase);
        }

        if (baseType.Kind == XsdComponentKind.SimpleType)
        {
            return CheckSimpleBase(type, baseType);
        }

        return Violation("cos-ct-extends", "The base type of an extension must be a complex or a simple type.");
    }

    private static TypeViolation CheckComplexBase(ComplexTypeDefinition type, ComplexTypeDefinition baseType)
    {
        if ((baseType.Final & XsdDerivationMethod.Extension) != XsdDerivationMethod.None)
        {
            return Violation(
                "cos-ct-extends.1.1",
                "The base type " + TypeComponents.Describe(baseType) + " is final for extension."
            );
        }

        var uses = TypeComponents.IndexUses(type.AttributeUses);
        for (var i = 0; i < baseType.AttributeUses.Count; i++)
        {
            var baseUse = baseType.AttributeUses[i];
            if (
                !uses.TryGetValue(baseUse.Name, out var use)
                || !TypeComponents.IsSame(use.Declaration.TypeDefinition, baseUse.Declaration.TypeDefinition)
            )
            {
                return Violation(
                    "cos-ct-extends.1.2",
                    "The attribute '"
                        + baseUse.Name.LocalName
                        + "' of the base type is missing from the type, or has another type there."
                );
            }
        }

        if (
            baseType.AttributeWildcard is { } baseWildcard
            && (type.AttributeWildcard is not { } wildcard || !AllowsEverythingOf(baseWildcard, wildcard))
        )
        {
            return Violation(
                "cos-ct-extends.1.3",
                "The attribute wildcard of the type must allow every namespace that the one of the base type allows."
            );
        }

        return CheckContent(type, baseType);
    }

    // The wildcard of an extension is the union with that of the base (Errata E1-10), so a union that the wildcard equals
    // counts as a superset although the intensional subset test rejects not(a) against not(absent).
    private static bool AllowsEverythingOf(IWildcard baseWildcard, IWildcard wildcard) =>
        baseWildcard.IsSubsetOf(wildcard) || baseWildcard.Union(wildcard)?.IsSubsetOf(wildcard) == true;

    private static TypeViolation CheckContent(ComplexTypeDefinition type, ComplexTypeDefinition baseType)
    {
        var kind = type.ContentKind;
        var baseKind = baseType.ContentKind;
        if (kind == ComplexContentKind.Simple && baseKind == ComplexContentKind.Simple)
        {
            return
                type.SimpleContentType is { } simple
                && baseType.SimpleContentType is { } baseSimple
                && TypeComponents.IsSame(simple, baseSimple)
                ? TypeViolation.None
                : Violation("cos-ct-extends.1.4", "The simple content of the type must be that of the base type.");
        }

        if (kind == ComplexContentKind.Empty && baseKind == ComplexContentKind.Empty)
        {
            return TypeViolation.None;
        }

        if (type.ContentParticle is not { } particle)
        {
            return Violation(
                "cos-ct-extends.1.4",
                "The content of the type must extend that of the base type with a particle, or be the same simple type."
            );
        }

        if (baseKind == ComplexContentKind.Empty)
        {
            return TypeViolation.None;
        }

        if (kind != baseKind || baseType.ContentParticle is null)
        {
            return Violation(
                "cos-ct-extends.1.4.3.2.2.1",
                "The content of the type and of the base type must both be mixed or both be element-only."
            );
        }

        return ParticleRules.IsValidExtension(particle, baseType.ContentParticle)
            ? TypeViolation.None
            : Violation(
                "cos-ct-extends.1.4.3.2.2.2",
                "The content of the type must be a sequence that starts with the content of the base type (Particle Valid (Extension))."
            );
    }

    private static TypeViolation CheckSimpleBase(ComplexTypeDefinition type, IXsdComponent baseType)
    {
        if (
            type.ContentKind != ComplexContentKind.Simple
            || type.SimpleContentType is not { } simple
            || !TypeComponents.IsSame(simple, baseType)
        )
        {
            return Violation(
                "cos-ct-extends.2.1",
                "The content type of an extension of a simple type must be that simple type."
            );
        }

        return (TypeComponents.FinalOf(baseType) & XsdDerivationMethod.Extension) != XsdDerivationMethod.None
            ? Violation("cos-ct-extends.2.2", "The base type is final for extension.")
            : TypeViolation.None;
    }

    private static TypeViolation Violation(string clause, string message) =>
        new(XsdRuleCodes.CosCtExtends, clause, message);
}
