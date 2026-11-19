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
}
