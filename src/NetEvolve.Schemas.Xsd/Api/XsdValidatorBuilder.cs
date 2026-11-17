#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, #146 DP-1)

namespace NetEvolve.Schemas;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>Fluent builder for an XSD validator: version, limits, and resolver. Builders are not thread-safe.</summary>
/// <remarks>
/// Limitation: until work package XSD-57 lands, <c>Build</c> and <c>BuildAsync</c> fail with a
/// <see cref="NotSupportedException"/>. Schema sources, DTD processing, and schema location hints are added by
/// the work packages that implement them.
/// </remarks>
public sealed class XsdValidatorBuilder : SchemaValidatorBuilder<XsdValidatorBuilder, IXsdValidator>
{
    private XsdVersion _version = XsdVersion.V11;

    internal XsdValidatorBuilder() { }

    /// <summary>Gets the XSD version, <see cref="XsdVersion.V11"/> unless set.</summary>
    public XsdVersion Version => _version;

    /// <summary>Selects the XSD version. <see cref="XsdVersion.V10"/> is strict: every 1.1 construct is a schema error.</summary>
    /// <param name="version">The version.</param>
    /// <returns>This builder.</returns>
    public XsdValidatorBuilder WithVersion(XsdVersion version)
    {
        Argument.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        Argument.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        _version = version;
        return this;
    }

    /// <inheritdoc />
    protected override IXsdValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver) =>
        throw new NotSupportedException("The XSD validator is not available until work package XSD-57 lands.");
}
