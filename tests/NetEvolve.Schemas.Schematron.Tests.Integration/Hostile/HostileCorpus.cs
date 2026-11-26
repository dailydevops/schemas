namespace NetEvolve.Schemas.Schematron.Tests.Integration.Hostile;

using System.Text;

/// <summary>
/// The hostile corpus: every case is generated, because the sizes are the attack and a file would only hide them. Each case names the
/// diagnostic number its run must report; a case that needs a document outside the map ends on the resolver, never on a real file or address.
/// </summary>
internal static partial class HostileCorpus
{
    private const string Sch = "http://purl.oclc.org/dsdl/schematron";
    private const string Xsl = "http://www.w3.org/1999/XSL/Transform";
    private const string XInclude = "http://www.w3.org/2001/XInclude";

    private const string One = "<a/>";

    private static readonly int Denied = CoreDiagnosticCodes.ResourceDenied.Number;
    private static readonly int ExternalLimit = CoreDiagnosticCodes.ExternalResourceLimitExceeded.Number;

    public static IEnumerable<HostileCase> All() =>
        [.. Includes(), .. Recursion(), .. Growth(), .. Expressions(), .. Instances(), .. External()];

    /// <summary>The principal schema of a case: the 2025 edition of the XSLT 2 binding, with the namespaces the cases use.</summary>
    internal static string Schema(string body) =>
        $"<schema xmlns=\"{Sch}\" queryBinding=\"xslt2\" schematronEdition=\"2025\" xmlns:xsl=\"{Xsl}\" xmlns:f=\"urn:f\" xmlns:xi=\"{XInclude}\"><ns prefix=\"f\" uri=\"urn:f\"/>{body}</schema>";

    internal static string Library(string body) => $"<library xmlns=\"{Sch}\" xmlns:xi=\"{XInclude}\">{body}</library>";

    /// <summary>A pattern with one rule on the element <c>a</c> and one assertion.</summary>
    internal static string Rule(string test, string context = "a") =>
        $"<pattern id=\"p\"><rule context=\"{context}\"><assert test=\"{Escape(test)}\">Failed.</assert></rule></pattern>";

    internal static string Escape(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal);

    internal static string Repeat(string text, int count) =>
        new StringBuilder(text.Length * count).Insert(0, text, count).ToString();

    internal static HostileCase Make(
        string id,
        string schema,
        string? instance,
        int code,
        IReadOnlyDictionary<string, string>? documents = null,
        ValidationLimits? limits = null,
        bool external = false
    ) =>
        new(
            id,
            schema,
            documents ?? new Dictionary<string, string>(),
            instance,
            code,
            limits ?? ValidationLimits.Default,
            external
        );

    /// <summary>The documents of a case by their file name below the case address.</summary>
    internal static Dictionary<string, string> Files(string id, params (string Name, string Text)[] files) =>
        files.ToDictionary(f => "file:///hostile/" + id + "/" + f.Name, static f => f.Text, StringComparer.Ordinal);
}
