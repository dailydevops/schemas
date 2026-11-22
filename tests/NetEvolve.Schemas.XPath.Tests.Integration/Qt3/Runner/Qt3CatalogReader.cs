namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using System.Xml.Linq;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

/// <summary>Reads the QT3 catalog and its test-set files.</summary>
internal static class Qt3CatalogReader
{
    private static readonly XNamespace Ns = "http://www.w3.org/2010/09/qt-fots-catalog";

    /// <summary>Reads every test set listed in <c>catalog.xml</c>.</summary>
    /// <param name="suiteRoot">The suite directory that holds <c>catalog.xml</c>.</param>
    /// <returns>The test sets in catalog order.</returns>
    public static IReadOnlyList<Qt3TestSet> Read(string suiteRoot)
    {
        var catalog = XDocument.Load(Path.Combine(suiteRoot, "catalog.xml")).Root!;
        var sets = new List<Qt3TestSet>();
        foreach (var entry in catalog.Elements(Ns + "test-set"))
        {
            sets.Add(ReadSet(Path.Combine(suiteRoot, (string)entry.Attribute("file")!)));
        }
        return sets;
    }

    private static Qt3TestSet ReadSet(string file)
    {
        // White space is kept: an assert-string-value of only white space expects exactly that string.
        var root = XDocument.Load(file, LoadOptions.PreserveWhitespace).Root!;
        var directory = Path.GetDirectoryName(file)!;
        var name = (string)root.Attribute("name")!;
        var cases = new List<Qt3Case>();
        foreach (var element in root.Elements(Ns + "test-case"))
        {
            var test = element.Element(Ns + "test");
            var queryFile = (string?)test?.Attribute("file");
            var assertion = element.Element(Ns + "result")?.Elements().FirstOrDefault();
            cases.Add(
                new Qt3Case(
                    name,
                    (string)element.Attribute("name")!,
                    test?.Value ?? string.Empty,
                    queryFile is null ? null : Path.Combine(directory, queryFile),
                    (string?)element.Element(Ns + "environment")?.Attribute("ref"),
                    new Qt3Assertion(
                        assertion?.Name.LocalName ?? "none",
                        (string?)assertion?.Attribute("code"),
                        assertion?.Value ?? string.Empty,
                        string.Equals(
                            (string?)assertion?.Attribute("normalize-space"),
                            "true",
                            StringComparison.Ordinal
                        ),
                        assertion?.Attribute("file") is { } assertionFile
                            ? Path.Combine(directory, assertionFile.Value)
                            : null,
                        string.Equals(
                            (string?)assertion?.Attribute("ignore-prefixes"),
                            "true",
                            StringComparison.Ordinal
                        )
                    ),
                    ReadDependencies(element)
                )
            );
        }
        return new Qt3TestSet(name, ReadDependencies(root), cases);
    }

    private static List<Qt3Dependency> ReadDependencies(XElement parent)
    {
        var list = new List<Qt3Dependency>();
        foreach (var dependency in parent.Elements(Ns + "dependency"))
        {
            list.Add(
                new Qt3Dependency(
                    (string)dependency.Attribute("type")!,
                    (string)dependency.Attribute("value")!,
                    !string.Equals((string?)dependency.Attribute("satisfied"), "false", StringComparison.Ordinal)
                )
            );
        }
        return list;
    }
}
