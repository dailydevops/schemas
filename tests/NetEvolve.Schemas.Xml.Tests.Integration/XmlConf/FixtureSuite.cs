namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

/// <summary>Writes a small suite in the layout of the W3C XML Conformance Test Suite: a catalog that includes one entity file per test set.</summary>
internal static class FixtureSuite
{
    public static string Create(string directory)
    {
        Write(
            directory,
            "xmlconf.xml",
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE TESTSUITE [
              <!ENTITY set-a SYSTEM "a/a.xml">
              <!ENTITY set-b SYSTEM "b/b.xml">
            ]>
            <TESTSUITE PROFILE="fixture">
            <TESTCASES PROFILE="Set A" xml:base="a/">
                &set-a;
            </TESTCASES>
            <TESTCASES PROFILE="Set B" xml:base="b/">
                &set-b;
            </TESTCASES>
            </TESTSUITE>
            """
        );
        Write(
            directory,
            "a/a.xml",
            """
            <TEST ID="valid-1" TYPE="valid" ENTITIES="none" URI="valid-1.xml" SECTIONS="2.1">A valid case.</TEST>
            <TEST ID="notwf-1" TYPE="not-wf" ENTITIES="general" URI="sub/notwf-1.xml" SECTIONS="4.1" EDITION="5">A not-wf case with <EM>markup</EM>.</TEST>
            <TEST ID="invalid-1" TYPE="invalid" ENTITIES="parameter" URI="invalid-1.xml" SECTIONS="3.2">An invalid case.</TEST>
            <TEST ID="error-1" TYPE="error" ENTITIES="both" URI="error-1.xml" SECTIONS="4.2.2">An error case.</TEST>
            <TEST ID="default-1" TYPE="valid" URI="default-1.xml" SECTIONS="2.1">No ENTITIES and no NAMESPACE attribute.</TEST>
            """
        );
        Write(
            directory,
            "b/b.xml",
            """
            <TEST ID="xml11-version" TYPE="valid" VERSION="1.1" URI="v11.xml" SECTIONS="2.2">Version 1.1 only.</TEST>
            <TEST ID="xml11-recommendation" TYPE="not-wf" RECOMMENDATION="XML1.1" URI="r11.xml" SECTIONS="2.2">XML 1.1 recommendation.</TEST>
            <TEST ID="ns11" TYPE="valid" RECOMMENDATION="NS1.1" URI="ns11.xml" SECTIONS="2">Namespaces 1.1.</TEST>
            <TEST ID="edition-1-4" TYPE="not-wf" VERSION="1.0" EDITION="1 2 3 4" URI="e14.xml" SECTIONS="2.3">Editions one to four.</TEST>
            <TEST ID="edition-4-5" TYPE="valid" VERSION="1.0" EDITION="4 5" URI="e45.xml" SECTIONS="2.3">Editions four and five.</TEST>
            <TEST ID="both-versions" TYPE="valid" VERSION="1.0 1.1" URI="both.xml" SECTIONS="2.2">Both versions.</TEST>
            <TEST ID="no-namespaces" TYPE="valid" NAMESPACE="no" URI="nons.xml" SECTIONS="2.3">Colons without namespace processing.</TEST>
            """
        );
        return directory;
    }

    public static void Write(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath);
        _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
