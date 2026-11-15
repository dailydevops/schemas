namespace NetEvolve.Schemas.Tests.PublicApi;

public class PlaceholderTests
{
    [Test]
    public async Task Placeholder_Passes() => await Assert.That(typeof(PlaceholderTests).Assembly).IsNotNull();
}
