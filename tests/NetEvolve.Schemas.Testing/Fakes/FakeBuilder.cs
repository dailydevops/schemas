namespace NetEvolve.Schemas.Testing.Fakes;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;

/// <summary>Builder derived from <see cref="SchemaValidatorBuilder{TBuilder, TValidator}"/> that creates a <see cref="FakeValidator"/>.</summary>
/// <remarks>
/// The protected creation hooks are exposed directly, so tests can call them without building. The fake ignores
/// the limits and resolver it is given.
/// </remarks>
public sealed class FakeBuilder : SchemaValidatorBuilder<FakeBuilder, FakeValidator>
{
    /// <summary>Calls the protected <c>CreateValidator</c> hook.</summary>
    /// <returns>A new fake validator.</returns>
    public FakeValidator CreateFakeValidator() => CreateValidator(null!, null!);

    /// <summary>Calls the protected <c>CreateValidatorAsync</c> hook.</summary>
    /// <param name="cancellationToken">The token passed to the hook.</param>
    /// <returns>A completed task over a new fake validator.</returns>
    public ValueTask<FakeValidator> CreateFakeValidatorAsync(CancellationToken cancellationToken = default) =>
        CreateValidatorAsync(null!, null!, cancellationToken);

    /// <inheritdoc />
    protected override FakeValidator CreateValidator(ValidationLimits limits, IResourceResolver resolver) => new();

    /// <inheritdoc />
    protected override ValueTask<FakeValidator> CreateValidatorAsync(
        ValidationLimits limits,
        IResourceResolver resolver,
        CancellationToken cancellationToken
    ) => new(CreateValidator(limits, resolver));
}
