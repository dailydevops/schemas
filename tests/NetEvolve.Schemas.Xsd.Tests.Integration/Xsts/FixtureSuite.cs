namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

using System.Globalization;
using System.Text;

/// <summary>Writes a small suite in the layout of the W3C XML Schema Test Suite: suite.xml, extra-suite.xml, test sets.</summary>
internal static class FixtureSuite
{
    private const string Header =
        "xmlns=\"http://www.w3.org/XML/2004/xml-schema-test-suite/\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"";

    public static string Create(string directory)
    {
        WriteSuiteFiles(directory);
        Write(directory, "metaA/alpha.testSet", TestSet("Alpha", null, [.. VersionGroups(), .. StatusGroups()]));
        WriteOtherSets(directory);
        return directory;
    }

    private static void WriteSuiteFiles(string directory)
    {
        Write(
            directory,
            "suite.xml",
            $"""
            <testSuite {Header} name="fixture" releaseDate="2007-06-15" schemaVersion="fixture">
              <testSetRef xlink:href="metaA/alpha.testSet"/>
              <testSetRef xlink:href="metaB/beta.xml"/>
              <testSetRef xlink:href="metaB/alpha.testSet"/>
            </testSuite>
            """
        );
        Write(
            directory,
            "extra-suite.xml",
            $"""
            <testSuite {Header} name="extra" releaseDate="2007-06-15" schemaVersion="fixture">
              <testSetRef xlink:href="metaA/alpha.testSet"/>
              <testSetRef xlink:href="metaC/decimal.testSet"/>
            </testSuite>
            """
        );
    }

    private static void WriteOtherSets(string directory)
    {
        Write(
            directory,
            "metaB/beta.xml",
            TestSet(
                "Beta",
                "1.1",
                Group("both", "1.0 1.1", SchemaTest("schema", null, "valid", "accepted", "../data/both.xsd")),
                Group(
                    "narrowed",
                    "1.0 1.1",
                    InstanceTest("inst10", "1.0", "valid", "accepted", "../data/n10.xml"),
                    InstanceTest("inst11", "1.1", "valid", "accepted", "../data/n11.xml")
                )
            )
        );
        Write(
            directory,
            "metaB/alpha.testSet",
            TestSet(
                "Alpha",
                null,
                Group("plain", null, SchemaTest("schema", null, "invalid", "accepted", "../data/other.xsd"))
            )
        );
        Write(
            directory,
            "metaC/decimal.testSet",
            TestSet(
                "Decimal",
                "1.1",
                Group("pd", null, SchemaTest("schema", null, "valid", "accepted", "../data/pd.xsd"))
            )
        );
    }

    private static string[] VersionGroups() =>
        [
            Group(
                "plain",
                null,
                SchemaTest("schema", null, "valid", "accepted", "../data/plain.xsd"),
                InstanceTest("inst-valid", null, "valid", "accepted", "../data/valid.xml"),
                InstanceTest("inst-invalid", null, "invalid", "stable", "../data/invalid.xml")
            ),
            Group("only10", "1.0", SchemaTest("schema", null, "valid", "accepted", "../data/only10.xsd")),
            Group("only11", "1.1", SchemaTest("schema", null, "valid", "accepted", "../data/only11.xsd")),
            Group(
                "differs",
                "1.0 1.1",
                SchemaTest("schema", null, "valid", "accepted", "../data/differs.xsd"),
                InstanceTest("inst", null, "accepted", "../data/differs.xml", ("1.0", "valid"), ("1.1", "invalid"))
            ),
            Group(
                "defaulted",
                null,
                InstanceTest("inst", null, "accepted", "../data/defaulted.xml", (null, "valid"), ("1.0", "invalid"))
            ),
        ];

    private static string[] StatusGroups() =>
        [
            Group(
                "statuses",
                null,
                InstanceTest("accepted", null, "valid", "accepted", "../data/s.xml"),
                InstanceTest("stable", null, "valid", "stable", "../data/s.xml"),
                InstanceTest("queried", null, "valid", "queried", "../data/s.xml"),
                InstanceTest("disputed-test", null, "valid", "disputed-test", "../data/s.xml"),
                InstanceTest("disputed-spec", null, "valid", "disputed-spec", "../data/s.xml"),
                InstanceTest("submitted", null, "valid", "submitted", "../data/s.xml"),
                InstanceTest("no-status", null, "valid", null, "../data/s.xml")
            ),
            Group(
                "undetermined",
                null,
                InstanceTest("indeterminate", null, "indeterminate", "accepted", "../data/u.xml"),
                InstanceTest(
                    "feature-expected",
                    null,
                    "accepted",
                    "../data/u.xml",
                    ("Unicode_4.0.0", "valid"),
                    ("Unicode_6.0.0", "invalid")
                ),
                InstanceTest("only-other-version", null, "accepted", "../data/u.xml", ("1.0", "valid"))
            ),
            Group("feature-gated", "full-xpath-in-CTA", SchemaTest("schema", null, "valid", "accepted", "../d.xsd")),
            Group("schemaless", null, InstanceTest("inst", null, "valid", "accepted", "../data/n.xml")),
            Group("multi", null, SchemaTest("schema", null, "valid", "accepted", "../data/a.xsd", "../data/sub/b.xsd")),
        ];

    private static string TestSet(string name, string? version, params string[] groups) =>
        $"""<testSet {Header} contributor="fixture" name="{name}"{Attribute("version", version)}>{string.Concat(groups)}</testSet>""";

    private static string Group(string name, string? version, params string[] tests) =>
        $"""<testGroup name="{name}"{Attribute("version", version)}>{string.Concat(tests)}</testGroup>""";

    private static string SchemaTest(
        string name,
        string? version,
        string validity,
        string? status,
        params string[] documents
    )
    {
        var builder = new StringBuilder();
        _ = builder.Append(
            CultureInfo.InvariantCulture,
            $"""<schemaTest name="{name}"{Attribute("version", version)}>"""
        );
        foreach (var document in documents)
        {
            _ = builder.Append(CultureInfo.InvariantCulture, $"""<schemaDocument xlink:href="{document}"/>""");
        }

        return builder
            .Append(
                CultureInfo.InvariantCulture,
                $"""<expected validity="{validity}"/>{Current(status)}</schemaTest>"""
            )
            .ToString();
    }

    private static string InstanceTest(
        string name,
        string? version,
        string validity,
        string? status,
        string document
    ) => InstanceTest(name, version, status, document, (null, validity));

    private static string InstanceTest(
        string name,
        string? version,
        string? status,
        string document,
        params (string? Version, string Validity)[] expected
    )
    {
        var builder = new StringBuilder();
        _ = builder.Append(
            CultureInfo.InvariantCulture,
            $"""<instanceTest name="{name}"{Attribute("version", version)}>"""
        );
        _ = builder.Append(CultureInfo.InvariantCulture, $"""<instanceDocument xlink:href="{document}"/>""");
        foreach (var (expectedVersion, validity) in expected)
        {
            _ = builder.Append(
                CultureInfo.InvariantCulture,
                $"""<expected{Attribute("version", expectedVersion)} validity="{validity}"/>"""
            );
        }

        return builder.Append(Current(status)).Append("</instanceTest>").ToString();
    }

    private static string Current(string? status) =>
        status is null ? "" : $"""<current status="{status}" date="2007-01-01"/>""";

    private static string Attribute(string name, string? value) => value is null ? "" : $" {name}=\"{value}\"";

    private static void Write(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
