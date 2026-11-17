namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

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
}
