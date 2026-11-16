namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Fakes;

public class FakeBuilderContractTests
{
    private static FakeValidator Create(FakeBuilder builder) => builder.CreateFakeValidator();

    [Test]
    public async Task CreateValidator_ReturnsFakeValidator(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = Create(new FakeBuilder());

        await Assert.That(validator).IsNotNull();
        await Assert.That(validator).IsAssignableTo<ISchemaValidator>();
    }

    [Test]
    public async Task CreateValidatorAsync_ReturnsFakeValidator(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = await new FakeBuilder().CreateFakeValidatorAsync(cancellationToken);

        await Assert.That(validator).IsNotNull();
    }
}
