#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, AP-3)

namespace NetEvolve.Schemas;

using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Entry;

/// <summary>Fluent builder for a DTD validator: resolver, limits, external access, and non-deterministic content models. Builders are not thread-safe.</summary>
/// <remarks>
/// The builder holds no validator state: <c>Build</c> and <c>BuildAsync</c> snapshot the resolver, the core limits, and the
/// DTD settings into one immutable validator. External access is off unless a resolver is set and the flag of the kind is on.
/// </remarks>
public sealed class DtdValidatorBuilder : SchemaValidatorBuilder<DtdValidatorBuilder, IDtdValidator>
{
    // The one place of the DTD settings; the guards live in the options, not here. The resolver and the core limits
    // belong to the base builder and are combined into the options when they are read.
    private DtdValidationOptions _options = DtdValidationOptions.Default;

    internal DtdValidatorBuilder() { }

    /// <summary>Gets a value indicating whether the external subset may be requested; <see langword="false"/> unless set.</summary>
    public bool AllowExternalSubset => _options.AllowExternalSubset;

    /// <summary>Gets a value indicating whether external general entities may be requested; <see langword="false"/> unless set.</summary>
    public bool AllowExternalGeneralEntities => _options.AllowExternalGeneralEntities;

    /// <summary>Gets a value indicating whether external parameter entities may be requested; <see langword="false"/> unless set.</summary>
    public bool AllowExternalParameterEntities => _options.AllowExternalParameterEntities;

    /// <summary>Gets the treatment of non-deterministic content models, <see cref="DtdNonDeterminism.Warn"/> unless set.</summary>
    public DtdNonDeterminism NonDeterministicContentModels => _options.NonDeterministicContentModels;

    /// <summary>Gets the limits of DTD validation, <see cref="DtdValidationLimits.Default"/> unless set.</summary>
    public DtdValidationLimits DtdLimits => _options.DtdLimits;

    /// <summary>Gets the options a validator built now would use: the resolver, the limits, and the DTD settings of this builder.</summary>
    public DtdValidationOptions Options => _options.WithResolver(Resolver).WithLimits(Limits);

    /// <summary>Lets the external subset be requested; the request still needs a resolver that allows it.</summary>
    /// <param name="value"><see langword="true"/> to allow the request.</param>
    /// <returns>This builder.</returns>
    public DtdValidatorBuilder WithAllowExternalSubset(bool value)
    {
        _options = _options.WithAllowExternalSubset(value);
        return this;
    }

    /// <summary>Lets external general entities be requested; the request still needs a resolver that allows it.</summary>
    /// <param name="value"><see langword="true"/> to allow the request.</param>
    /// <returns>This builder.</returns>
    public DtdValidatorBuilder WithAllowExternalGeneralEntities(bool value)
    {
        _options = _options.WithAllowExternalGeneralEntities(value);
        return this;
    }

    /// <summary>Lets external parameter entities be requested; the request still needs a resolver that allows it.</summary>
    /// <param name="value"><see langword="true"/> to allow the request.</param>
    /// <returns>This builder.</returns>
    public DtdValidatorBuilder WithAllowExternalParameterEntities(bool value)
    {
        _options = _options.WithAllowExternalParameterEntities(value);
        return this;
    }

    /// <summary>Selects the treatment of non-deterministic content models; the accepted language is the same in every mode.</summary>
    /// <param name="value">The treatment.</param>
    /// <returns>This builder.</returns>
    public DtdValidatorBuilder WithNonDeterministicContentModels(DtdNonDeterminism value)
    {
        _options = _options.WithNonDeterministicContentModels(value);
        return this;
    }

    /// <summary>Sets the limits of DTD validation.</summary>
    /// <param name="value">The limits.</param>
    /// <returns>This builder.</returns>
    public DtdValidatorBuilder WithDtdLimits(DtdValidationLimits value)
    {
        _options = _options.WithDtdLimits(value);
        return this;
    }

    /// <inheritdoc />
    protected override IDtdValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver) =>
        new DtdValidator(_options.WithResolver(resolver).WithLimits(limits));
}
