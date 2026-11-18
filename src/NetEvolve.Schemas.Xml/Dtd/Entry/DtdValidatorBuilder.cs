#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, AP-3)

namespace NetEvolve.Schemas;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>Fluent builder for a DTD validator: resolver, limits, external access, and non-deterministic content models. Builders are not thread-safe.</summary>
/// <remarks>
/// Limitation: until work package XML-69 lands, <c>Build</c> and <c>BuildAsync</c> fail with a
/// <see cref="NotSupportedException"/>. External access is off unless a resolver is set and the flag of the kind is on.
/// </remarks>
public sealed class DtdValidatorBuilder : SchemaValidatorBuilder<DtdValidatorBuilder, IDtdValidator>
{
    private bool _allowExternalSubset;
    private bool _allowExternalGeneralEntities;
    private bool _allowExternalParameterEntities;
    private DtdNonDeterminism _nonDeterministicContentModels = DtdNonDeterminism.Warn;
    private DtdValidationLimits _dtdLimits = DtdValidationLimits.Default;

    internal DtdValidatorBuilder() { }

    /// <summary>Gets a value indicating whether the external subset may be requested; <see langword="false"/> unless set.</summary>
    public bool AllowExternalSubset => _allowExternalSubset;

    /// <summary>Gets a value indicating whether external general entities may be requested; <see langword="false"/> unless set.</summary>
    public bool AllowExternalGeneralEntities => _allowExternalGeneralEntities;

    /// <summary>Gets a value indicating whether external parameter entities may be requested; <see langword="false"/> unless set.</summary>
    public bool AllowExternalParameterEntities => _allowExternalParameterEntities;

    /// <summary>Gets the treatment of non-deterministic content models, <see cref="DtdNonDeterminism.Warn"/> unless set.</summary>
    public DtdNonDeterminism NonDeterministicContentModels => _nonDeterministicContentModels;

    /// <summary>Gets the limits of DTD validation, <see cref="DtdValidationLimits.Default"/> unless set.</summary>
    public DtdValidationLimits DtdLimits => _dtdLimits;

    /// <summary>Gets the options a validator built now would use: the resolver, the limits, and the DTD settings of this builder.</summary>
    public DtdValidationOptions Options =>
        DtdValidationOptions
            .Default.WithResolver(Resolver)
            .WithLimits(Limits)
            .WithDtdLimits(_dtdLimits)
            .WithAllowExternalSubset(_allowExternalSubset)
            .WithAllowExternalGeneralEntities(_allowExternalGeneralEntities)
            .WithAllowExternalParameterEntities(_allowExternalParameterEntities)
            .WithNonDeterministicContentModels(_nonDeterministicContentModels);

    /// <summary>Lets the external subset be requested; the request still needs a resolver that allows it.</summary>
    /// <param name="value"><see langword="true"/> to allow the request.</param>
    /// <returns>This builder.</returns>
    public DtdValidatorBuilder WithAllowExternalSubset(bool value)
    {
        _allowExternalSubset = value;
        return this;
    }

    /// <summary>Lets external general entities be requested; the request still needs a resolver that allows it.</summary>
    /// <param name="value"><see langword="true"/> to allow the request.</param>
    /// <returns>This builder.</returns>
    public DtdValidatorBuilder WithAllowExternalGeneralEntities(bool value)
    {
        _allowExternalGeneralEntities = value;
        return this;
    }

    /// <summary>Lets external parameter entities be requested; the request still needs a resolver that allows it.</summary>
    /// <param name="value"><see langword="true"/> to allow the request.</param>
    /// <returns>This builder.</returns>
    public DtdValidatorBuilder WithAllowExternalParameterEntities(bool value)
    {
        _allowExternalParameterEntities = value;
        return this;
    }

    /// <summary>Selects the treatment of non-deterministic content models; the accepted language is the same in every mode.</summary>
    /// <param name="value">The treatment.</param>
    /// <returns>This builder.</returns>
    public DtdValidatorBuilder WithNonDeterministicContentModels(DtdNonDeterminism value)
    {
        Argument.ThrowIfGreaterThan((int)value, (int)DtdNonDeterminism.Reject);
        Argument.ThrowIfLessThan((int)value, (int)DtdNonDeterminism.Accept);

        _nonDeterministicContentModels = value;
        return this;
    }

    /// <summary>Sets the limits of DTD validation.</summary>
    /// <param name="value">The limits.</param>
    /// <returns>This builder.</returns>
    public DtdValidatorBuilder WithDtdLimits(DtdValidationLimits value)
    {
        Argument.ThrowIfNull(value);

        _dtdLimits = value;
        return this;
    }

    /// <inheritdoc />
    protected override IDtdValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver) =>
        throw new NotSupportedException("The DTD validator is not available until work package XML-69 lands.");
}
