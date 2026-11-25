namespace NetEvolve.Schemas.Tests.Architecture.PackageGraph;

using NetEvolve.Schemas.Tests.Architecture.Shared;

/// <summary>The dependency injection package holds the generic core only; each format package carries its own registrations (#1054).</summary>
public class DependencyInjectionLayeringTests
{
    private static readonly string[] FormatPackages =
    [
        SourceAssemblies.Xml,
        SourceAssemblies.XPath,
        SourceAssemblies.Schematron,
        SourceAssemblies.Xsd,
    ];

    [Test]
    public async Task DependencyInjection_ReferencesNoFormatPackage()
    {
        var referenced = SourceAssemblies
            .Load(SourceAssemblies.DependencyInjection)
            .GetReferencedAssemblies()
            .Select(static reference => reference.Name);

        await Assert.That(referenced.Intersect(FormatPackages, StringComparer.Ordinal)).IsEmpty();
    }

    [Test]
    public async Task XsdAndDependencyInjection_BringNoSchematronAssembly()
    {
        var closure = Closure(SourceAssemblies.Xsd, SourceAssemblies.DependencyInjection);

        await Assert.That(closure).Contains(SourceAssemblies.DependencyInjection);
        await Assert.That(closure).DoesNotContain(SourceAssemblies.Schematron);
    }

    [Test]
    public async Task SchematronAndDependencyInjection_BringNoXsdAssembly()
    {
        var closure = Closure(SourceAssemblies.Schematron, SourceAssemblies.DependencyInjection);

        await Assert.That(closure).Contains(SourceAssemblies.DependencyInjection);
        await Assert.That(closure).DoesNotContain(SourceAssemblies.Xsd);
    }

    [Test]
    public async Task XmlAndDependencyInjection_BringNoXPathAndNoFormatAssembly()
    {
        var closure = Closure(SourceAssemblies.Xml, SourceAssemblies.DependencyInjection);

        await Assert.That(closure).Contains(SourceAssemblies.DependencyInjection);
        await Assert.That(closure).DoesNotContain(SourceAssemblies.XPath);
        await Assert.That(closure).DoesNotContain(SourceAssemblies.Schematron);
        await Assert.That(closure).DoesNotContain(SourceAssemblies.Xsd);
    }

    // Walks the assembly references of the NetEvolve.Schemas packages, which is what a consumer of the given packages restores.
    private static HashSet<string> Closure(params string[] roots)
    {
        var seen = new HashSet<string>(roots, StringComparer.Ordinal);
        var pending = new Queue<string>(roots);

        while (pending.TryDequeue(out var name))
        {
            foreach (var reference in SourceAssemblies.Load(name).GetReferencedAssemblies())
            {
                if (
                    reference.Name is { } referenced
                    && referenced.StartsWith("NetEvolve.Schemas", StringComparison.Ordinal)
                    && seen.Add(referenced)
                )
                {
                    pending.Enqueue(referenced);
                }
            }
        }

        return seen;
    }
}
