namespace NetEvolve.Schemas.Conformance.Baseline;

using System.Text;

/// <summary>Reads and writes baseline files; every test set has its own file so parallel work edits different files.</summary>
public static class BaselineFiles
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Gets the path of the baseline file of a test set.</summary>
    /// <param name="directory">The baseline directory.</param>
    /// <param name="testSet">The test set name; it is the file name without extension.</param>
    /// <returns>The file path.</returns>
    public static string PathFor(string directory, string testSet)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(testSet);
        return Path.Combine(directory, testSet + ".txt");
    }

    /// <summary>Loads the baseline of a test set; a missing file is an empty baseline.</summary>
    /// <param name="directory">The baseline directory.</param>
    /// <param name="testSet">The test set name.</param>
    /// <returns>The baseline.</returns>
    /// <exception cref="BaselineFormatException">The file is malformed.</exception>
    public static KnownFailureBaseline Load(string directory, string testSet)
    {
        var path = PathFor(directory, testSet);
        return File.Exists(path)
            ? KnownFailureBaseline.Parse(File.ReadAllText(path, Utf8NoBom))
            : KnownFailureBaseline.Empty;
    }

    /// <summary>Regenerates the baseline file of a test set in stable sort order.</summary>
    /// <param name="directory">The baseline directory; it is created when missing.</param>
    /// <param name="testSet">The test set name.</param>
    /// <param name="caseIds">The case ids of the known failures in any order.</param>
    public static void Save(string directory, string testSet, IEnumerable<string> caseIds)
    {
        ArgumentNullException.ThrowIfNull(caseIds);
        var path = PathFor(directory, testSet);
        _ = Directory.CreateDirectory(directory);
        File.WriteAllText(path, KnownFailureBaseline.Format(caseIds), Utf8NoBom);
    }
}
