namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

/// <summary>Writes a small suite in the layout of the official Schematron conformance suite.</summary>
internal static class FixtureSuite
{
    public const string Namespace = "https://doi.org/10.5281/zenodo.5679629#";

    public static string Create(string directory)
    {
        Write(directory, "iso-schematron-xslt-1.xml", Suite("""<include href="iso-schematron-xslt-common.xml"/>"""));
        Write(
            directory,
            "iso-schematron-xslt-2.xml",
            Suite(
                """
                <include href="iso-schematron-xslt-common.xml"/>
                <testcase href="set-b/only-xslt2.xml"/>
                """
            )
        );
        Write(
            directory,
            "iso-schematron-xslt-3.xml",
            Suite(
                """
                <include href="iso-schematron-xslt-2.xml"/>
                <testcase href="set-b/only-xslt3.xml"/>
                """
            )
        );
        Write(
            directory,
            "iso-schematron-xslt-common.xml",
            Suite(
                """
                <testcase href="set-a/one.xml"/>
                <testcase href="set-a/two.xml"/>
                """
            )
        );
        Write(directory, "set-a/one.xml", Case("invalid", "<root xmlns=\"\"><child/></root>", phase: "p1"));
        Write(directory, "set-a/two.xml", Case("valid", "<root xmlns=\"\"/>"));
        Write(directory, "set-b/only-xslt2.xml", Case("error", "<root xmlns=\"\"/>"));
        Write(directory, "set-b/only-xslt3.xml", Case("valid", "<root xmlns=\"\"/>"));
        return directory;
    }

    public static string Case(string expect, string instance, string? phase = null, int schemaCount = 1)
    {
        var phaseAttribute = phase is null ? "" : $" phase=\"{phase}\"";
        var schema =
            """<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron"><sch:pattern><sch:rule context="/"><sch:assert test="true()"/></sch:rule></sch:pattern></sch:schema>""";
        var schemas = string.Concat(Enumerable.Repeat(schema, schemaCount));
        return $"""
            <testcase expect="{expect}"{phaseAttribute} xmlns="{Namespace}">
              <label>fixture</label>
              <schemas>{schemas}</schemas>
              <documents>
                <document filename="document.xml">{instance}</document>
                <document filename="extra.xml"><extra xmlns=""/></document>
              </documents>
              <assertions xmlns:svrl="http://purl.oclc.org/dsdl/svrl">
                <assertion assert="count(//svrl:failed-assert) = 0"/>
              </assertions>
            </testcase>
            """;
    }

    private static string Suite(string body) =>
        $"""<testsuite xmlns="{Namespace}"><label>fixture</label>{body}</testsuite>""";

    private static void Write(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
