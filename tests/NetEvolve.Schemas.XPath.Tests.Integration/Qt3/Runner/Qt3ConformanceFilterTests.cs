namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.XPath.Tests.Integration.Conformance;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.ParserOnly;

/// <summary>The conformance workflow selects tests with <c>/*/*Conformance*/*/*</c>, which matches the namespace segment.</summary>
public class Qt3ConformanceFilterTests
{
    [Test]
    public async Task ConformanceTests_SitInANamespaceTheWorkflowFilterSelects()
    {
        var type = typeof(Qt3ConformanceFilterTests)
            .Assembly.GetTypes()
            .Single(t => string.Equals(t.Name, "Qt3ConformanceTests", StringComparison.Ordinal));

        await Assert.That(type.Namespace).Contains("Conformance");
    }

    [Test]
    [Arguments(typeof(Qt3ConformanceTests))]
    [Arguments(typeof(Qt3BaselineTests))]
    [Arguments(typeof(Qt3ParserOnlyTests))]
    public async Task SuiteRuns_AreSelectedByTheConformanceLeg(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        await Assert.That(type.GetCustomAttributes(typeof(ConformanceLegAttribute), inherit: false)).IsNotEmpty();
    }

    [Test]
    public async Task SuiteClasses_AreExplicitAndInTheConformanceCategory()
    {
        var suites = typeof(Qt3ConformanceFilterTests)
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
