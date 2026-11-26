namespace NetEvolve.Schemas.Xml.Tests.Integration.Libxml2;

/// <summary>Reads the documents of the libxml2 <c>test/errors</c> folder and decides from the message file whether the reader must reject each one.</summary>
internal static class Libxml2ErrorCatalog
{
    private static readonly string[] Rejections = [" parser error : ", " namespace error : ", " I/O error : "];

    /// <summary>Reads the folder.</summary>
    /// <param name="root">The libxml2 source root.</param>
    /// <returns>The case id (the file name) and the document path of every case, with whether it must be rejected.</returns>
    public static List<(string Id, string Path, bool MustReject)> Read(string root)
    {
        var cases = new List<(string, string, bool)>();
        var paths = Directory
            .GetFiles(Path.Combine(root, "test", "errors"), "*.xml")
            .OrderBy(p => p, StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var id = Path.GetFileName(path);
            var messages = Path.Combine(root, "result", "errors", id + ".err");
            cases.Add((id, path, MustReject(File.Exists(messages) ? File.ReadAllText(messages) : string.Empty)));
        }

        return cases;
    }

    /// <summary>Decides from the text of a libxml2 message file.</summary>
    /// <param name="messages">The message text.</param>
    /// <returns><see langword="true"/> when libxml2 reported a parser, namespace, or I/O error; warnings alone are no rejection.</returns>
    public static bool MustReject(string messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        return Rejections.Any(r => messages.Contains(r, StringComparison.Ordinal));
    }
}
