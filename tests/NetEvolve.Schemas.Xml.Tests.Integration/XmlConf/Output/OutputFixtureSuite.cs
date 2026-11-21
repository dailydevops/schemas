namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Output;

/// <summary>Writes a small suite with canonical output files: one equal, one with a one-byte difference, one with a document type declaration, and one without an output file.</summary>
internal static class OutputFixtureSuite
{
    public static string Create(string directory)
    {
        FixtureSuite.Write(
            directory,
            "xmlconf.xml",
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE TESTSUITE [
              <!ENTITY set-a SYSTEM "a/a.xml">
            ]>
            <TESTSUITE PROFILE="fixture">
            <TESTCASES PROFILE="Set A" xml:base="a/">
                &set-a;
            </TESTCASES>
            </TESTSUITE>
            """
        );
        FixtureSuite.Write(
            directory,
            "a/a.xml",
            """
            <TEST ID="equal" TYPE="valid" URI="equal.xml" OUTPUT="out/equal.xml" SECTIONS="2.1">Output equals.</TEST>
            <TEST ID="differs" TYPE="valid" URI="differs.xml" OUTPUT="out/differs.xml" SECTIONS="2.1">Output differs by one byte.</TEST>
            <TEST ID="doctype" TYPE="valid" URI="doctype.xml" OUTPUT="out/doctype.xml" SECTIONS="2.8">Has a document type declaration.</TEST>
            <TEST ID="no-output" TYPE="valid" URI="equal.xml" SECTIONS="2.1">Has no output file.</TEST>
            <TEST ID="not-wf" TYPE="not-wf" URI="notwf.xml" SECTIONS="2.1">Has no output file.</TEST>
            """
        );
        FixtureSuite.Write(directory, "a/equal.xml", "<?xml version=\"1.0\"?><doc b='2' a=\"1\">t&amp;<e/></doc>");
        FixtureSuite.Write(directory, "a/out/equal.xml", "<doc a=\"1\" b=\"2\">t&amp;<e></e></doc>");
        FixtureSuite.Write(directory, "a/differs.xml", "<doc b='2' a=\"1\">t&amp;<e/></doc>");
        FixtureSuite.Write(directory, "a/out/differs.xml", "<doc a=\"1\" b=\"3\">t&amp;<e></e></doc>");
        FixtureSuite.Write(directory, "a/doctype.xml", "<!DOCTYPE doc [<!ATTLIST doc a CDATA \"x\">]><doc/>");
        FixtureSuite.Write(directory, "a/out/doctype.xml", "<doc a=\"x\"></doc>");
        FixtureSuite.Write(directory, "a/notwf.xml", "<doc>");
        return directory;
    }
}
