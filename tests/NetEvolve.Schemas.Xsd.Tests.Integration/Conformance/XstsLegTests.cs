namespace NetEvolve.Schemas.Xsd.Tests.Integration.Conformance;

using NetEvolve.Schemas.Conformance.Reporting;

public class XstsLegTests
{
    [Test]
    public async Task SuiteRuns_AreSelectedByTheConformanceLeg() =>
        await Assert
            .That(typeof(XstsConformanceTests).GetCustomAttributes(typeof(ConformanceLegAttribute), inherit: false))
            .IsNotEmpty();

    [Test]
    public async Task SuiteClasses_AreExplicitAndInTheConformanceCategory()
    {
        var suites = typeof(XstsConformanceTests)
            .Assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(ConformanceLegAttribute), inherit: false).Length != 0)
            .ToArray();

        await Assert.That(suites).IsNotEmpty();
        foreach (var suite in suites)
        {
            await Assert.That(suite.GetCustomAttributes(typeof(ExplicitAttribute), inherit: false)).IsNotEmpty();
            await Assert
                .That(
                    suite
                        .GetCustomAttributes(typeof(CategoryAttribute), inherit: false)
                        .Cast<CategoryAttribute>()
                        .Select(a => a.Category)
                )
                .Contains(ConformanceCategory.Name);
        }
    }
}
