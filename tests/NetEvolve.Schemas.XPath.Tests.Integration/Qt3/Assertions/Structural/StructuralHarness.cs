namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural;

using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;
using static NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner.Qt3Fixtures;

/// <summary>Runs one case from a hand-written catalog fragment on the real engine: the source document is the context item and <c>p</c> is bound to <c>urn:p</c>.</summary>
internal static class StructuralHarness
{
    public const string Document = "<doc id=\"7\"><a>1</a>x<b/></doc>";

    private const string Environment =
        "<environment name=\"e\"><namespace prefix=\"p\" uri=\"urn:p\"/><source role=\".\" file=\"doc.xml\"/></environment>";

    public static CaseResult Run(
        string query,
        string assertion,
        string document = Document,
        params (string Name, string Content)[] files
    )
    {
        using var directory = new TempDirectory();
        var testCaseXml =
            $"<test-case name=\"c1\"><environment ref=\"e\"/><test>{query}</test><result>{assertion}</result></test-case>";
        var root = WriteSuite(directory, ("set", Environment + testCaseXml));
        File.WriteAllText(Path.Combine(directory.Path, "doc.xml"), document);
        foreach (var (name, content) in files)
        {
            File.WriteAllText(Path.Combine(directory.Path, name), content);
        }

        var testCase = Qt3CatalogReader.Read(root)[0].Cases[0];
        return Qt3Runner.Run(testCase, new Qt3Engine(root));
    }

    public static string Xml(string expected, bool ignorePrefixes = false) =>
        $"<assert-xml{(ignorePrefixes ? " ignore-prefixes=\"true\"" : "")}><![CDATA[{expected}]]></assert-xml>";
}
