namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

/// <summary>A temporary XSpec project directory for the tests of the reader and the engine.</summary>
internal sealed class XspecFixture : IDisposable
{
    public const string XspecNamespace = "http://www.jenitennison.com/xslt/xspec";

    public XspecFixture() =>
        Root = Directory
            .CreateDirectory(Path.Combine(Path.GetTempPath(), "xspec-fixture-" + Guid.NewGuid().ToString("N")))
            .FullName;

    public string Root { get; }

    /// <summary>Writes a file below the root and returns its full path.</summary>
    public string Write(string relativePath, string content)
    {
        var path = Path.Combine(Root, relativePath);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Writes an XSpec description with the given attributes and body to <c>test/schematron-{name}.xspec</c>.</summary>
    public string WriteXspec(string name, string body, string attributes = "schematron=\"s.sch\"") =>
        Write(
            $"test/schematron-{name}.xspec",
            $"""<x:description xmlns:x="{XspecNamespace}" {attributes}>{body}</x:description>"""
        );

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
