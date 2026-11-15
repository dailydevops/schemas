namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Fakes;

public class FakeBuilderContractTests
{
    [Test]
    public async Task CreateValidator_ReturnsFakeValidator()
    {
        var validator = new FakeBuilder().CreateFakeValidator();

        await Assert.That(validator).IsNotNull();
        await Assert.That(validator).IsAssignableTo<ISchemaValidator>();
    }

    [Test]
    public async Task CreateValidatorAsync_ReturnsFakeValidator()
    {
        var validator = await new FakeBuilder().CreateFakeValidatorAsync(CancellationToken.None);

        await Assert.That(validator).IsNotNull();
    }
}
