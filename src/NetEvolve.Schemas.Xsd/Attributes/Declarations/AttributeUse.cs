namespace NetEvolve.Schemas.Xsd.Attributes.Declarations;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;

/// <summary>
/// An attribute use (Structures section 3.5): a declaration with required, optional, or prohibited and an effective value
/// constraint. Immutable.
/// </summary>
/// <remarks>
/// Limitation: two fixed values are compared as strings, not in the value space of the type.
/// </remarks>
internal sealed class AttributeUse : IXsdComponent
{
    private AttributeUse(
        DocumentLocation location,
        AttributeUseKind use,
        AttributeDeclaration declaration,
        AttributeValueConstraint valueConstraint
    )
    {
        Location = location;
        Use = use;
        Declaration = declaration;
        ValueConstraint = valueConstraint;
    }

    /// <inheritdoc />
    public XsdComponentKind Kind => XsdComponentKind.AttributeUse;

    /// <summary>Gets the name of the declaration, the identity of the use inside a group or type.</summary>
    public XPathQName Name => Declaration.Name;

    /// <inheritdoc />
    public DocumentLocation Location { get; }

    /// <summary>Gets the <c>use</c> value.</summary>
    public AttributeUseKind Use { get; }

    /// <summary>Gets a value indicating whether the attribute is required.</summary>
    public bool Required => Use == AttributeUseKind.Required;

    /// <summary>Gets a value indicating whether the attribute is prohibited.</summary>
    public bool Prohibited => Use == AttributeUseKind.Prohibited;

    /// <summary>Gets the declaration.</summary>
    public AttributeDeclaration Declaration { get; }

    /// <summary>Gets the effective value constraint: the own one, else that of the declaration.</summary>
    public AttributeValueConstraint ValueConstraint { get; }

    /// <summary>Creates an attribute use.</summary>
    /// <param name="location">The location of the use.</param>
    /// <param name="use">The use.</param>
    /// <param name="declaration">The resolved declaration.</param>
    /// <param name="valueConstraint">The own value constraint, <see langword="default"/> for none.</param>
    /// <param name="reporter">The reporter.</param>
    /// <returns>The use, <see langword="null"/> after an issue was reported.</returns>
    internal static AttributeUse? Create(
        DocumentLocation location,
        AttributeUseKind use,
        AttributeDeclaration declaration,
        AttributeValueConstraint valueConstraint,
        XsdIssueReporter reporter
    )
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(reporter);

        if (valueConstraint.Kind == AttributeValueConstraintKind.Default && use != AttributeUseKind.Optional)
        {
            reporter.Report(
                XsdRuleCodes.SrcAttribute,
                location,
                "An attribute use with a default value must be optional."
            );
            return null;
        }

        var inherited = declaration.ValueConstraint;
        if (
            valueConstraint.Kind == AttributeValueConstraintKind.Default
            && inherited.Kind == AttributeValueConstraintKind.Fixed
        )
        {
            reporter.Report(
                XsdRuleCodes.AuPropsCorrect,
                location,
                "The use of a declaration with a fixed value must not carry a default value."
            );
            return null;
        }

        if (
            valueConstraint.Kind == AttributeValueConstraintKind.Fixed
            && inherited.Kind == AttributeValueConstraintKind.Fixed
            && !string.Equals(valueConstraint.Value, inherited.Value, StringComparison.Ordinal)
        )
        {
            reporter.Report(
                XsdRuleCodes.AuPropsCorrect,
                location,
                "The fixed value of the use differs from the fixed value of the declaration."
            );
            return null;
        }

        var effective = valueConstraint.Kind == AttributeValueConstraintKind.None ? inherited : valueConstraint;
        return new(location, use, declaration, effective);
    }
}
