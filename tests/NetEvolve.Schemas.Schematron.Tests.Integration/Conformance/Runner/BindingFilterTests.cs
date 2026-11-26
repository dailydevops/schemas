namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

public class BindingFilterTests
{
    private static readonly BindingRule[] Rules =
    [
        new("xslt-1", "iso-schematron-xslt-1.xml"),
        new("xslt-2", "iso-schematron-xslt-2.xml"),
        new("xslt-3", "iso-schematron-xslt-3.xml", "xslt3 cases belong to SCH-21."),
    ];

    [Test]
    public async Task Select_CasesOfSelectedBindings_AreKeptAndOthersExcludedWithReason()
    {
        using var fixture = new FixtureDirectory();
        var cases = SchematronCatalogReader.Read(fixture.Path, Rules);

        var selection = BindingFilter.Select(cases, Rules);

        await Assert
            .That(selection.Selected.Select(c => c.Id))
            .IsEquivalentTo(["set-a/one.xml", "set-a/two.xml", "set-b/only-xslt2.xml"]);
        var excluded = selection.Excluded.Single();
        await Assert.That(excluded.Case.Id).IsEqualTo("set-b/only-xslt3.xml");
        await Assert.That(excluded.Reason).IsEqualTo("xslt3 cases belong to SCH-21.");
    }

    [Test]
    public async Task Select_CaseInSelectedAndExcludedBinding_IsSelected()
    {
        using var fixture = new FixtureDirectory();
        var cases = SchematronCatalogReader.Read(fixture.Path, Rules);

        var selection = BindingFilter.Select(cases, Rules);

        await Assert.That(selection.Selected.Select(c => c.Id)).Contains("set-b/only-xslt2.xml");
    }

    [Test]
    public async Task Select_SelectedCase_KeepsOnlyTheSelectedBindings()
    {
        using var fixture = new FixtureDirectory();
        var cases = SchematronCatalogReader.Read(fixture.Path, Rules);

        var selection = BindingFilter.Select(cases, Rules);

        await Assert
            .That(selection.Selected.Single(c => c.Id == "set-b/only-xslt2.xml").Bindings)
            .IsEquivalentTo(["xslt-2"]);
        await Assert
            .That(selection.Selected.Single(c => c.Id == "set-a/one.xml").Bindings)
            .IsEquivalentTo(["xslt-1", "xslt-2"]);
    }

    [Test]
    public async Task Select_NoBindingSelected_ExcludesEveryCaseAndEveryExclusionHasAReason()
    {
        using var fixture = new FixtureDirectory();
        BindingRule[] rules =
        [
            new("xslt-1", "iso-schematron-xslt-1.xml", "not implemented"),
            new("xslt-2", "iso-schematron-xslt-2.xml", "not implemented either"),
            new("xslt-3", "iso-schematron-xslt-3.xml", "later"),
        ];
        var cases = SchematronCatalogReader.Read(fixture.Path, rules);

        var selection = BindingFilter.Select(cases, rules);

        await Assert.That(selection.Selected).IsEmpty();
        await Assert.That(selection.Excluded.Count).IsEqualTo(cases.Count);
        await Assert.That(selection.Excluded.All(e => !string.IsNullOrWhiteSpace(e.Reason))).IsTrue();
    }

    [Test]
    public async Task BindingRule_BlankExclusionReason_Throws() =>
        await Assert
            .That(() => new BindingRule("xslt-1", "iso-schematron-xslt-1.xml", " "))
            .Throws<ArgumentException>();
}
