namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;

public class BuilderShapeTests
{
    [Test]
    public async Task SchemaValidatorBuilder_HasApprovedMembers()
    {
        var type = typeof(SchemaValidatorBuilder<,>);

        await Assert.That(type.IsAbstract).IsTrue();
        await Assert
            .That(Shape.Of(type))
            .IsEquivalentTo(
                Shape.Sorted(
                    "protected .ctor()",
                    "ValidationLimits Limits { get }",
                    "IResourceResolver Resolver { get }",
                    "TBuilder WithLimits(ValidationLimits)",
                    "TBuilder WithResolver(IResourceResolver)",
                    "TValidator Build()",
                    "ValueTask<TValidator> BuildAsync(CancellationToken)",
                    "protected abstract TValidator CreateValidator(ValidationLimits,IResourceResolver)",
                    "protected virtual ValueTask<TValidator> CreateValidatorAsync(ValidationLimits,IResourceResolver,CancellationToken)"
                )
            );
    }
}
