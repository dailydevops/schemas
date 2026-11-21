namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>Locates files of the repository; the suite fetch of the XSTS runner is not started.</summary>
internal static class RepositoryFiles
{
    public static string Root { get; } = FindRoot();

    public static string DeviationListPath { get; } =
        Path.Combine(Root, "tests", "NetEvolve.Schemas.Xsd.Tests.Integration", "Differential", "deviations.txt");

    private static string FindRoot()
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
