#pragma warning disable CA1065, MA0025 // Contract stub: the owning work package replaces the throwing bodies.

namespace NetEvolve.Schemas.Formats;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Resources;

/// <summary>Shared fluent builder base. Builders are not thread-safe; the validators they build are.</summary>
/// <typeparam name="TBuilder">The concrete builder type.</typeparam>
/// <typeparam name="TValidator">The validator type returned by <see cref="Build"/>.</typeparam>
/// <remarks>Limitation: members other than the constructor throw until FND-55 lands.</remarks>
public abstract class SchemaValidatorBuilder<TBuilder, TValidator>
    where TBuilder : SchemaValidatorBuilder<TBuilder, TValidator>
    where TValidator : ISchemaValidator
{
    /// <summary>Initializes a new instance of the <see cref="SchemaValidatorBuilder{TBuilder, TValidator}"/> class.</summary>
    protected SchemaValidatorBuilder() { }

    /// <summary>Gets the limits, <see cref="ValidationLimits.Default"/> unless set.</summary>
    public ValidationLimits Limits => throw Stub.For("FND-55");

    /// <summary>Gets the resolver, never <see langword="null"/>, <see cref="DenyAllResourceResolver.Instance"/> unless set.</summary>
    public IResourceResolver Resolver => throw Stub.For("FND-55");

    /// <summary>Sets the limits.</summary>
    /// <param name="limits">The limits.</param>
    /// <returns>This builder.</returns>
    public TBuilder WithLimits(ValidationLimits limits) => throw Stub.For("FND-55");

    /// <summary>Sets the resolver.</summary>
    /// <param name="resolver">The resolver.</param>
    /// <returns>This builder.</returns>
    public TBuilder WithResolver(IResourceResolver resolver) => throw Stub.For("FND-55");

    /// <summary>Builds an immutable validator.</summary>
    /// <returns>The validator.</returns>
    public TValidator Build() => throw Stub.For("FND-55");

    /// <summary>Builds an immutable validator, reading schema sources asynchronously.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validator.</returns>
    public ValueTask<TValidator> BuildAsync(CancellationToken cancellationToken = default) => throw Stub.For("FND-55");

    /// <summary>Creates the validator from the snapshotted limits and resolver.</summary>
    /// <param name="limits">The limits.</param>
    /// <param name="resolver">The resolver.</param>
    /// <returns>The validator.</returns>
    protected abstract TValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver);

    /// <summary>Creates the validator asynchronously. The default completes synchronously over <see cref="CreateValidator"/>.</summary>
    /// <param name="limits">The limits.</param>
    /// <param name="resolver">The resolver.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validator.</returns>
    protected virtual ValueTask<TValidator> CreateValidatorAsync(
        ValidationLimits limits,
        IResourceResolver resolver,
        CancellationToken cancellationToken
    ) => throw Stub.For("FND-55");
}
