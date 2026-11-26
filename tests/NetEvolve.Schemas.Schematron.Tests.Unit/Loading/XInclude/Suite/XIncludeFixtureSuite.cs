namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.Suite;

/// <summary>A small suite in the layout of the W3C XInclude test suite, written to a temporary directory.</summary>
internal sealed class XIncludeFixtureSuite : IDisposable
{
    private const string Catalog = """
        <?xml version="1.0" encoding="UTF-8"?>
        <!DOCTYPE testsuite SYSTEM "testcases.dtd">
        <testsuite>
          <testcases creator="Fixture" basedir="Alpha/test/docs">
            <testcase id="alpha-ok" href="ok.xml" type="success">
              <description>Includes a document.</description>
              <output>../../result/ok.xml</output>
            </testcase>
            <testcase id="alpha-differs" href="ok.xml" type="success">
              <description>The reference result differs.</description>
              <output>../../result/other.xml</output>
            </testcase>
            <testcase id="alpha-no-output" href="ok.xml" type="success"/>
            <testcase id="alpha-missing" href="missing.xml" type="error">
              <description>No fallback.</description>
            </testcase>
            <testcase id="alpha-error-but-ok" href="ok.xml" type="error"/>
            <testcase id="alpha-missing-but-success" href="missing.xml" type="success"/>
            <testcase id="alpha-malformed-error" href="malformed.xml" type="error"/>
            <testcase id="alpha-malformed-success" href="malformed.xml" type="success"/>
            <testcase id="alpha-no-reference" href="ok.xml" type="success">
              <output>../../result/absent.xml</output>
            </testcase>
            <testcase id="alpha-bad-reference" href="ok.xml" type="success">
              <output>../../result/malformed.xml</output>
            </testcase>
            <testcase id="alpha-xpointer" href="ok.xml" type="success" features="xpointer-scheme"/>
          </testcases>
          <testcases creator="Fixture" basedir="Beta/test">
            <testcase id="beta-entities" href="ok.xml" type="success" features="unparsed-entities lang-fixup">
              <output>../result/ok.xml</output>
            </testcase>
          </testcases>
        </testsuite>
        """;

    public XIncludeFixtureSuite()
    {
        Root = Path.Combine(Path.GetTempPath(), "xinclude-suite-" + Guid.NewGuid().ToString("N"));
        Write("testdescr.xml", Catalog);
        Write(
            "Alpha/test/docs/ok.xml",
            """<doc xmlns:xi="http://www.w3.org/2001/XInclude"><xi:include href="part.txt" parse="text"/></doc>"""
        );
        Write("Alpha/test/docs/part.txt", "hello");
        Write(
            "Alpha/test/docs/missing.xml",
            """<doc xmlns:xi="http://www.w3.org/2001/XInclude"><xi:include href="none.xml"/></doc>"""
        );
        Write("Alpha/test/docs/malformed.xml", "<doc>");
        Write("Alpha/result/ok.xml", "<doc>hello</doc>");
        Write("Alpha/result/other.xml", "<doc/>");
        Write("Alpha/result/malformed.xml", "<doc>");
    }

    public string Root { get; }

    public void Dispose() => Directory.Delete(Root, recursive: true);

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(Root, relativePath);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
