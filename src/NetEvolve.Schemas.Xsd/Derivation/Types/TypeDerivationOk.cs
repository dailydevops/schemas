namespace NetEvolve.Schemas.Xsd.Derivation.Types;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;

/// <summary>
/// Type Derivation OK for complex types (<c>cos-ct-derived-ok</c>, Structures 3.4.6) and for simple types
/// (<c>cos-st-derived-ok</c>, Structures 3.14.6): whether one type is validly derived from another given a set of blocked
/// derivation methods.
/// </summary>
/// <remarks>
/// <para>
/// The chain of base types is walked in a loop, so its length costs no stack. The base of a built-in type that reaches a schema
/// as a wrapper of its datatype (which keeps no base) is found by name among the built-in types of the version; a list or union
/// that has no base is derived from <c>xs:anySimpleType</c>.
/// </para>
/// <para>
/// Limitations: only restriction is relevant for a simple type, as the specification says, and an extension step is never
/// blocked for one; the <c>final</c> of a base is read for simple types only, as the specification reads it for them (for
/// complex types it is <c>cos-ct-extends</c> and <c>derivation-ok-restriction</c>, checked when the type is defined); the
/// clause <c>2.1</c> of <c>cos-st-derived-ok</c> is read as "restriction is neither in the subset nor in the final of the
/// base type" (the specification text leaves this ambiguous).
/// </para>
/// </remarks>
internal sealed class TypeDerivationOk
{
    private const XsdDerivationMethod TypeMethods = XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction;

    private readonly Dictionary<XPathQName, XsdBuiltInType> _builtIns = [];
    private readonly IXsdComponent? _simpleUrType;

    internal TypeDerivationOk(XsdVersion version)
    {
        var types = XsdBuiltInTypes.For(version);
        for (var i = 0; i < types.Count; i++)
        {
            if (types[i] is XsdBuiltInType builtIn)
            {
                _builtIns[builtIn.Name] = builtIn;
            }
        }

        _ = _builtIns.TryGetValue(new XPathQName(SchemaGrammar.Namespace, "anySimpleType"), out var simpleUrType);
        _simpleUrType = simpleUrType;
    }

    /// <summary>Checks that a type is validly derived from another.</summary>
    /// <param name="derived">The derived type, a complex or a simple type.</param>
    /// <param name="baseType">The base type.</param>
    /// <param name="subset">The blocked derivation methods; only extension and restriction count.</param>
    /// <returns>The violation, none when the derivation is valid.</returns>
    internal TypeViolation Check(IXsdComponent derived, IXsdComponent baseType, XsdDerivationMethod subset)
    {
        ArgumentNullException.ThrowIfNull(derived);
        ArgumentNullException.ThrowIfNull(baseType);

        subset &= TypeMethods;
        return derived.Kind == XsdComponentKind.ComplexType
            ? CheckComplex(derived, baseType, subset)
            : CheckSimple(derived, baseType, subset);
    }

    private TypeViolation CheckComplex(IXsdComponent derived, IXsdComponent baseType, XsdDerivationMethod subset)
    {
        var current = derived;
        while (!TypeComponents.IsSame(current, baseType))
        {
            var parent = BaseOf(current);
            if (parent is null || (TypeComponents.IsUrType(parent) && !TypeComponents.IsSame(parent, baseType)))
            {
                return Violation(
                    XsdRuleCodes.CosCtDerivedOk,
                    "cos-ct-derived-ok.2.3.1",
                    TypeComponents.Describe(derived) + " is not derived from " + TypeComponents.Describe(baseType) + "."
                );
            }

            var method = current is ComplexTypeDefinition complex ? complex.DerivationMethod : XsdDerivationMethod.None;
            if ((method & subset) != XsdDerivationMethod.None)
            {
                return Violation(
                    XsdRuleCodes.CosCtDerivedOk,
                    "cos-ct-derived-ok.1",
                    "The derivation of "
                        + TypeComponents.Describe(current)
                        + " by "
                        + (method == XsdDerivationMethod.Extension ? "extension" : "restriction")
                        + " is blocked."
                );
            }

            if (parent.Kind == XsdComponentKind.SimpleType)
            {
                // A complex type with simple content ends in a simple type; the rest of the chain is simple.
                var simple = CheckSimple(parent, baseType, subset);
                return simple.IsViolation
                    ? Violation(XsdRuleCodes.CosCtDerivedOk, "cos-ct-derived-ok.2.3.2.2", simple.Message!)
                    : TypeViolation.None;
            }

            current = parent;
        }

        return TypeViolation.None;
    }

    private TypeViolation CheckSimple(IXsdComponent derived, IXsdComponent baseType, XsdDerivationMethod subset)
    {
        var current = derived;
        while (!TypeComponents.IsSame(current, baseType))
        {
            var parent = BaseOf(current);
            if (parent is null)
            {
                return NotDerived(derived, baseType);
            }

            if (
                ((subset | TypeComponents.FinalOf(parent)) & XsdDerivationMethod.Restriction)
                != XsdDerivationMethod.None
            )
            {
                return Violation(
                    XsdRuleCodes.CosStDerivedOk,
                    "cos-st-derived-ok.2.1",
                    "The restriction of " + TypeComponents.Describe(current) + " is blocked or final."
                );
            }

            if (TypeComponents.IsSame(parent, baseType))
            {
                return TypeViolation.None;
            }

            if (
                baseType is SimpleTypeDefinition { Variety: DatatypeVariety.Union } union
                && IsDerivedFromAMember(current, union, subset)
            )
            {
                return TypeViolation.None;
            }

            if (TypeComponents.IsUrType(parent))
            {
                return NotDerived(derived, baseType);
            }

            current = parent;
        }

        return TypeViolation.None;
    }

    private bool IsDerivedFromAMember(IXsdComponent derived, SimpleTypeDefinition union, XsdDerivationMethod subset)
    {
        for (var i = 0; i < union.MemberTypes.Count; i++)
        {
            if (!CheckSimple(derived, union.MemberTypes[i], subset).IsViolation)
            {
                return true;
            }
        }

        return false;
    }

    private IXsdComponent? BaseOf(IXsdComponent type)
    {
        switch (type)
        {
            case ComplexTypeDefinition complex:
                return complex.BaseType;
            case XsdBuiltInType builtIn:
                return builtIn.BaseType;
            case SimpleTypeDefinition simple:
                if (simple.BaseType is not null)
                {
                    return simple.BaseType;
                }

                if (_builtIns.TryGetValue(simple.Name, out var registered))
                {
                    return registered.BaseType;
                }

                // A list or a union that is not a restriction derives from the simple ur-type.
                return simple.Variety == DatatypeVariety.Atomic ? null : _simpleUrType;
            default:
                return null;
        }
    }

    private static TypeViolation NotDerived(IXsdComponent derived, IXsdComponent baseType) =>
        Violation(
            XsdRuleCodes.CosStDerivedOk,
            "cos-st-derived-ok.2.2",
            TypeComponents.Describe(derived) + " is not derived from " + TypeComponents.Describe(baseType) + "."
        );

    private static TypeViolation Violation(DiagnosticCode code, string clause, string message) =>
        new(code, clause, message);
}
