namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using NetEvolve.Schemas.Conformance.Reporting;

public class XmlConfLegTests
{
    [Test]
    public async Task SuiteRuns_AreSelectedByTheConformanceLeg() =>
        await Assert
            .That(typeof(XmlConfSuiteTests).GetCustomAttributes(typeof(ConformanceLegAttribute), inherit: false))
            .IsNotEmpty();

    [Test]
    public async Task SuiteClasses_AreExplicitAndInTheConformanceCategory()
    {
        var suites = typeof(XmlConfSuiteTests)
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
