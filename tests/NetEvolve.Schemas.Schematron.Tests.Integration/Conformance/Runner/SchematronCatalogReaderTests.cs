namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

public class SchematronCatalogReaderTests
{
    private static readonly BindingRule[] Bindings =
    [
        new("xslt-1", "iso-schematron-xslt-1.xml"),
        new("xslt-2", "iso-schematron-xslt-2.xml"),
        new("xslt-3", "iso-schematron-xslt-3.xml"),
    ];

    [Test]
    public async Task Read_FixtureCase_ReportsSchemaInstanceBindingAndExpectedOutcome()
    {
        using var fixture = new FixtureDirectory();

        var cases = SchematronCatalogReader.Read(fixture.Path, Bindings);
        var one = cases.Single(c => string.Equals(c.Id, "set-a/one.xml", StringComparison.Ordinal));

        await Assert.That(one.TestSet).IsEqualTo("set-a");
        await Assert.That(one.Expect).IsEqualTo(SchematronOutcome.Invalid);
        await Assert.That(one.Phase).IsEqualTo("p1");
        await Assert.That(one.Schemas).HasSingleItem();
        await Assert.That(one.Schemas[0]).Contains("<sch:schema");
        await Assert.That(one.Instance).IsEqualTo("<root xmlns=\"\"><child /></root>");
        await Assert.That(one.Documents.Select(d => d.FileName)).IsEquivalentTo(["document.xml", "extra.xml"]);
        await Assert.That(one.Assertions).HasSingleItem();
        await Assert.That(one.Bindings).IsEquivalentTo(["xslt-1", "xslt-2", "xslt-3"]);
    }

    [Test]
    public async Task Read_CaseListedByNestedSuites_AppearsOnceWithEveryBinding()
    {
        using var fixture = new FixtureDirectory();

        var cases = SchematronCatalogReader.Read(fixture.Path, Bindings);

        await Assert
            .That(cases.Select(c => c.Id))
            .IsEquivalentTo(["set-a/one.xml", "set-a/two.xml", "set-b/only-xslt2.xml", "set-b/only-xslt3.xml"]);
        await Assert
            .That(cases.Single(c => string.Equals(c.Id, "set-b/only-xslt2.xml", StringComparison.Ordinal)).Bindings)
            .IsEquivalentTo(["xslt-2", "xslt-3"]);
        await Assert
            .That(cases.Single(c => string.Equals(c.Id, "set-b/only-xslt3.xml", StringComparison.Ordinal)).Bindings)
            .IsEquivalentTo(["xslt-3"]);
    }

    [Test]
    public async Task Read_Cases_AreSortedByIdOrdinal()
    {
        using var fixture = new FixtureDirectory();

        var ids = SchematronCatalogReader.Read(fixture.Path, Bindings).Select(c => c.Id).ToArray();

        await Assert.That(ids).IsEquivalentTo(ids.Order(StringComparer.Ordinal));
    }

    [Test]
    public async Task Read_CaseWithTwoSchemas_ListsBothSchemas(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(fixture.Path, "set-a", "two.xml"),
            FixtureSuite.Case("valid", "<root/>", schemaCount: 2),
            cancellationToken
        );

        var cases = SchematronCatalogReader.Read(fixture.Path, Bindings);

        await Assert
            .That(cases.Single(c => string.Equals(c.Id, "set-a/two.xml", StringComparison.Ordinal)).Schemas.Count)
            .IsEqualTo(2);
    }

    [Test]
    public async Task Read_UnknownExpectedOutcome_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(fixture.Path, "set-a", "two.xml"),
            FixtureSuite.Case("maybe", "<root xmlns=\"\"/>"),
            cancellationToken
        );

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() =>
            Task.FromResult(SchematronCatalogReader.Read(fixture.Path, Bindings))
        );

        await Assert.That(ex!.Message).Contains("set-a/two.xml");
    }

    [Test]
    public async Task Read_CaseAtSuiteRoot_ThrowsInvalidDataWithTheCaseId(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var fixture = new FixtureDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(fixture.Path, "iso-schematron-xslt-1.xml"),
            $"<testsuite xmlns=\"{FixtureSuite.Namespace}\"><testcase href=\"rootcase.xml\"/></testsuite>",
            cancellationToken
        );
        await File.WriteAllTextAsync(
            Path.Combine(fixture.Path, "rootcase.xml"),
            FixtureSuite.Case("valid", "<root xmlns=\"\"/>"),
            cancellationToken
        );

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() =>
            Task.FromResult(SchematronCatalogReader.Read(fixture.Path, Bindings))
        );

        await Assert.That(ex!.Message).Contains("rootcase.xml");
    }
}
