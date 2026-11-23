namespace NetEvolve.Schemas.Conformance.Tests.Unit.Reporting;

using NetEvolve.Schemas.Conformance.Reporting;

public class TargetFrameworkNameTests
{
    [Test]
    [Arguments(".NETCoreApp,Version=v8.0", "net8.0")]
    [Arguments(".NETCoreApp,Version=v11.0", "net11.0")]
    [Arguments(".NETStandard,Version=v2.1", "netstandard2.1")]
    [Arguments("Unknown,Version=v1.0", "Unknown,Version=v1.0")]
    public async Task FromFrameworkName_KnownMonikers_ReturnsShortName(string frameworkName, string expected) =>
        await Assert.That(TargetFrameworkName.FromFrameworkName(frameworkName)).IsEqualTo(expected);

    [Test]
    public async Task Of_TestAssembly_ReturnsCurrentLegMoniker()
    {
        var name = TargetFrameworkName.Of(typeof(TargetFrameworkNameTests).Assembly);

        await Assert.That(name).StartsWith("net");
    }
}
