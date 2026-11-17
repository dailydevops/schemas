namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

public class XmlConfFilterTests
{
    private static XmlConfSelection Select()
    {
        using var fixture = new FixtureDirectory();
        return XmlConfFilter.Select(XmlConfCatalogReader.Read(fixture.Path));
    }

    [Test]
    public async Task Select_Xml11Cases_AreExcludedWithAReason()
    {
        var selection = Select();

        var excluded = selection.Excluded.ToDictionary(e => e.Case.Id, e => e.Reason, StringComparer.Ordinal);
        await Assert.That(excluded["b/xml11-version"]).Contains("XML 1.1");
        await Assert.That(excluded["b/xml11-recommendation"]).Contains("XML 1.1");
        await Assert.That(excluded["b/ns11"]).Contains("Namespaces in XML 1.1");
    }

    [Test]
    public async Task Select_EditionsOneToFourOnly_AreExcludedWithAReason()
    {
        var selection = Select();

        var excluded = selection.Excluded.Single(e =>
            string.Equals(e.Case.Id, "b/edition-1-4", StringComparison.Ordinal)
        );
        await Assert.That(excluded.Reason).Contains("editions one to four");
    }

    [Test]
    public async Task Select_Exclusions_AreExactlyTheXml11AndOldEditionCases()
    {
        var selection = Select();

        await Assert
            .That(selection.Excluded.Select(e => e.Case.Id))
            .IsEquivalentTo(["b/xml11-version", "b/xml11-recommendation", "b/ns11", "b/edition-1-4"]);
        await Assert.That(selection.Excluded.All(e => !string.IsNullOrWhiteSpace(e.Reason))).IsTrue();
    }

    [Test]
    public async Task Select_FifthEditionAndSharedVersionCases_AreKept()
    {
        var selection = Select();

        await Assert
            .That(selection.Selected.Select(c => c.Id))
            .IsEquivalentTo([
                "a/valid-1",
                "a/notwf-1",
                "a/invalid-1",
                "a/error-1",
                "a/default-1",
                "b/edition-4-5",
                "b/both-versions",
                "b/no-namespaces",
            ]);
    }

    [Test]
    public async Task Select_EachSelectedCase_CarriesItsEntitiesTag()
    {
        var selection = Select();

        var tags = selection.Selected.ToDictionary(c => c.Id, c => c.Entities, StringComparer.Ordinal);
        await Assert.That(tags["a/valid-1"]).IsEqualTo(XmlConfEntities.None);
        await Assert.That(tags["a/notwf-1"]).IsEqualTo(XmlConfEntities.General);
        await Assert.That(tags["a/invalid-1"]).IsEqualTo(XmlConfEntities.Parameter);
        await Assert.That(tags["a/error-1"]).IsEqualTo(XmlConfEntities.Both);
        await Assert.That(tags["a/default-1"]).IsEqualTo(XmlConfEntities.None);
    }
}
