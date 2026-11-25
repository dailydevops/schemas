namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.Conformance.Reporting;

/// <summary>The conformance workflow selects the explicit suite classes by their category, see <see cref="ConformanceCategory"/>.</summary>
public class Qt3ConformanceFilterTests
{
    [Test]
    public async Task SuiteClasses_AreExplicitAndInTheConformanceCategory()
    {
        var suites = typeof(Qt3ConformanceFilterTests)
            .Assembly.GetTypes()
            .Where(t =>
                t.GetCustomAttributes(typeof(CategoryAttribute), inherit: false)
                    .Concat(
                        t.GetMethods().SelectMany(m => m.GetCustomAttributes(typeof(CategoryAttribute), inherit: false))
                    )
                    .Cast<CategoryAttribute>()
                    .Any(a => a.Category == ConformanceCategory.Name)
            )
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
