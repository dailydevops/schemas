namespace NetEvolve.Schemas.Xml.Tests.Unit;

// Pins the placeholder probe; delete together with XmlProbe when the Xml package ships its first public type.
public class XmlProbeTests
{
    [Test]
    public async Task Name_ReturnsTypeName() => await Assert.That(XmlProbe.Name).IsEqualTo("XmlProbe");
}
