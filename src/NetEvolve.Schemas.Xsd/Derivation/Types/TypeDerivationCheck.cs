namespace NetEvolve.Schemas.Xsd.Derivation.Types;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Attributes.Declarations;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;

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
    private readonly ValueSpace _values;

    /// <summary>Initializes a new instance of the <see cref="TypeDerivationCheck"/> class.</summary>
    /// <param name="version">The XSD version, which selects the built-in types.</param>
    /// <param name="values">The value space, which compares the fixed values of attribute uses.</param>
    internal TypeDerivationCheck(XsdVersion version, ValueSpace values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        _derivation = new TypeDerivationOk(version);
        _values = values;
    }

    /// <inheritdoc />
    public DerivationResult Check(
        IXsdComponent derived,
        IXsdComponent baseType,
        XsdDerivationMethod method,
        XsdDerivationMethod blockSet
    )
    {
        ArgumentNullException.ThrowIfNull(derived);
        ArgumentNullException.ThrowIfNull(baseType);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)method, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)method, (int)AllMethods);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)blockSet, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)blockSet, (int)AllMethods);

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
                ? RestrictionValidity.Check(restriction, baseType, _derivation, _values)
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
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(reporter);

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
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(baseType);
        ArgumentNullException.ThrowIfNull(reporter);

        var violation =
            type.DerivationMethod == XsdDerivationMethod.Extension
                ? ExtensionValidity.Check(type, baseType)
                : RestrictionValidity.Check(type, baseType, _derivation, _values);
        if (violation.IsViolation)
        {
            reporter.Report(XsdRuleLookup.Find(violation.Code), type.Location, violation.Message!, violation.Clause);
        }
    }

    /// <summary>
    /// Reports an attribute group that is redefined without a self-reference and is no restriction of the original (<c>src-redefine</c>
    /// 7.2.2): the uses and the wildcard are compared like those of a complex type that restricts its base type.
    /// </summary>
    /// <param name="redefinition">The redefinition.</param>
    /// <param name="original">The original, which the loader renamed.</param>
    /// <param name="reporter">The reporter.</param>
    internal void ReportRedefinition(
        AttributeGroupDefinition redefinition,
        AttributeGroupDefinition original,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(redefinition);
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(reporter);

        var violation = RestrictionValidity.CheckAttributes(
            new AttributeSide(redefinition.Uses, redefinition.AttributeWildcard),
            new AttributeSide(original.Uses, original.AttributeWildcard),
            false,
            _derivation,
            _values
        );
        if (violation.IsViolation)
        {
            reporter.Report(
                XsdRuleLookup.Find(XsdRuleCodes.SrcRedefine),
                redefinition.Location,
                "The attribute group '"
                    + redefinition.Name.LocalName
                    + "' is redefined without a reference to itself and must restrict the original ("
                    + violation.Clause
                    + "): "
                    + violation.Message,
                "src-redefine.7.2.2"
            );
        }
    }

    /// <summary>Reports the violations of the derivation of every complex type of a list of components.</summary>
    /// <param name="components">The components; types of other kinds are skipped.</param>
    /// <param name="reporter">The reporter.</param>
    internal void ReportAll(IReadOnlyList<IXsdComponent> components, XsdIssueReporter reporter)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(reporter);

        for (var i = 0; i < components.Count; i++)
        {
            var component = components[i];
            ArgumentNullException.ThrowIfNull(component);

            if (component is ComplexTypeDefinition type)
            {
                Report(type, reporter);
            }
        }
    }
}
