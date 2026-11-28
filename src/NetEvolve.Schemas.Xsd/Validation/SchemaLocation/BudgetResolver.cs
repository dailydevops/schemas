namespace NetEvolve.Schemas.Xsd.Validation.SchemaLocation;

using System.Threading;
using NetEvolve.Schemas.Resources;

/// <summary>
/// Hands the requests of a hinted schema document on to the resolver and counts them, so the documents that such a schema includes or
/// imports count against the same <see cref="ValidationLimits.MaxExternalResources"/> as the hints of the run (design #146 DP-22).
/// A request beyond the allowance is refused with <see cref="LimitMessage"/> and never reaches the resolver.
/// </summary>
internal sealed class BudgetResolver : IResourceResolver
{
    /// <summary>The message of a request that the allowance does not cover; the caller turns it into the limit code.</summary>
    internal const string LimitMessage = "The number of external resources that a run may use is exhausted.";

    private readonly IResourceResolver _inner;
    private readonly int _allowance;

    internal BudgetResolver(IResourceResolver inner, int allowance)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentOutOfRangeException.ThrowIfLessThan(allowance, 0);

        _inner = inner;
        _allowance = allowance;
    }

    /// <summary>Gets the number of requests that were handed on to the resolver.</summary>
    internal int Forwarded { get; private set; }

    /// <inheritdoc />
    public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Forwarded >= _allowance)
        {
            return ResourceResult.Denied(LimitMessage);
        }

        Forwarded++;
        return _inner.Resolve(in request, cancellationToken);
    }
}
