namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using static Qt3Fixtures;

public class Qt3SelectionTests
{
    private static Qt3Selection Select(string setDependencies, params string[] cases)
    {
        using var temp = new TempDirectory();
        var root = WriteSuite(temp, ("set", setDependencies + string.Concat(cases)));
        return Qt3Selector.Select(Qt3CatalogReader.Read(root));
    }

    [Test]
    [Arguments("XP20", true)]
    [Arguments("XP20+", true)]
    [Arguments("XP10+", true)]
    [Arguments("XP20 XQ10", true)]
    [Arguments("XQ10+ XP20+", true)]
    [Arguments("XP30+", false)]
    [Arguments("XP30 XQ30", false)]
    [Arguments("XP31+ XQ31+", false)]
    [Arguments("XQ10+", false)]
    [Arguments("XQ10", false)]
    [Arguments("XT30+", false)]
    public async Task Select_SpecToken_MatchesOnlyXPath20(string spec, bool selected)
    {
        var selection = Select("", TestCase("c1", Dependency("spec", spec)));

        await Assert.That(selection.Selected.Count).IsEqualTo(selected ? 1 : 0);
        await Assert.That(selection.Excluded.Count).IsEqualTo(selected ? 0 : 1);
    }

    [Test]
    public async Task Select_CaseWithoutDependencies_IsSelected()
    {
        var selection = Select("", TestCase("c1"));

        await Assert.That(selection.Selected[0].Id).IsEqualTo("set/c1");
    }

    [Test]
    public async Task Select_SetAndCaseDependencies_AreAnded()
    {
        var caseFails = Select(
            Dependency("spec", "XP20+"),
            TestCase("ok", Dependency("spec", "XP20+")),
            TestCase("case-fails", Dependency("spec", "XQ10+"))
        );
        var setFails = Select(Dependency("spec", "XQ10+"), TestCase("set-fails", Dependency("spec", "XP20+")));

        await Assert.That(caseFails.Selected.Count).IsEqualTo(1);
        await Assert.That(caseFails.Selected[0].Name).IsEqualTo("ok");
        await Assert.That(caseFails.Excluded[0].CaseId).IsEqualTo("set/case-fails");
        await Assert.That(setFails.Selected).IsEmpty();
        await Assert.That(setFails.Excluded[0].CaseId).IsEqualTo("set/set-fails");
    }

    [Test]
    [Arguments("schemaImport")]
    [Arguments("schemaValidation")]
    [Arguments("staticTyping")]
    [Arguments("namespace-axis")]
    [Arguments("non_unicode_codepoint_collation")]
    public async Task Select_ExcludedFeature_IsExcludedWithReasonNamingIt(string feature)
    {
        var selection = Select("", TestCase("c1", Dependency("feature", feature)));

        await Assert.That(selection.Selected).IsEmpty();
        await Assert.That(selection.Excluded[0].Reason).Contains(feature);
    }

    [Test]
    public async Task Select_ExcludedFeatureWithSatisfiedFalse_IsSelected()
    {
        var selection = Select("", TestCase("c1", Dependency("feature", "staticTyping", "false")));

        await Assert.That(selection.Selected.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Select_OtherFeature_IsSelected()
    {
        var selection = Select("", TestCase("c1", Dependency("feature", "higherOrderFunctions")));

        await Assert.That(selection.Selected.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments("1.1", false)]
    [Arguments("1.0", true)]
    [Arguments("1.0:5+ 1.1", true)]
    public async Task Select_XmlVersion_ExcludesOnlyXml11(string value, bool selected)
    {
        var selection = Select("", TestCase("c1", Dependency("xml-version", value)));

        await Assert.That(selection.Selected.Count).IsEqualTo(selected ? 1 : 0);
        if (!selected)
        {
            await Assert.That(selection.Excluded[0].Reason).Contains("xml-version");
        }
    }

    [Test]
    [Arguments("1.1", false)]
    [Arguments("1.0", true)]
    public async Task Select_XsdVersion_ExcludesXsd11(string value, bool selected)
    {
        var selection = Select("", TestCase("c1", Dependency("xsd-version", value)));

        await Assert.That(selection.Selected.Count).IsEqualTo(selected ? 1 : 0);
        if (!selected)
        {
            await Assert.That(selection.Excluded[0].Reason).Contains("xsd-version");
        }
    }

    [Test]
    public async Task Selection_EnvironmentWithSchema_NotExcludedWithoutDeclaredDependency()
    {
        using var temp = new TempDirectory();
        var environment =
            "<environment name=\"schema-env\"><schema uri=\"urn:x\" file=\"x.xsd\"/><source role=\".\" file=\"x.xml\" validation=\"strict\"/></environment>";
        var root = WriteSuite(
            temp,
            (
                "set",
                environment
                    + "<test-case name=\"c1\"><environment ref=\"schema-env\"/><test>1</test><result><assert-true/></result></test-case>"
            )
        );

        var selection = Qt3Selector.Select(Qt3CatalogReader.Read(root));

        await Assert.That(selection.Selected[0].Environment).IsEqualTo("schema-env");
    }

    [Test]
    public async Task Read_TestElement_KeepsInlineQueryAndFileQuery()
    {
        using var temp = new TempDirectory();
        var root = WriteSuite(
            temp,
            (
                "set",
                "<test-case name=\"a\"><test>fn:abs(1)</test><result><assert-true/></result></test-case>"
                    + "<test-case name=\"b\"><test file=\"q.xq\"/><result><error code=\"XPST0003\"/></result></test-case>"
            )
        );

        var cases = Qt3CatalogReader.Read(root)[0].Cases;

        await Assert.That(cases[0].Query).IsEqualTo("fn:abs(1)");
        await Assert.That(cases[1].QueryFile).IsEqualTo(Path.Combine(root, "q.xq"));
        await Assert.That(cases[1].Assertion).IsEqualTo(new Qt3Assertion("error", "XPST0003"));
    }
}
