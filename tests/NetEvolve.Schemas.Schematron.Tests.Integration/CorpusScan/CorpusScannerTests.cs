namespace NetEvolve.Schemas.Schematron.Tests.Integration.CorpusScan;

public class CorpusScannerTests
{
    private static RuleSetScan ScanFixture(string file) =>
        CorpusScanner.Scan(Path.Combine(CorpusFiles.FixturesDirectory, file), CorpusFiles.FixturesDirectory, file);

    [Test]
    public async Task Scan_FunctionElementInDefaultNamespace_CountsAsXslFunction()
    {
        var scan = ScanFixture("default-namespace.sch");

        await Assert.That(scan.Elements["xsl:function"]).IsEqualTo(1);
        await Assert.That(scan.Elements["xsl:param"]).IsEqualTo(1);
        await Assert.That(scan.Elements["xsl:value-of"]).IsEqualTo(1);
    }

    [Test]
    public async Task Scan_UnusualPrefix_GivesTheSameCountsAsDefaultNamespace()
    {
        var reference = ScanFixture("default-namespace.sch");
        var scan = ScanFixture("unusual-prefix.sch");

        await Assert.That(scan.Elements).IsEquivalentTo(reference.Elements);
        await Assert.That(scan.Functions).IsEquivalentTo(reference.Functions);
    }

    [Test]
    public async Task Scan_LocalNameFunctionInAnotherNamespace_IsNotCounted()
    {
        var scan = ScanFixture("unusual-prefix.sch");

        await Assert.That(scan.Elements["xsl:function"]).IsEqualTo(1);
        await Assert.That(scan.Elements.Keys.Any(k => !k.StartsWith("xsl:", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task Scan_PartReachedByXIncludeAndSchematronInclude_IsCountedOnce()
    {
        var scan = ScanFixture("with-xinclude.sch");

        await Assert.That(scan.Elements["xsl:function"]).IsEqualTo(1);
        await Assert.That(scan.Elements["xsl:param"]).IsEqualTo(1);
        await Assert.That(scan.Elements["xsl:sequence"]).IsEqualTo(1);
        await Assert.That(scan.Functions["concat"]).IsEqualTo(1);
        await Assert.That(scan.Functions["exists"]).IsEqualTo(1);
        await Assert.That(scan.Unresolved).IsEmpty();
    }

    [Test]
    public async Task Scan_FunctionNamesInsideStringLiteralsAndKeywords_AreNotCounted(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        var path = Path.Combine(temp.Path, "rules.sch");
        await File.WriteAllTextAsync(
            path,
            """
            <schema xmlns="http://purl.oclc.org/dsdl/schematron">
              <pattern><rule context="a"><assert test="if (b) then string('count(x)') else text() and (c) or (d)">m</assert></rule></pattern>
            </schema>
            """,
            cancellationToken
        );

        var scan = CorpusScanner.Scan(path, temp.Path, "rules.sch");

        await Assert.That(scan.Functions.Keys).IsEquivalentTo(["string"]);
    }

    [Test]
    public async Task Scan_MissingAndOutsideAndRemoteIncludes_AreReportedAsUnresolved(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var temp = new TempDirectory();
        var root = Directory.CreateDirectory(Path.Combine(temp.Path, "root")).FullName;
        await File.WriteAllTextAsync(
            Path.Combine(temp.Path, "outside.xml"),
            "<function xmlns=\"http://www.w3.org/1999/XSL/Transform\"/>",
            cancellationToken
        );
        var path = Path.Combine(root, "rules.sch");
        await File.WriteAllTextAsync(
            path,
            """
            <schema xmlns="http://purl.oclc.org/dsdl/schematron" xmlns:xi="http://www.w3.org/2001/XInclude">
              <include href="missing.sch"/>
              <xi:include href="../outside.xml"/>
              <xi:include href="https://example.invalid/remote.xml"/>
            </schema>
            """,
            cancellationToken
        );

        var scan = CorpusScanner.Scan(path, root, "rules.sch");

        await Assert.That(scan.Elements).IsEmpty();
        await Assert.That(scan.Unresolved.Count).IsEqualTo(3);
    }
}
