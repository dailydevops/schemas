namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

using NetEvolve.Schemas.Conformance.Reporting;

public class SchematronLegTests
{
    [Test]
    public async Task SuiteRuns_AreSelectedByTheConformanceLeg() =>
        await Assert
            .That(
                typeof(SchematronConformanceSuiteTests).GetCustomAttributes(
                    typeof(ConformanceLegAttribute),
                    inherit: false
                )
            )
            .IsNotEmpty();
}
