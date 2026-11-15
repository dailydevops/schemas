namespace NetEvolve.Schemas.Xml.Tests.Unit;

public class PlaceholderTests
{
    [Test]
    public async Task Placeholder_Passes() => await Assert.That(typeof(PlaceholderTests).Assembly).IsNotNull();
}
