namespace NetEvolve.Schemas.Conformance.Reporting;

using System.Buffers;
using System.Text.Json;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>
/// Writes the complete result of a conformance run, one line per case, to the folder named by the
/// <c>SCHEMAS_CONFORMANCE_REPORT</c> environment variable; the conformance workflow uploads the folder as an artifact.
/// </summary>
/// <remarks>
/// The folder gets one file per package and target framework, in the JSON Lines format: every line is one JSON
/// object with the properties <c>suite</c>, <c>targetFramework</c>, <c>testSet</c>, <c>caseId</c>, <c>status</c>,
/// and, when the case has a failure message or a skip reason, <c>reason</c>. The status is <c>passed</c>,
/// <c>pending</c> (the case fails and the known-failure baseline lists it), <c>regression</c> (the case fails and
/// the baseline does not list it), <c>unexpected-pass</c> (the case passes although the baseline lists it), or
/// <c>skipped</c>. The lines are sorted by test set and case id, so two runs compare as text. The job summary shows
/// the counts of the same run; see <see cref="ConformanceRateReporter"/>.
/// </remarks>
public static class ConformanceReportFile
{
    /// <summary>The name of the environment variable that holds the folder of the report files.</summary>
    public const string DirectoryVariable = "SCHEMAS_CONFORMANCE_REPORT";

    /// <summary>Gets the file name of one package and target framework.</summary>
    /// <param name="package">The package that owns the suite.</param>
    /// <param name="targetFramework">The target framework of the test run.</param>
    /// <returns>The file name; every character outside letters, digits, <c>.</c>, and <c>-</c> is replaced by <c>-</c>.</returns>
    public static string FileName(string package, string targetFramework)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(targetFramework);

        var name = (package + "." + targetFramework).ToCharArray();
        for (var i = 0; i < name.Length; i++)
        {
            if (!char.IsAsciiLetterOrDigit(name[i]) && name[i] is not ('.' or '-'))
            {
                name[i] = '-';
            }
        }

        return new string(name) + ".jsonl";
    }

    /// <summary>Renders the lines of the cases of the given test sets.</summary>
    /// <param name="package">The package that owns the suite.</param>
    /// <param name="targetFramework">The target framework of the test run.</param>
    /// <param name="testSets">The reports of the test sets.</param>
    /// <returns>The lines as UTF-8 text; every line ends with <c>\n</c>. Empty when the test sets have no cases.</returns>
    public static ReadOnlyMemory<byte> Render(
        string package,
        string targetFramework,
        IEnumerable<TestSetReport> testSets
    )
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(targetFramework);
        ArgumentNullException.ThrowIfNull(testSets);

        var sets = new List<TestSetReport>(testSets);
        sets.Sort(static (left, right) => string.CompareOrdinal(left.TestSet, right.TestSet));

        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        foreach (var set in sets)
        {
            var regressions = new HashSet<string>(set.Regressions, StringComparer.Ordinal);
            var unexpectedPasses = new HashSet<string>(set.UnexpectedPasses, StringComparer.Ordinal);
            foreach (var result in set.Cases)
            {
                // One root value per line: the writer starts over after every case.
                writer.Reset();
                writer.WriteStartObject();
                writer.WriteString("suite", package);
                writer.WriteString("targetFramework", targetFramework);
                writer.WriteString("testSet", set.TestSet);
                writer.WriteString("caseId", result.CaseId);
                writer.WriteString("status", Status(result, regressions, unexpectedPasses));
                if (!string.IsNullOrWhiteSpace(result.Reason))
                {
                    writer.WriteString("reason", result.Reason);
                }

                writer.WriteEndObject();
                writer.Flush();
                buffer.Write("\n"u8);
            }
        }

        return buffer.WrittenMemory;
    }

    /// <summary>
    /// Appends the lines of the cases to the file of the package and target framework in the given folder and
    /// creates the folder when it is missing; does nothing when the folder is <see langword="null"/> or empty, or
    /// when the test sets have no cases.
    /// </summary>
    /// <param name="package">The package that owns the suite.</param>
    /// <param name="targetFramework">The target framework of the test run.</param>
    /// <param name="testSets">The reports of the test sets.</param>
    /// <param name="directory">The folder of the report files.</param>
    public static void Append(
        string package,
        string targetFramework,
        IEnumerable<TestSetReport> testSets,
        string? directory
    )
    {
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        var lines = Render(package, targetFramework, testSets);
        if (lines.IsEmpty)
        {
            return;
        }

        var path = Path.Combine(Directory.CreateDirectory(directory).FullName, FileName(package, targetFramework));
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        stream.Write(lines.Span);
    }

    private static string Status(CaseResult result, HashSet<string> regressions, HashSet<string> unexpectedPasses) =>
        result.Outcome switch
        {
            CaseOutcome.Pass => unexpectedPasses.Contains(result.CaseId) ? "unexpected-pass" : "passed",
            CaseOutcome.Fail => regressions.Contains(result.CaseId) ? "regression" : "pending",
            _ => "skipped",
        };
}
