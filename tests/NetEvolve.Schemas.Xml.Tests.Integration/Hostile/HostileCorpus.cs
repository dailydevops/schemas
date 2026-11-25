namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

using System.Text;

/// <summary>The hostile input corpus: documents that must all end in a coded diagnostic.</summary>
/// <remarks>
/// A file is a generated document, because a bomb is better described by its recipe than by megabytes of text and the
/// owned path of the corpus holds no resource files. Every file loads with document type declaration processing on, so the
/// bombs reach the entity code; the library default options, which reject the declaration, are checked on their own.
/// Limitation: files that expose a defect are not part of the corpus until it is fixed; #829 (a multi-segment sequence
/// ignores the external access options) is filed.
/// </remarks>
public static partial class HostileCorpus
{
    private const string Origin = "http://corpus.test/";

    private static readonly Lazy<IReadOnlyList<HostileCase>> Cases = new(() =>
        [
            .. GeneralEntities(),
            .. RecursiveAndTightEntities(),
            .. ExternalGeneralEntities(),
            .. ParameterEntities(),
            .. Nesting(),
            .. HugeNames(),
            .. AttributeCounts(),
            .. TextSizes(),
            .. TokenSizes(),
            .. OtherLimits(),
            .. Unterminated(),
            .. Floods(),
            .. ExternalSubsets(),
            .. ExternalEntities(),
            .. ExternalParameterEntities(),
        ]
    );

    /// <summary>The processor time every file may use on every input kind.</summary>
    internal static TimeSpan TimeBound { get; } = TimeSpan.FromSeconds(10);

    /// <summary>Gets the names of the files of the corpus.</summary>
    /// <returns>The names, one per file.</returns>
    public static IEnumerable<string> Names() => Cases.Value.Select(file => file.Name);

    /// <summary>Gets a file of the corpus.</summary>
    /// <param name="name">The name of the file.</param>
    /// <returns>The file.</returns>
    internal static HostileCase Get(string name) =>
        Cases.Value.Single(file => string.Equals(file.Name, name, StringComparison.Ordinal));

    /// <summary>Gets every file of the corpus.</summary>
    /// <returns>The files; their documents are built on demand.</returns>
    internal static IReadOnlyList<HostileCase> All() => Cases.Value;

    private static string Repeat(string text, int count) =>
        new StringBuilder(text.Length * count).Insert(0, text, count).ToString();

    private static Dictionary<string, Func<string>> Resources(params (string Name, Func<string> Content)[] entries) =>
        entries.ToDictionary(entry => Origin + entry.Name, entry => entry.Content, StringComparer.Ordinal);
}
