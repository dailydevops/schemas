namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.XPath.Tests.Integration.Conformance;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.ParserOnly;

/// <summary>The conformance workflow selects the explicit suite classes by their category, see <see cref="ConformanceCategory"/>.</summary>
public class Qt3ConformanceFilterTests
{
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
            // Methods marked explicit on their own (baseline regeneration) stay out of the category on purpose.
            var methods = suite
                .GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(TestAttribute), inherit: false).Length != 0)
                .ToArray();
            var classExplicit = suite.GetCustomAttributes(typeof(ExplicitAttribute), inherit: false).Length != 0;

            await Assert
                .That(
                    methods.All(m =>
                        classExplicit || m.GetCustomAttributes(typeof(ExplicitAttribute), inherit: false).Length != 0
                    )
                )
                .IsTrue();
            await Assert
                .That(
                    suite
                        .GetCustomAttributes(typeof(CategoryAttribute), inherit: false)
                        .Concat(
                            methods.SelectMany(m => m.GetCustomAttributes(typeof(CategoryAttribute), inherit: false))
                        )
                        .Cast<CategoryAttribute>()
                        .Select(a => a.Category)
                )
                .Contains(ConformanceCategory.Name);
        }
    }
}
