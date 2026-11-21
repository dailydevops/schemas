namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The hostile input corpus: documents that must all end in a coded diagnostic.</summary>
public static class HostileCorpus
{
    /// <summary>The time every file may take on every input kind.</summary>
    internal static readonly TimeSpan TimeBound = TimeSpan.FromSeconds(10);

    /// <summary>Gets the names of the files of the corpus.</summary>
    /// <returns>The names, one per file.</returns>
    public static IEnumerable<string> Names() => All().Select(file => file.Name);

    /// <summary>Gets a file of the corpus.</summary>
    /// <param name="name">The name of the file.</param>
    /// <returns>The file.</returns>
    internal static HostileCase Get(string name) =>
        All().Single(file => string.Equals(file.Name, name, StringComparison.Ordinal));

    /// <summary>Gets every file of the corpus.</summary>
    /// <returns>The files; their documents are built on demand.</returns>
    internal static IReadOnlyList<HostileCase> All() =>
        [
            new HostileCase("nesting-257-elements", HostileCategory.DeepNesting, XmlErrorCodes.XML1102, () =>
                string.Concat(Enumerable.Repeat("<a>", 257))
            ),
        ];
}
