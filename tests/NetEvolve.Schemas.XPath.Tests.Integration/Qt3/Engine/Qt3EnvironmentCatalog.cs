namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;

using System.Collections.Concurrent;
using System.Xml.Linq;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>
/// Finds the environment of a case. The catalog reader of the runner keeps only the name an environment reference gives, so this
/// reads the catalog and the test-set files once more, lazily and once per test set, for the three places an environment lives:
/// the catalog, the test set, and the test case itself.
/// </summary>
internal sealed class Qt3EnvironmentCatalog(string suiteRoot)
{
    private static readonly XNamespace Ns = "http://www.w3.org/2010/09/qt-fots-catalog";

    private readonly Lazy<Catalog> _catalog = new(
        () => ReadCatalog(suiteRoot),
        LazyThreadSafetyMode.ExecutionAndPublication
    );
    private readonly ConcurrentDictionary<string, Lazy<TestSetEnvironments>> _sets = new(StringComparer.Ordinal);

    /// <summary>Resolves the environment of a case: defined in the case, else named in the test set, else named in the catalog.</summary>
    /// <param name="testCase">The case.</param>
    /// <returns>The environment, <see cref="Qt3Environment.Empty"/> when the case names none.</returns>
    /// <exception cref="KeyNotFoundException">The case names an environment that no file defines.</exception>
    public Qt3Environment Resolve(Qt3Case testCase)
    {
        var catalog = _catalog.Value;
        var set = _sets
            .GetOrAdd(testCase.TestSet, name => new Lazy<TestSetEnvironments>(
                () => ReadTestSet(catalog.TestSetFiles[name]),
                LazyThreadSafetyMode.ExecutionAndPublication
            ))
            .Value;
        if (set.Inline.TryGetValue(testCase.Name, out var inline))
        {
            return inline;
        }

        var reference = testCase.Environment;
        if (reference is null)
        {
            return Qt3Environment.Empty;
        }

        return set.Named.TryGetValue(reference, out var environment) ? environment : catalog.Named[reference];
    }

    private static Catalog ReadCatalog(string root)
    {
        var element = XDocument.Load(Path.Combine(root, "catalog.xml")).Root!;
        var named = new Dictionary<string, Qt3Environment>(StringComparer.Ordinal);
        foreach (var environment in element.Elements(Ns + "environment"))
        {
            named[(string)environment.Attribute("name")!] = Qt3EnvironmentReader.Read(environment, root);
        }

        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in element.Elements(Ns + "test-set"))
        {
            files[(string)entry.Attribute("name")!] = Path.Combine(root, (string)entry.Attribute("file")!);
        }
        return new Catalog(named, files);
    }

    private static TestSetEnvironments ReadTestSet(string file)
    {
        var directory = Path.GetDirectoryName(file)!;
        var element = XDocument.Load(file).Root!;
        var named = new Dictionary<string, Qt3Environment>(StringComparer.Ordinal);
        foreach (var environment in element.Elements(Ns + "environment"))
        {
            named[(string)environment.Attribute("name")!] = Qt3EnvironmentReader.Read(environment, directory);
        }

        var inline = new Dictionary<string, Qt3Environment>(StringComparer.Ordinal);
        foreach (var testCase in element.Elements(Ns + "test-case"))
        {
            var environment = testCase.Element(Ns + "environment");
            if (environment is not null && environment.Attribute("ref") is null)
            {
                inline[(string)testCase.Attribute("name")!] = Qt3EnvironmentReader.Read(environment, directory);
            }
        }
        return new TestSetEnvironments(named, inline);
    }

    private sealed record Catalog(
        IReadOnlyDictionary<string, Qt3Environment> Named,
        IReadOnlyDictionary<string, string> TestSetFiles
    );

    private sealed record TestSetEnvironments(
        IReadOnlyDictionary<string, Qt3Environment> Named,
        IReadOnlyDictionary<string, Qt3Environment> Inline
    );
}
