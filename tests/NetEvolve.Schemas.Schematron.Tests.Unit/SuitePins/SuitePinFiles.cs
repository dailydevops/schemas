namespace NetEvolve.Schemas.Schematron.Tests.Unit.SuitePins;

using NetEvolve.Schemas.Conformance.Manifest;

internal static class SuitePinFiles
{
    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static string SuitesDirectory { get; } = Path.Combine(RepositoryRoot, "tests", "conformance", "suites");

    public static SuiteManifest Load(string fileName)
    {
        var path = Path.Combine(SuitesDirectory, fileName);
        return SuiteManifestLoader.Parse(File.ReadAllText(path), path);
    }

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
