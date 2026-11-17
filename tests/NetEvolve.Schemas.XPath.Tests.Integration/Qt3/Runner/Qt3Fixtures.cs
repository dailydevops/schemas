namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using System.Text;
using NetEvolve.Schemas.Conformance.Fetch;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

internal sealed class FakeEngine(Func<Qt3Case, EngineResult> evaluate) : IQt3Engine
{
    public int Calls { get; private set; }

    public EngineResult Evaluate(Qt3Case testCase)
    {
        Calls++;
        return evaluate(testCase);
    }
}

internal sealed class FakeTransport(byte[] payload) : ISuiteTransport
{
    public Task<Stream> OpenAsync(Uri source, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream(payload, writable: false));
}

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory() => Path = Directory.CreateTempSubdirectory("qt3-").FullName;

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
}

internal static class Qt3Fixtures
{
    private const string CatalogNamespace = "http://www.w3.org/2010/09/qt-fots-catalog";

    public static Qt3Case Case(Qt3Assertion assertion, string name = "c1", string testSet = "set") =>
        new(testSet, name, "1", null, null, assertion, []);

    /// <summary>Writes a catalog with one test set per entry; the entry body is the inner XML of the test-set element.</summary>
    public static string WriteSuite(TempDirectory directory, params (string Name, string Body)[] sets)
    {
        var catalog = new StringBuilder().Append($"<catalog xmlns=\"{CatalogNamespace}\">");
        foreach (var (name, body) in sets)
        {
            _ = catalog.Append($"<test-set name=\"{name}\" file=\"{name}.xml\"/>");
            File.WriteAllText(
                Path.Combine(directory.Path, name + ".xml"),
                $"<test-set xmlns=\"{CatalogNamespace}\" name=\"{name}\">{body}</test-set>"
            );
        }
        File.WriteAllText(Path.Combine(directory.Path, "catalog.xml"), catalog.Append("</catalog>").ToString());
        return directory.Path;
    }

    public static string TestCase(string name, string dependencies = "", string result = "<assert-true/>") =>
        $"<test-case name=\"{name}\">{dependencies}<test>1</test><result>{result}</result></test-case>";

    public static string Dependency(string type, string value, string? satisfied = null) =>
        $"<dependency type=\"{type}\" value=\"{value}\"{(satisfied is null ? "" : $" satisfied=\"{satisfied}\"")}/>";
}
