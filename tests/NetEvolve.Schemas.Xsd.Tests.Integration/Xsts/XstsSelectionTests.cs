namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

public class XstsSelectionTests
{
    private static XstsSelection Select(
        FixtureDirectory fixture,
        string version,
        IReadOnlySet<string>? statuses = null
    ) =>
        XstsSelector.Select(
            XstsReader.Read(fixture.Path),
            new XstsFilter(version, statuses ?? XstsFilter.AcceptedStatuses)
        );

    private static string[] Ids(XstsSelection selection) => [.. selection.Selected.Select(c => $"{c.TestSet}:{c.Id}")];

    private static readonly string[] Selected10 =
    [
        "metaA-alpha:1.0/defaulted/inst",
        "metaA-alpha:1.0/differs/inst",
        "metaA-alpha:1.0/differs/schema",
        "metaA-alpha:1.0/multi/schema",
        "metaA-alpha:1.0/only10/schema",
        "metaA-alpha:1.0/plain/inst-invalid",
        "metaA-alpha:1.0/plain/inst-valid",
        "metaA-alpha:1.0/plain/schema",
        "metaA-alpha:1.0/schemaless/inst",
        "metaA-alpha:1.0/statuses/accepted",
        "metaA-alpha:1.0/statuses/stable",
        "metaA-alpha:1.0/undetermined/only-other-version",
        "metaB-alpha:1.0/plain/schema",
    ];

    private static readonly string[] Selected11 =
    [
        "metaA-alpha:1.1/defaulted/inst",
        "metaA-alpha:1.1/differs/inst",
        "metaA-alpha:1.1/differs/schema",
        "metaA-alpha:1.1/multi/schema",
        "metaA-alpha:1.1/only11/schema",
        "metaA-alpha:1.1/plain/inst-invalid",
        "metaA-alpha:1.1/plain/inst-valid",
        "metaA-alpha:1.1/plain/schema",
        "metaA-alpha:1.1/schemaless/inst",
        "metaA-alpha:1.1/statuses/accepted",
        "metaA-alpha:1.1/statuses/stable",
        "metaB-alpha:1.1/plain/schema",
        "metaB-beta:1.1/both/schema",
        "metaB-beta:1.1/narrowed/inst11",
    ];

    [Test]
    public async Task VersionFilter_Version10_SelectsFixtureCasesForV10Only()
    {
        using var fixture = new FixtureDirectory();

        var selection = Select(fixture, XstsVersions.V10);

        await Assert.That(Ids(selection)).IsEquivalentTo(Selected10);
    }

    [Test]
    public async Task VersionFilter_Version11_SelectsFixtureCasesForV11Only()
    {
        using var fixture = new FixtureDirectory();

        var selection = Select(fixture, XstsVersions.V11);

        await Assert.That(Ids(selection)).IsEquivalentTo(Selected11);
    }

    [Test]
    public async Task VersionFilter_BothVersions_SelectsBothSelections()
    {
        using var fixture = new FixtureDirectory();

        var both = Select(fixture, XstsVersions.V10)
            .Selected.Concat(Select(fixture, XstsVersions.V11).Selected)
            .Select(c => $"{c.TestSet}:{c.Id}");

        await Assert.That(both).IsEquivalentTo([.. Selected10, .. Selected11]);
    }

    [Test]
    public async Task VersionFilter_ParentAndChildScopesMustAllAdmitTheVersion()
    {
        using var fixture = new FixtureDirectory();

        var v10 = Ids(Select(fixture, XstsVersions.V10));
        var v11 = Ids(Select(fixture, XstsVersions.V11));

        // Set version="1.1" with group version="1.0 1.1": the set already excludes 1.0.
        await Assert.That(v10.Any(i => i.StartsWith("metaB-beta:", StringComparison.Ordinal))).IsFalse();
        // Group version="1.0 1.1" with test version="1.0" selects the test for 1.0 only; the set excludes 1.0.
        await Assert.That(v11).DoesNotContain("metaB-beta:1.1/narrowed/inst10");
    }

    [Test]
    public async Task StatusFilter_ExcludesNotAcceptedCases_EachWithAReason()
    {
        using var fixture = new FixtureDirectory();

        var selection = Select(fixture, XstsVersions.V10);

        var statuses = selection
            .Excluded.Where(e => e.Id.StartsWith("1.0/statuses/", StringComparison.Ordinal))
            .ToDictionary(e => e.Id, e => e.Reason, StringComparer.Ordinal);
        await Assert
            .That(statuses.Keys)
            .IsEquivalentTo([
                "1.0/statuses/queried",
                "1.0/statuses/disputed-test",
                "1.0/statuses/disputed-spec",
                "1.0/statuses/submitted",
                "1.0/statuses/no-status",
            ]);
        await Assert.That(statuses["1.0/statuses/queried"]).Contains("queried");
        await Assert.That(statuses["1.0/statuses/no-status"]).Contains("no current status");
        await Assert.That(selection.Excluded.All(e => !string.IsNullOrWhiteSpace(e.Reason))).IsTrue();
    }

    [Test]
    public async Task StatusFilter_StatusSetIsData()
    {
        using var fixture = new FixtureDirectory();

        var selection = Select(fixture, XstsVersions.V10, new HashSet<string>(["queried"], StringComparer.Ordinal));

        await Assert.That(Ids(selection)).IsEquivalentTo(["metaA-alpha:1.0/statuses/queried"]);
    }

    [Test]
    public async Task Selection_DifferentExpectedValidityPerVersion_YieldsTheRightExpectationPerVersion()
    {
        using var fixture = new FixtureDirectory();

        var v10 = Select(fixture, XstsVersions.V10).Selected;
        var v11 = Select(fixture, XstsVersions.V11).Selected;

        await Assert
            .That(v10.Single(c => string.Equals(c.Id, "1.0/differs/inst", StringComparison.Ordinal)).ExpectValid)
            .IsTrue();
        await Assert
            .That(v11.Single(c => string.Equals(c.Id, "1.1/differs/inst", StringComparison.Ordinal)).ExpectValid)
            .IsFalse();
        // A version-specific expected element wins over the one without a version.
        await Assert
            .That(v10.Single(c => string.Equals(c.Id, "1.0/defaulted/inst", StringComparison.Ordinal)).ExpectValid)
            .IsFalse();
        await Assert
            .That(v11.Single(c => string.Equals(c.Id, "1.1/defaulted/inst", StringComparison.Ordinal)).ExpectValid)
            .IsTrue();
    }

    [Test]
    public async Task Selection_CasesWithoutADefiniteVersionExpectation_AreExcludedWithAReason()
    {
        using var fixture = new FixtureDirectory();

        var selection = Select(fixture, XstsVersions.V11);

        var reasons = selection.Excluded.ToDictionary(e => e.Id, e => e.Reason, StringComparer.Ordinal);
        await Assert.That(reasons["1.1/undetermined/indeterminate"]).Contains("indeterminate");
        await Assert.That(reasons["1.1/undetermined/feature-expected"]).Contains("Unicode_4.0.0");
        await Assert.That(reasons["1.1/undetermined/only-other-version"]).Contains("no expected result");
        await Assert.That(reasons["1.1/feature-gated/schema"]).Contains("full-xpath-in-CTA");
    }

    [Test]
    public async Task Selection_SetsOnlyListedInExtraSuite_AreExcludedForPrecisionDecimal()
    {
        using var fixture = new FixtureDirectory();

        var selection = Select(fixture, XstsVersions.V11);

        var excluded = selection.Excluded.Single(e =>
            string.Equals(e.TestSet, "metaC-decimal", StringComparison.Ordinal)
        );
        await Assert.That(excluded.Id).IsEqualTo("1.1/pd/schema");
        await Assert.That(excluded.Reason).Contains("precisionDecimal");
        await Assert.That(excluded.Reason).Contains("DEF-52");
        await Assert.That(Ids(selection).Any(i => i.StartsWith("metaC-decimal:", StringComparison.Ordinal))).IsFalse();
    }

    [Test]
    public async Task Reader_TestSetKeys_AreUniqueAcrossDirectories()
    {
        using var fixture = new FixtureDirectory();

        var keys = XstsReader.Read(fixture.Path).Select(c => c.TestSet).Distinct(StringComparer.Ordinal);

        await Assert.That(keys).IsEquivalentTo(["metaA-alpha", "metaB-alpha", "metaB-beta", "metaC-decimal"]);
    }

    [Test]
    public async Task Reader_SchemaDocuments_AreRelativeToTheSuiteRoot()
    {
        using var fixture = new FixtureDirectory();

        var cases = XstsReader.Read(fixture.Path);

        await Assert
            .That(cases.Single(c => string.Equals(c.Group, "multi", StringComparison.Ordinal)).SchemaDocuments)
            .IsEquivalentTo(["data/a.xsd", "data/sub/b.xsd"]);
        await Assert
            .That(
                cases
                    .Single(c =>
                        string.Equals(c.Group, "plain", StringComparison.Ordinal)
                        && string.Equals(c.Name, "inst-valid", StringComparison.Ordinal)
                        && string.Equals(c.TestSet, "metaA-alpha", StringComparison.Ordinal)
                    )
                    .InstanceDocument
            )
            .IsEqualTo("data/valid.xml");
    }

    [Test]
    public async Task Reader_InstanceTestsOfAGroup_UseTheSchemaDocumentsOfTheGroup()
    {
        using var fixture = new FixtureDirectory();

        var cases = XstsReader.Read(fixture.Path);

        await Assert
            .That(
                cases
                    .Single(c =>
                        string.Equals(c.Group, "plain", StringComparison.Ordinal)
                        && string.Equals(c.Name, "inst-valid", StringComparison.Ordinal)
                        && string.Equals(c.TestSet, "metaA-alpha", StringComparison.Ordinal)
                    )
                    .SchemaDocuments
            )
            .IsEquivalentTo(["data/plain.xsd"]);
        await Assert
            .That(cases.Single(c => string.Equals(c.Group, "schemaless", StringComparison.Ordinal)).SchemaDocuments)
            .IsEmpty();
    }
}
