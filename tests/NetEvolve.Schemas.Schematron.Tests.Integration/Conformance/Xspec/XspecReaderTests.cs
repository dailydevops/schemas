namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

public class XspecReaderTests
{
    [Test]
    public async Task Read_NestedScenarios_YieldOneCasePerExpectationWithTheInheritedContext()
    {
        using var fixture = new XspecFixture();
        _ = fixture.Write("test/s.sch", "<schema/>");
        _ = fixture.Write("test/i.xml", "<a/>");
        _ = fixture.WriteXspec(
            "a",
            """
            <x:scenario label="outer">
              <x:context href="i.xml"/>
              <x:scenario label="inner one">
                <x:expect-assert id="a1" role="error" location="/a[1]"/>
                <x:expect-not-report id="r1"/>
              </x:scenario>
              <x:expect-valid/>
            </x:scenario>
            """
        );

        var selection = XspecReader.Read(fixture.Root);

        await Assert.That(selection.Excluded).IsEmpty();
        await Assert.That(selection.Selected).Count().IsEqualTo(3);
        var first = selection.Selected.First(c => c.Expectation.Kind == XspecExpectKind.Assert);
        await Assert.That(first.TestSet).IsEqualTo("test-schematron-a");
        await Assert.That(first.Id).StartsWith("test-schematron-a/");
        await Assert.That(first.Id).Contains("outer inner one");
        await Assert.That(first.SchemaPath).IsEqualTo(Path.Combine(fixture.Root, "test", "s.sch"));
        await Assert.That(first.Context.Path).IsEqualTo(Path.Combine(fixture.Root, "test", "i.xml"));
        await Assert.That(first.Expectation.Id).IsEqualTo("a1");
        await Assert.That(first.Expectation.Role).IsEqualTo("error");
        await Assert.That(first.Expectation.Location).IsEqualTo("/a[1]");
        await Assert
            .That(selection.Selected.Select(c => c.Expectation.Kind))
            .IsEquivalentTo([XspecExpectKind.Assert, XspecExpectKind.NotReport, XspecExpectKind.Valid]);
        await Assert.That(selection.Selected.Select(c => c.Id).Distinct(StringComparer.Ordinal)).Count().IsEqualTo(3);
    }

    [Test]
    public async Task Read_InlineContext_IsSerializedAsTheInstance()
    {
        using var fixture = new XspecFixture();
        _ = fixture.Write("test/s.sch", "<schema/>");
        _ = fixture.WriteXspec(
            "a",
            """
            <x:scenario label="s">
              <x:context><p>foo<span>bar</span></p></x:context>
              <x:expect-report id="r" count="2">text  here</x:expect-report>
            </x:scenario>
            """
        );

        var selected = XspecReader.Read(fixture.Root).Selected.Single();

        await Assert.That(selected.Context.Path).IsNull();
        await Assert.That(selected.Context.Xml).IsEqualTo("<p>foo<span>bar</span></p>");
        await Assert.That(selected.Expectation.Count).IsEqualTo(2);
        await Assert.That(selected.Expectation.Text).IsEqualTo("text here");
    }

    [Test]
    public async Task Read_PhaseParameter_AppliesToGlobalAndScenarioScope()
    {
        using var fixture = new XspecFixture();
        _ = fixture.Write("test/s.sch", "<schema/>");
        _ = fixture.Write("test/i.xml", "<a/>");
        _ = fixture.WriteXspec(
            "a",
            """
            <x:param name="schxslt:phase" xmlns:schxslt="urn:schxslt">PhaseB</x:param>
            <x:scenario label="global"><x:context href="i.xml"/><x:expect-valid/></x:scenario>
            <x:scenario label="own">
              <x:param name="phase">PhaseC</x:param>
              <x:context href="i.xml"/>
              <x:expect-valid/>
            </x:scenario>
            """
        );

        var selected = XspecReader.Read(fixture.Root).Selected;

        await Assert.That(selected.Select(c => c.Phase ?? "")).IsEquivalentTo(["PhaseB", "PhaseC"]);
    }

    [Test]
    public async Task Read_XmlBase_ResolvesSchemaAndContextAgainstIt()
    {
        using var fixture = new XspecFixture();
        _ = fixture.Write("test/sub/s.sch", "<schema/>");
        _ = fixture.Write("test/sub/i.xml", "<a/>");
        _ = fixture.WriteXspec(
            "a",
            """<x:scenario label="s"><x:context href="i.xml"/><x:expect-valid/></x:scenario>""",
            "schematron=\"s.sch\" xml:base=\"sub/\""
        );

        var selected = XspecReader.Read(fixture.Root).Selected.Single();

        await Assert.That(selected.SchemaPath).IsEqualTo(Path.Combine(fixture.Root, "test", "sub", "s.sch"));
        await Assert.That(selected.Context.Path).IsEqualTo(Path.Combine(fixture.Root, "test", "sub", "i.xml"));
    }

    [Test]
    public async Task Read_Import_AddsTheScenariosOfTheImportedFileToTheImportingFile()
    {
        using var fixture = new XspecFixture();
        _ = fixture.Write("test/s.sch", "<schema/>");
        _ = fixture.Write("test/i.xml", "<a/>");
        _ = fixture.Write(
            "test/other.xspec",
            $"""
            <x:description xmlns:x="{XspecFixture.XspecNamespace}">
              <x:scenario label="imported"><x:context href="i.xml"/><x:expect-valid/></x:scenario>
            </x:description>
            """
        );
        _ = fixture.WriteXspec(
            "a",
            """
            <x:import href="other.xspec"/>
            <x:scenario label="own"><x:context href="i.xml"/><x:expect-valid/></x:scenario>
            """
        );

        var selection = XspecReader.Read(fixture.Root);

        await Assert.That(selection.Selected).Count().IsEqualTo(2);
        await Assert.That(selection.Selected.All(c => c.TestSet == "test-schematron-a")).IsTrue();
        await Assert
            .That(selection.Selected.Select(c => c.Id))
            .Contains(id => id.Contains("imported", StringComparison.Ordinal));
    }

    [Test]
    [Arguments(
        """<x:scenario label="s" pending="why"><x:context href="i.xml"/><x:expect-valid/></x:scenario>""",
        "pending"
    )]
    [Arguments("""<x:scenario label="s"><x:expect-valid/></x:scenario>""", "no context")]
    [Arguments(
        """<x:scenario label="s"><x:context href="i.xml"/><x:expect-rule id="r"/></x:scenario>""",
        "x:expect-rule"
    )]
    [Arguments("""<x:scenario label="s"><x:context href="i.xml"/><x:expect test="1"/></x:scenario>""", "x:expect")]
    [Arguments(
        """<x:scenario label="s"><x:call function="f"/><x:context href="i.xml"/><x:expect-valid/></x:scenario>""",
        "x:call"
    )]
    [Arguments(
        """<x:scenario label="s"><x:variable name="v" select="1"/><x:context href="i.xml"/><x:expect-valid/></x:scenario>""",
        "x:variable"
    )]
    [Arguments(
        """<x:scenario label="s"><x:context href="i.xml" select="/a"/><x:expect-valid/></x:scenario>""",
        "select"
    )]
    [Arguments("""<x:scenario label="s"><x:context href="missing.xml"/><x:expect-valid/></x:scenario>""", "not found")]
    [Arguments(
        """<x:scenario label="s"><x:context><a/><b/></x:context><x:expect-valid/></x:scenario>""",
        "one element"
    )]
    [Arguments(
        """<x:pending label="why"><x:scenario label="s"><x:context href="i.xml"/><x:expect-valid/></x:scenario></x:pending>""",
        "x:pending"
    )]
    [Arguments(
        """<x:param name="other">1</x:param><x:scenario label="s"><x:context href="i.xml"/><x:expect-valid/></x:scenario>""",
        "parameter other"
    )]
    public async Task Read_UnsupportedConstruct_IsExcludedWithItsReason(string body, string reasonPart)
    {
        using var fixture = new XspecFixture();
        _ = fixture.Write("test/s.sch", "<schema/>");
        _ = fixture.Write("test/i.xml", "<a/>");
        _ = fixture.WriteXspec("a", body);

        var selection = XspecReader.Read(fixture.Root);

        await Assert.That(selection.Selected).IsEmpty();
        await Assert.That(selection.Excluded).IsNotEmpty();
        await Assert.That(selection.Excluded.All(e => e.TestSet == "test-schematron-a")).IsTrue();
        await Assert
            .That(selection.Excluded.Any(e => e.Reason.Contains(reasonPart, StringComparison.Ordinal)))
            .IsTrue();
    }

    [Test]
    public async Task Read_FileWithoutSchematronAttribute_IsExcludedAsAWhole()
    {
        using var fixture = new XspecFixture();
        _ = fixture.WriteXspec("a", """<x:scenario label="s"><x:expect-valid/></x:scenario>""", "");

        var selection = XspecReader.Read(fixture.Root);

        await Assert.That(selection.Selected).IsEmpty();
        var excluded = selection.Excluded.Single();
        await Assert.That(excluded.TestSet).IsEqualTo("test-schematron-a");
        await Assert.That(excluded.Reason).Contains("schematron attribute");
    }

    [Test]
    public async Task Read_MissingSchemaFile_ExcludesTheFile()
    {
        using var fixture = new XspecFixture();
        _ = fixture.WriteXspec("a", """<x:scenario label="s"><x:expect-valid/></x:scenario>""");

        var excluded = XspecReader.Read(fixture.Root).Excluded.Single();

        await Assert.That(excluded.Reason).Contains("not found");
    }

    [Test]
    public async Task Read_FilesWithoutSchematronInThePath_AreNotRead()
    {
        using var fixture = new XspecFixture();
        _ = fixture.Write("test/s.sch", "<schema/>");
        _ = fixture.Write(
            "test/plain.xspec",
            $"""<x:description xmlns:x="{XspecFixture.XspecNamespace}" schematron="s.sch"/>"""
        );
        _ = fixture.Write(
            "other/schematron-x.xspec",
            $"""<x:description xmlns:x="{XspecFixture.XspecNamespace}" schematron="s.sch"/>"""
        );
        _ = fixture.Write(
            "tutorial/schematron/demo.xspec",
            $"""<x:description xmlns:x="{XspecFixture.XspecNamespace}" schematron="../../test/s.sch"/>"""
        );

        var selection = XspecReader.Read(fixture.Root);

        await Assert.That(selection.Selected).IsEmpty();
        await Assert.That(selection.Excluded).IsEmpty();
    }

    [Test]
    public async Task Read_PathExclusion_ExcludesTheWholeFileWithAReason()
    {
        using var fixture = new XspecFixture();
        _ = fixture.Write("test/schematron/bad-location/s.sch", "<schema/>");
        _ = fixture.Write("test/schematron/bad-location/i.xml", "<a/>");
        _ = fixture.Write(
            "test/schematron/bad-location/expect-assert.xspec",
            $"""
            <x:description xmlns:x="{XspecFixture.XspecNamespace}" schematron="s.sch">
              <x:scenario label="s"><x:context href="i.xml"/><x:expect-assert location="1"/></x:scenario>
            </x:description>
            """
        );

        var selection = XspecReader.Read(fixture.Root);

        await Assert.That(selection.Selected).IsEmpty();
        await Assert.That(selection.Excluded.Single().TestSet).IsEqualTo("test-schematron-bad-location-expect-assert");
    }

    [Test]
    public async Task Read_Result_IsSortedAndRepeatable()
    {
        using var fixture = new XspecFixture();
        _ = fixture.Write("test/s.sch", "<schema/>");
        _ = fixture.Write("test/i.xml", "<a/>");
        const string body = """<x:scenario label="s"><x:context href="i.xml"/><x:expect-valid/></x:scenario>""";
        _ = fixture.WriteXspec("b", body);
        _ = fixture.WriteXspec("a", body);

        var first = XspecReader.Read(fixture.Root).Selected.Select(c => c.Id).ToArray();
        var second = XspecReader.Read(fixture.Root).Selected.Select(c => c.Id).ToArray();

        await Assert.That(first).IsEquivalentTo(second);
        await Assert.That(first).IsEquivalentTo(first.OrderBy(id => id, StringComparer.Ordinal));
    }
}
