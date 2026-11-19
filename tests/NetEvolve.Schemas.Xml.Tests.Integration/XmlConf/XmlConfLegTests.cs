namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using NetEvolve.Schemas.Conformance.Reporting;

public class XmlConfLegTests
{
    [Test]
    public async Task SuiteRuns_AreSelectedByTheConformanceLeg() =>
        await Assert
            .That(typeof(XmlConfSuiteTests).GetCustomAttributes(typeof(ConformanceLegAttribute), inherit: false))
            .IsNotEmpty();
}
