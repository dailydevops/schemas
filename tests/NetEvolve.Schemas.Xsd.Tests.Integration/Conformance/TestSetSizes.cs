namespace NetEvolve.Schemas.Xsd.Tests.Integration.Conformance;

/// <summary>The number of selected cases of every test set, read from the committed manifest <c>xsts-test-sets.txt</c> beside this file.</summary>
internal static class TestSetSizes
{
    /// <summary>Gets the path of the manifest in the repository.</summary>
    public static string ManifestPath { get; } =
        Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "NetEvolve.Schemas.Xsd.Tests.Integration",
            "Conformance",
            "xsts-test-sets.txt"
        );

    /// <summary>Reads the manifest: one line per test set, <c>name</c> and <c>count</c> separated by a tab, sorted by name.</summary>
    /// <returns>The sizes in file order.</returns>
    public static List<(string TestSet, int Count)> Load()
    {
        var sizes = new List<(string, int)>();
        foreach (var line in File.ReadAllLines(ManifestPath))
        {
            var tab = line.IndexOf('\t', StringComparison.Ordinal);
            sizes.Add((line[..tab], int.Parse(line[(tab + 1)..], System.Globalization.CultureInfo.InvariantCulture)));
        }

        return sizes;
    }

    // The repository root is found here, not through XstsSuite, whose first use starts the download of the suite.
    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Schemas.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException("Repository root (Schemas.slnx) not found.");
    }
}
