namespace NetEvolve.Schemas.Schematron.Tests.Integration.CorpusScan;

internal static class CorpusFiles
{
    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string SuitesDirectory { get; } = Path.Combine(RepositoryRoot, "tests", "conformance", "suites");

    public static string FixturesDirectory { get; } =
        Path.Combine(
            RepositoryRoot,
            "tests",
            "NetEvolve.Schemas.Schematron.Tests.Integration",
            "CorpusScan",
            "Fixtures"
        );

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
