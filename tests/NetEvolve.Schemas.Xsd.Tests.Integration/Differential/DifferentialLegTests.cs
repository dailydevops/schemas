namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

using NetEvolve.Schemas.Conformance.Reporting;

public class DifferentialLegTests
{
    [Test]
    [Arguments("net10.0", true)]
    [Arguments("net5.0", false)]
    [Arguments("net6.0", false)]
    [Arguments("net11.0", false)]
    [Arguments("netstandard2.0", false)]
    public async Task RunsBaseline_OnlyOnTheNet10Leg(string targetFramework, bool expected) =>
        await Assert.That(DifferentialLeg.RunsBaseline(targetFramework)).IsEqualTo(expected);

    [Test]
    public async Task Current_NamesTheLegAndTheMajorMinorRuntimeOfTheTestAssembly()
    {
        var environment = DifferentialLeg.Current(typeof(DifferentialLegTests).Assembly);

        await Assert
            .That(environment.TargetFramework)
            .IsEqualTo(TargetFrameworkName.Of(typeof(DifferentialLegTests).Assembly));
        await Assert
            .That(environment.RuntimeVersion)
            .IsEqualTo($"{Environment.Version.Major}.{Environment.Version.Minor}");
    }
}
