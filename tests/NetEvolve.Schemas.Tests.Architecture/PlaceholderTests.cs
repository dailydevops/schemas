namespace NetEvolve.Schemas.Tests.Architecture;

public class PlaceholderTests
{
    [Test]
    public async Task Placeholder_Passes() => await Assert.That(typeof(PlaceholderTests).Assembly).IsNotNull();
}
