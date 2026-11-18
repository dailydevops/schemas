namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// Immutable options of DTD validation: the resolver, the limits, the flags that enable external access, and the
/// treatment of non-deterministic content models.
/// </summary>
/// <remarks>
/// External access needs both a resolver supplied by the caller and the flag of the kind of resource (design #122, D6 and D7).
/// A resolver alone enables nothing; a flag with the default resolver reaches the deny-all resolver and is denied there.
/// Limitation: a validating processor should read external declarations (XML 1.0, section 5.1); external access is off by
/// default, so a document that depends on an external subset reports validity errors unless access is enabled.
/// </remarks>
public sealed class DtdValidationOptions
{
    private IResourceResolver? _resolver;
    private ValidationLimits? _limits;
    private DtdValidationLimits? _dtdLimits;
    private bool _allowExternalSubset;
    private bool _allowExternalGeneralEntities;
    private bool _allowExternalParameterEntities;
    private DtdNonDeterminism _nonDeterministicContentModels = DtdNonDeterminism.Warn;

    private DtdValidationOptions() { }

    /// <summary>Gets the default options: the deny-all resolver, no external access, and a warning for non-deterministic content models.</summary>
    public static DtdValidationOptions Default { get; } = new();

    /// <summary>Gets the resolver, never <see langword="null"/>, <see cref="DenyAllResourceResolver.Instance"/> unless set.</summary>
    public IResourceResolver Resolver => _resolver ?? DenyAllResourceResolver.Instance;

    /// <summary>Gets the core limits, <see cref="ValidationLimits.Default"/> unless set.</summary>
    public ValidationLimits Limits => _limits ?? ValidationLimits.Default;

    /// <summary>Gets the limits of DTD validation, <see cref="DtdValidationLimits.Default"/> unless set.</summary>
    public DtdValidationLimits DtdLimits => _dtdLimits ?? DtdValidationLimits.Default;

    /// <summary>Gets a value indicating whether the external subset may be requested; <see langword="false"/> by default.</summary>
    public bool AllowExternalSubset => _allowExternalSubset;

    /// <summary>Gets a value indicating whether external general entities may be requested; <see langword="false"/> by default.</summary>
    public bool AllowExternalGeneralEntities => _allowExternalGeneralEntities;

    /// <summary>Gets a value indicating whether external parameter entities may be requested; <see langword="false"/> by default.</summary>
    public bool AllowExternalParameterEntities => _allowExternalParameterEntities;

    /// <summary>Gets the treatment of non-deterministic content models, <see cref="DtdNonDeterminism.Warn"/> by default.</summary>
    public DtdNonDeterminism NonDeterministicContentModels => _nonDeterministicContentModels;

    /// <summary>Returns a copy with another <see cref="Resolver"/>.</summary>
    /// <param name="value">The new resolver.</param>
    /// <returns>The new options.</returns>
    public DtdValidationOptions WithResolver(IResourceResolver value)
    {
        Argument.ThrowIfNull(value);

        var copy = (DtdValidationOptions)MemberwiseClone();
        copy._resolver = value;

        return copy;
    }

    /// <summary>Returns a copy with other <see cref="Limits"/>.</summary>
    /// <param name="value">The new limits.</param>
    /// <returns>The new options.</returns>
    public DtdValidationOptions WithLimits(ValidationLimits value)
    {
        Argument.ThrowIfNull(value);

        var copy = (DtdValidationOptions)MemberwiseClone();
        copy._limits = value;

        return copy;
    }

    /// <summary>Returns a copy with other <see cref="DtdLimits"/>.</summary>
    /// <param name="value">The new limits.</param>
    /// <returns>The new options.</returns>
    public DtdValidationOptions WithDtdLimits(DtdValidationLimits value)
    {
        Argument.ThrowIfNull(value);

        var copy = (DtdValidationOptions)MemberwiseClone();
        copy._dtdLimits = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="AllowExternalSubset"/>.</summary>
    /// <param name="value"><see langword="true"/> to let the external subset be requested.</param>
    /// <returns>The new options.</returns>
    public DtdValidationOptions WithAllowExternalSubset(bool value)
    {
        var copy = (DtdValidationOptions)MemberwiseClone();
        copy._allowExternalSubset = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="AllowExternalGeneralEntities"/>.</summary>
    /// <param name="value"><see langword="true"/> to let external general entities be requested.</param>
    /// <returns>The new options.</returns>
    public DtdValidationOptions WithAllowExternalGeneralEntities(bool value)
    {
        var copy = (DtdValidationOptions)MemberwiseClone();
        copy._allowExternalGeneralEntities = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="AllowExternalParameterEntities"/>.</summary>
    /// <param name="value"><see langword="true"/> to let external parameter entities be requested.</param>
    /// <returns>The new options.</returns>
    public DtdValidationOptions WithAllowExternalParameterEntities(bool value)
    {
        var copy = (DtdValidationOptions)MemberwiseClone();
        copy._allowExternalParameterEntities = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="NonDeterministicContentModels"/>.</summary>
    /// <param name="value">The new treatment.</param>
    /// <returns>The new options.</returns>
    public DtdValidationOptions WithNonDeterministicContentModels(DtdNonDeterminism value)
    {
        Argument.ThrowIfGreaterThan((int)value, (int)DtdNonDeterminism.Reject);
        Argument.ThrowIfLessThan((int)value, (int)DtdNonDeterminism.Accept);

        var copy = (DtdValidationOptions)MemberwiseClone();
        copy._nonDeterministicContentModels = value;

        return copy;
    }

    /// <summary>
    /// Requests an external resource through the single path of the subset and the external entities: a kind whose flag is
    /// off is denied without calling the resolver, and every outcome other than found is a denial that carries
    /// <see cref="XmlErrorCodes.XML2002"/>, the kind, the requested URI, and the base URI.
    /// </summary>
    /// <param name="request">The request; its kind must be an external subset, an external entity, or an external parameter entity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The found resource, which the caller disposes, or a denial.</returns>
    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Argument.ThrowIfGreaterThan((int)request.Kind, (int)ResourceKind.ExternalParameterEntity);

        if (!IsAllowed(request.Kind))
        {
            return Deny(in request);
        }

        var result = Resolver.Resolve(in request, cancellationToken);
        if (result.Status == ResourceStatus.Found)
        {
            return result;
        }

        result.Dispose();

        return Deny(in request);
    }

    private bool IsAllowed(ResourceKind kind) =>
        kind switch
        {
            ResourceKind.ExternalSubset => _allowExternalSubset,
            ResourceKind.ExternalEntity => _allowExternalGeneralEntities,
            _ => _allowExternalParameterEntities,
        };

    private static ResourceResult Deny(in ResourceRequest request) =>
        ResourceResult.Denied(
            $"{XmlErrorCodes.XML2002.Alias}: {request.Kind} resource '{request.ResolvedUri ?? request.Reference}' was denied (base URI '{request.BaseUri ?? "(none)"}')."
        );
}
