namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>Finds source files that use System.Xml.Schema.</summary>
internal static class SourceScan
{
    private const string Token = "System.Xml.Schema";

    private static readonly string[] Extensions = [".cs", ".csproj", ".props", ".targets"];

    /// <summary>Lists the project and C# files below a directory that name System.Xml.Schema in code; comments and build output are ignored.</summary>
    /// <param name="directory">The directory to scan.</param>
    /// <returns>The paths of the files, relative to the directory, with <c>/</c> separators, in ordinal order.</returns>
    public static IReadOnlyList<string> FindSystemXmlSchemaUsage(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var found = new List<string>();
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(directory, file).Replace('\\', '/');
            if (Array.IndexOf(Extensions, Path.GetExtension(file)) >= 0 && !IsBuildOutput(relative) && Uses(file))
            {
                found.Add(relative);
            }
        }

        found.Sort(StringComparer.Ordinal);
        return found;
    }

    private static bool IsBuildOutput(string relativePath)
    {
        var segments = relativePath.Split('/');
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (
                string.Equals(segments[i], "bin", StringComparison.Ordinal)
                || string.Equals(segments[i], "obj", StringComparison.Ordinal)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static bool Uses(string file)
    {
        if (!file.EndsWith(".cs", StringComparison.Ordinal))
        {
            return File.ReadAllText(file).Contains(Token, StringComparison.Ordinal);
        }

        foreach (var line in File.ReadLines(file))
        {
            var comment = line.IndexOf("//", StringComparison.Ordinal);
            if ((comment < 0 ? line : line[..comment]).Contains(Token, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
