namespace NetEvolve.Schemas.Xsd.Derivation.Types;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// Checks the derivation of types (Structures 3.4.6 and 3.14.6): the <see cref="IDerivationCheck"/> seam that answers whether
/// one type may stand in for another (<c>xsi:type</c>, substitution groups), and the check of every complex type against its
/// base type that reports a located schema error.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Check"/> reads the <c>method</c> as the context of the question. <see cref="XsdDerivationMethod.Extension"/> asks
/// whether the derived type is a valid extension of the base type (<c>cos-ct-extends</c>), <see cref="XsdDerivationMethod.Restriction"/>
/// whether it is a valid restriction (<c>derivation-ok-restriction</c>; for a simple type Type Derivation OK with nothing
/// blocked), and every other value, in particular <see cref="XsdDerivationMethod.Substitution"/> that the substitution groups and
/// <c>xsi:type</c> pass, asks Type Derivation OK given the extension and restriction bits of the block set. The block set is
/// ignored for the first two.
/// </para>
/// <para>
/// Limitations: see <see cref="ExtensionValidity"/>, <see cref="RestrictionValidity"/>, <see cref="TypeDerivationOk"/>, and
/// <see cref="ParticleRules"/>. The check is a pure function of immutable components and keeps no cache. It is not yet
/// part of a compile pipeline (XSD-75), so a caller runs <see cref="ReportAll"/> on the components of a schema set.
/// </para>
/// </remarks>
internal sealed class TypeDerivationCheck : IDerivationCheck
{
    private const XsdDerivationMethod AllMethods =
        XsdDerivationMethod.Extension
        | XsdDerivationMethod.Restriction
        | XsdDerivationMethod.List
        | XsdDerivationMethod.Union
        | XsdDerivationMethod.Substitution;

    private readonly TypeDerivationOk _derivation;

    /// <summary>Initializes a new instance of the <see cref="TypeDerivationCheck"/> class.</summary>
    /// <param name="version">The XSD version, which selects the built-in types.</param>
    internal TypeDerivationCheck(XsdVersion version)
    {
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        _derivation = new TypeDerivationOk(version);
    }

    /// <inheritdoc />
    public DerivationResult Check(
        IXsdComponent derived,
        IXsdComponent baseType,
        XsdDerivationMethod method,
        XsdDerivationMethod blockSet
    )
    {
        Argument.ThrowIfNull(derived);
        Argument.ThrowIfNull(baseType);
        Argument.ThrowIfLessThan((int)method, 0);
        Argument.ThrowIfGreaterThan((int)method, (int)AllMethods);
        Argument.ThrowIfLessThan((int)blockSet, 0);
        Argument.ThrowIfGreaterThan((int)blockSet, (int)AllMethods);

        var violation = method switch
        {
            XsdDerivationMethod.Extension => derived is ComplexTypeDefinition extension
                ? ExtensionValidity.Check(extension, baseType)
                : new TypeViolation(
                    XsdRuleCodes.CosCtExtends,
                    "cos-ct-extends",
                    "Only a complex type can be derived by extension."
                ),
            XsdDerivationMethod.Restriction => derived is ComplexTypeDefinition restriction
                ? RestrictionValidity.Check(restriction, baseType, _derivation)
                : _derivation.Check(derived, baseType, XsdDerivationMethod.None),
            _ => _derivation.Check(derived, baseType, blockSet),
        };

        return violation.IsViolation
            ? DerivationResult.Invalid(XsdRuleLookup.Find(violation.Code))
            : DerivationResult.Valid;
    }

    /// <summary>Reports the violations of the derivation of one complex type from its base type as a located issue.</summary>
    /// <param name="type">The type; the ur-type has no base type and is not checked.</param>
    /// <param name="reporter">The reporter.</param>
    internal void Report(ComplexTypeDefinition type, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(type);
        Argument.ThrowIfNull(reporter);

        if (type.BaseType is { } baseType)
        {
            Report(type, baseType, reporter);
        }
    }

    /// <summary>Reports the violations of the derivation of one complex type from a given base type as a located issue.</summary>
    /// <param name="type">The type, whose derivation method is the one that is checked.</param>
    /// <param name="baseType">The base type to check against.</param>
    /// <param name="reporter">The reporter.</param>
    internal void Report(ComplexTypeDefinition type, IXsdComponent baseType, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(type);
        Argument.ThrowIfNull(baseType);
        Argument.ThrowIfNull(reporter);

        var violation =
            type.DerivationMethod == XsdDerivationMethod.Extension
                ? ExtensionValidity.Check(type, baseType)
                : RestrictionValidity.Check(type, baseType, _derivation);
        if (violation.IsViolation)
        {
            reporter.Report(XsdRuleLookup.Find(violation.Code), type.Location, violation.Message!, violation.Clause);
        }
    }

    /// <summary>Reports the violations of the derivation of every complex type of a list of components.</summary>
    /// <param name="components">The components; types of other kinds are skipped.</param>
    /// <param name="reporter">The reporter.</param>
    internal void ReportAll(IReadOnlyList<IXsdComponent> components, XsdIssueReporter reporter)
    {
        Argument.ThrowIfNull(components);
        Argument.ThrowIfNull(reporter);

        for (var i = 0; i < components.Count; i++)
        {
            var component = components[i];
            Argument.ThrowIfNull(component);

            if (component is ComplexTypeDefinition type)
            {
                Report(type, reporter);
            }
        }
    }
}
