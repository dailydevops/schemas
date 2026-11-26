namespace NetEvolve.Schemas.Tests.Unit.Resources.Libxml2;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Resources.Default;

/// <summary>Resolves the references of the libxml2 base composition file against its fixed base and compares them with the result file.</summary>
internal static class Libxml2UriRunner
{
    /// <summary>The base URI the libxml2 driver composes every reference against.</summary>
    public const string BaseUri = "http://foo.com/path/to/index.html?orig#help";

    /// <summary>The single test set of the sub-tree.</summary>
    public const string TestSet = "uri";

    /// <summary>The result line of a reference that libxml2 refuses to compose.</summary>
    public const string ErrorLine = "::ERROR::";

    private static readonly char[] LineEnd = ['\n', '\r', ' ', '\t'];

    /// <summary>Runs the pairs of the two files.</summary>
    /// <param name="references">The text of <c>test/URI/uri.data</c>, one reference per line (an empty line is the empty reference).</param>
    /// <param name="expected">The text of <c>result/URI/uri.data</c>, one result per line.</param>
    /// <returns>One result per reference; the case id is <c>uri.data/n</c> with a one-based line number.</returns>
    /// <exception cref="InvalidDataException">The files have a different number of lines.</exception>
    public static List<CaseResult> Run(string references, string expected)
    {
        ArgumentNullException.ThrowIfNull(references);
        ArgumentNullException.ThrowIfNull(expected);
        var input = Lines(references);
        var output = Lines(expected);
        if (input.Count != output.Count)
        {
            throw new InvalidDataException($"{input.Count} references but {output.Count} results.");
        }

        var results = new List<CaseResult>(input.Count);
        for (var i = 0; i < input.Count; i++)
        {
            var id = $"uri.data/{i + 1}";
            var resolution = UriReference.Resolve(input[i], BaseUri);
            var passed =
                output[i] == ErrorLine
                    ? resolution.Uri is null
                    : string.Equals(resolution.Uri, output[i], StringComparison.Ordinal);
            results.Add(
                passed
                    ? new CaseResult(id, TestSet, CaseOutcome.Pass)
                    : new CaseResult(
                        id,
                        TestSet,
                        CaseOutcome.Fail,
                        $"'{input[i]}' gives '{resolution.Uri}', expected '{output[i]}'."
                    )
            );
        }

        return results;
    }

    private static List<string> Lines(string text)
    {
        var lines = text.Split('\n').Select(l => l.TrimEnd(LineEnd)).ToList();
        if (text.Length > 0 && text[^1] == '\n')
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }
}
