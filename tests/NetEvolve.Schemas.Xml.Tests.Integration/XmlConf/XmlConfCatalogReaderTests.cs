namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

public class XmlConfCatalogReaderTests
{
    [Test]
    public async Task Read_FixtureCatalog_ReturnsEveryCaseInCatalogOrder()
    {
        using var fixture = new FixtureDirectory();

        var cases = XmlConfCatalogReader.Read(fixture.Path);

        await Assert
            .That(cases.Select(c => c.Id))
            .IsEquivalentTo(
                [
                    "a/valid-1",
                    "a/notwf-1",
                    "a/invalid-1",
                    "a/error-1",
                    "a/default-1",
                    "b/xml11-version",
                    "b/xml11-recommendation",
                    "b/ns11",
                    "b/edition-1-4",
                    "b/edition-4-5",
                    "b/both-versions",
                    "b/no-namespaces",
                ]
            );
        await Assert.That(cases.Select(c => c.TestSet).Distinct()).IsEquivalentTo(["a", "b"]);
    }

    [Test]
    public async Task Read_Uri_IsResolvedAgainstTheXmlBaseOfTheTestSet()
    {
        using var fixture = new FixtureDirectory();

        var cases = XmlConfCatalogReader.Read(fixture.Path);

        await Assert.That(cases.Single(c => c.Id == "a/notwf-1").Uri).IsEqualTo("a/sub/notwf-1.xml");
        await Assert.That(cases.Single(c => c.Id == "b/edition-4-5").Uri).IsEqualTo("b/e45.xml");
    }

    [Test]
    [Arguments("a/valid-1", XmlConfType.Valid)]
    [Arguments("a/notwf-1", XmlConfType.NotWellFormed)]
    [Arguments("a/invalid-1", XmlConfType.Invalid)]
    [Arguments("a/error-1", XmlConfType.Error)]
    public async Task Read_Type_IsMapped(string id, XmlConfType expected)
    {
        using var fixture = new FixtureDirectory();

        var cases = XmlConfCatalogReader.Read(fixture.Path);

        await Assert.That(cases.Single(c => c.Id == id).Type).IsEqualTo(expected);
    }

    [Test]
    [Arguments("a/valid-1", XmlConfEntities.None)]
    [Arguments("a/notwf-1", XmlConfEntities.General)]
    [Arguments("a/invalid-1", XmlConfEntities.Parameter)]
    [Arguments("a/error-1", XmlConfEntities.Both)]
    [Arguments("a/default-1", XmlConfEntities.None)]
    public async Task Read_EntitiesTag_IsMappedAndDefaultsToNone(string id, XmlConfEntities expected)
    {
        using var fixture = new FixtureDirectory();

        var cases = XmlConfCatalogReader.Read(fixture.Path);

        await Assert.That(cases.Single(c => c.Id == id).Entities).IsEqualTo(expected);
    }

    [Test]
    public async Task Read_NamespaceFlag_DefaultsToTrueAndHonorsNo()
    {
        using var fixture = new FixtureDirectory();

        var cases = XmlConfCatalogReader.Read(fixture.Path);

        await Assert.That(cases.Single(c => c.Id == "a/default-1").Namespaces).IsTrue();
        await Assert.That(cases.Single(c => c.Id == "b/no-namespaces").Namespaces).IsFalse();
    }

    [Test]
    public async Task Read_VersionEditionAndRecommendation_AreReadWithDefaults()
    {
        using var fixture = new FixtureDirectory();

        var cases = XmlConfCatalogReader.Read(fixture.Path);

        var testCase = cases.Single(c => c.Id == "b/edition-1-4");
        await Assert.That(testCase.Versions).IsEquivalentTo(["1.0"]);
        await Assert.That(testCase.Editions).IsEquivalentTo(["1", "2", "3", "4"]);
        var plain = cases.Single(c => c.Id == "a/valid-1");
        await Assert.That(plain.Versions).IsEmpty();
        await Assert.That(plain.Editions).IsEmpty();
        await Assert.That(plain.Recommendation).IsEqualTo("XML1.0");
    }

    [Test]
    public async Task Read_Description_IsTheTextOfTheTestElement()
    {
        using var fixture = new FixtureDirectory();

        var cases = XmlConfCatalogReader.Read(fixture.Path);

        await Assert.That(cases.Single(c => c.Id == "a/notwf-1").Description).IsEqualTo("A not-wf case with markup.");
    }

    [Test]
    public async Task Read_UnknownType_Throws()
    {
        using var fixture = new FixtureDirectory();
        FixtureSuite.Write(
            fixture.Path,
            "b/b.xml",
            """<TEST ID="odd" TYPE="surprising" URI="x.xml" SECTIONS="1">Odd.</TEST>"""
        );

        _ = await Assert.That(() => XmlConfCatalogReader.Read(fixture.Path)).Throws<InvalidDataException>();
    }

    [Test]
    public async Task Read_DuplicateId_Throws()
    {
        using var fixture = new FixtureDirectory();
        FixtureSuite.Write(
            fixture.Path,
            "b/b.xml",
            """
            <TEST ID="twice" TYPE="valid" URI="x.xml" SECTIONS="1">One.</TEST>
            <TEST ID="twice" TYPE="valid" URI="y.xml" SECTIONS="1">Two.</TEST>
            """
        );

        _ = await Assert.That(() => XmlConfCatalogReader.Read(fixture.Path)).Throws<InvalidDataException>();
    }
}
