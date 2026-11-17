namespace NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;

using System.Reflection;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Tests.Unit.SuitePins;

public class PatternArchitectureTests
{
    private const string PatternsNamespace = "NetEvolve.Schemas.Schematron.Patterns";
    private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    private static List<Type> PatternTypes() =>
        [
            .. typeof(PatternMatcher)
                .Assembly.GetTypes()
                .Where(t => string.Equals(t.Namespace, PatternsNamespace, StringComparison.Ordinal)),
        ];

    [Test]
    public async Task Patterns_DeclareTheirSubsetAsAProfileOfTheXPathChecker()
    {
        var usesChecker = PatternTypes()
            .Exists(t =>
                t.GetProperties(AnyStatic)
                    .Any(p => string.Equals(p.PropertyType.Name, "XPathSubsetChecker", StringComparison.Ordinal))
            );

        await Assert.That(usesChecker).IsTrue();
    }

    [Test]
    public async Task Patterns_ContainNoSecondSubsetChecker()
    {
        var checkers = PatternTypes()
            .Where(t =>
                t.GetInterfaces().Any(i => string.Equals(i.Name, "IXPathSubsetProfile", StringComparison.Ordinal))
                || t.Name.EndsWith("Checker", StringComparison.Ordinal)
            )
            .Select(t => t.Name)
            .ToList();

        await Assert.That(checkers).IsEmpty();
    }

    [Test]
    public async Task Patterns_ContainNoLinq(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var folder = Path.Combine(SuitePinFiles.RepositoryRoot, "src", "NetEvolve.Schemas.Schematron", "Patterns");
        var files = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);
        var offenders = new List<string>();
        foreach (var file in files)
        {
            var text = await File.ReadAllTextAsync(file, cancellationToken);
            if (
                text.Contains("System.Linq", StringComparison.Ordinal)
                || text.Contains(".Select(", StringComparison.Ordinal)
                || text.Contains(".Where(", StringComparison.Ordinal)
            )
            {
                offenders.Add(Path.GetFileName(file));
            }
        }

        await Assert.That(files).IsNotEmpty();
        await Assert.That(offenders).IsEmpty();
    }
}
