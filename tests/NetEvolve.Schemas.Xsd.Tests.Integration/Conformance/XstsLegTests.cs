namespace NetEvolve.Schemas.Xsd.Tests.Integration.Conformance;

using NetEvolve.Schemas.Conformance.Reporting;

public class XstsLegTests
{
    [Test]
    public async Task SuiteRuns_AreSelectedByTheConformanceLeg() =>
        await Assert
            .That(typeof(XstsConformanceTests).GetCustomAttributes(typeof(ConformanceLegAttribute), inherit: false))
            .IsNotEmpty();
}
