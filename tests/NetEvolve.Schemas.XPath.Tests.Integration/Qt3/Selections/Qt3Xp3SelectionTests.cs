namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Selections;

using System.Security;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;
using static NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner.Qt3Fixtures;

public class Qt3Xp3SelectionTests
{
    /// <summary>The cases the XPath 3.0 selection keeps; a change of the rules or of the pinned suite makes it red.</summary>
    private const int SelectedXp30Cases = 19270;

    /// <summary>The cases the XPath 3.1 selection keeps; a change of the rules or of the pinned suite makes it red.</summary>
    private const int SelectedXp31Cases = 20971;

    private const int TotalCases = 31821;

    private const string PinnedSuiteChecksum = "ba9032e4fa5b71da574e3eab7ff16320b7294f6ba586a5cd060430c55974cf29";

    private static readonly string[] DeferredFeatureIds =
    [
        "DEF-40",
        "DEF-41",
        "DEF-42",
        "DEF-43",
        "DEF-44",
        "DEF-45",
        "DEF-46",
        "DEF-47",
        "DEF-48",
        "DEF-49",
        "DEF-68",
        "DEF-69",
        "DEF-73",
        "DEF-74",
        "DEF-76",
    ];

    private static Qt3Selection Select(Qt3SpecLevel level, string setDependencies, params string[] cases) =>
        SelectNamed(level, "set", setDependencies + string.Concat(cases));

    private static Qt3Selection SelectNamed(Qt3SpecLevel level, string testSet, string body)
    {
        using var temp = new TempDirectory();
        var root = WriteSuite(temp, (testSet, body));
        return Qt3Xp3Selector.Select(Qt3CatalogReader.Read(root), level);
    }

    private static string QueryCase(string name, string query, string assertion = "<assert-true/>") =>
        $"<test-case name=\"{name}\"><test>{SecurityElement.Escape(query)}</test><result>{assertion}</result></test-case>";

    [Test]
    [Arguments(Qt3SpecLevel.XPath30, "XP10+", true)]
    [Arguments(Qt3SpecLevel.XPath30, "XP20+", true)]
    [Arguments(Qt3SpecLevel.XPath30, "XP30", true)]
    [Arguments(Qt3SpecLevel.XPath30, "XP30+", true)]
    [Arguments(Qt3SpecLevel.XPath30, "XQ30 XP30", true)]
    [Arguments(Qt3SpecLevel.XPath30, "XP20", false)]
    [Arguments(Qt3SpecLevel.XPath30, "XP31", false)]
    [Arguments(Qt3SpecLevel.XPath30, "XP31+", false)]
    [Arguments(Qt3SpecLevel.XPath30, "XP31+ XQ31+", false)]
    [Arguments(Qt3SpecLevel.XPath30, "XQ10+", false)]
    [Arguments(Qt3SpecLevel.XPath30, "XQ30+", false)]
    [Arguments(Qt3SpecLevel.XPath30, "XT30+", false)]
    [Arguments(Qt3SpecLevel.XPath31, "XP10+", true)]
    [Arguments(Qt3SpecLevel.XPath31, "XP20+", true)]
    [Arguments(Qt3SpecLevel.XPath31, "XP30+", true)]
    [Arguments(Qt3SpecLevel.XPath31, "XP31", true)]
    [Arguments(Qt3SpecLevel.XPath31, "XP31+", true)]
    [Arguments(Qt3SpecLevel.XPath31, "XP31+ XQ31+", true)]
    [Arguments(Qt3SpecLevel.XPath31, "XP20", false)]
    [Arguments(Qt3SpecLevel.XPath31, "XP30", false)]
    [Arguments(Qt3SpecLevel.XPath31, "XP20 XP30 XQ10 XQ30", false)]
    [Arguments(Qt3SpecLevel.XPath31, "XQ31+", false)]
    [Arguments(Qt3SpecLevel.XPath31, "XT30+", false)]
    public async Task Select_SpecToken_MatchesTheXPathVersionOfTheLevel(Qt3SpecLevel level, string spec, bool selected)
    {
        var selection = Select(level, "", TestCase("c1", Dependency("spec", spec)));

        await Assert.That(selection.Selected.Count).IsEqualTo(selected ? 1 : 0);
        await Assert.That(selection.Excluded.Count).IsEqualTo(selected ? 0 : 1);
    }

    [Test]
    public async Task Select_SpecWithSatisfiedFalse_IsNegated()
    {
        var selection = Select(Qt3SpecLevel.XPath30, "", TestCase("c1", Dependency("spec", "XP30+", "false")));

        await Assert.That(selection.Selected).IsEmpty();
    }

    [Test]
    public async Task Select_CaseWithoutDependencies_IsSelectedAtBothLevels()
    {
        var xp30 = Select(Qt3SpecLevel.XPath30, "", TestCase("c1"));
        var xp31 = Select(Qt3SpecLevel.XPath31, "", TestCase("c1"));

        await Assert.That(xp30.Selected[0].Id).IsEqualTo("set/c1");
        await Assert.That(xp31.Selected[0].Id).IsEqualTo("set/c1");
    }

    [Test]
    public async Task Select_XQueryOnlyCase_IsExcludedWithTheReasonThatItAppliesToXQueryOnly()
    {
        var selection = Select(Qt3SpecLevel.XPath31, "", TestCase("c1", Dependency("spec", "XQ31+")));

        await Assert.That(selection.Excluded[0].Reason).StartsWith("spec XQ31+");
        await Assert.That(selection.Excluded[0].Reason).Contains("XQuery");
    }

    [Test]
    public async Task Select_SetAndCaseDependencies_AreAnded()
    {
        var caseFails = Select(
            Qt3SpecLevel.XPath30,
            Dependency("spec", "XP30+"),
            TestCase("ok", Dependency("spec", "XP30+")),
            TestCase("case-fails", Dependency("spec", "XQ30+"))
        );
        var setFails = Select(
            Qt3SpecLevel.XPath30,
            Dependency("spec", "XQ30+"),
            TestCase("set-fails", Dependency("spec", "XP30+"))
        );

        await Assert.That(caseFails.Selected.Count).IsEqualTo(1);
        await Assert.That(caseFails.Excluded[0].CaseId).IsEqualTo("set/case-fails");
        await Assert.That(setFails.Selected).IsEmpty();
        await Assert.That(setFails.Excluded[0].CaseId).IsEqualTo("set/set-fails");
    }

    [Test]
    [Arguments("schemaImport", "DEF-40 (#52)")]
    [Arguments("schemaValidation", "DEF-40 (#52)")]
    [Arguments("staticTyping", "DEF-41 (#53)")]
    [Arguments("namespace-axis", "DEF-42 (#54)")]
    [Arguments("simple-uca-fallback", "DEF-43 (#55)")]
    [Arguments("advanced-uca-fallback", "DEF-43 (#55)")]
    [Arguments("serialization", "DEF-46 (#58)")]
    [Arguments("fn-transform-XSLT", "DEF-47 (#59)")]
    [Arguments("fn-transform-XSLT30", "DEF-47 (#59)")]
    [Arguments("fn-load-xquery-module", "DEF-48 (#60)")]
    [Arguments("non_empty_sequence_collection", "DEF-49 (#61)")]
    public async Task Select_ExcludedFeature_IsExcludedWithReasonNamingTheFeatureAndTheDeferredFeature(
        string feature,
        string deferred
    )
    {
        foreach (var level in new[] { Qt3SpecLevel.XPath30, Qt3SpecLevel.XPath31 })
        {
            var selection = Select(level, "", TestCase("c1", Dependency("feature", feature)));

            await Assert.That(selection.Selected).IsEmpty();
            await Assert.That(selection.Excluded[0].Reason).StartsWith("feature " + feature);
            await Assert.That(selection.Excluded[0].Reason).Contains(deferred);
        }
    }

    [Test]
    public async Task Select_ExcludedFeatureOnTheTestSet_ExcludesEveryCaseOfTheSet()
    {
        var selection = Select(
            Qt3SpecLevel.XPath31,
            Dependency("feature", "schemaImport"),
            TestCase("a"),
            TestCase("b")
        );

        await Assert.That(selection.Selected).IsEmpty();
        await Assert.That(selection.Excluded.Count).IsEqualTo(2);
    }

    [Test]
    [Arguments("schemaImport")]
    [Arguments("serialization")]
    [Arguments("fn-load-xquery-module")]
    [Arguments("collection-stability")]
    public async Task Select_ExcludedFeatureWithSatisfiedFalse_IsSelected(string feature)
    {
        var selection = Select(Qt3SpecLevel.XPath31, "", TestCase("c1", Dependency("feature", feature, "false")));

        await Assert.That(selection.Selected.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments("higherOrderFunctions")]
    [Arguments("infoset-dtd")]
    [Arguments("xpath-1.0-compatibility")]
    [Arguments("olson-timezone")]
    public async Task Select_OtherFeature_IsSelected(string feature)
    {
        var selection = Select(Qt3SpecLevel.XPath31, "", TestCase("c1", Dependency("feature", feature)));

        await Assert.That(selection.Selected.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments("xml-version", "1.1", false)]
    [Arguments("xml-version", "1.0", true)]
    [Arguments("xml-version", "1.0:5+ 1.1", true)]
    [Arguments("xsd-version", "1.1", false)]
    [Arguments("xsd-version", "1.0", true)]
    [Arguments("feature", "non_unicode_codepoint_collation", false)]
    public async Task Select_VersionAndCollationDependencies_FollowTheXPath20Rules(
        string type,
        string value,
        bool selected
    )
    {
        var selection = Select(Qt3SpecLevel.XPath30, "", TestCase("c1", Dependency(type, value)));

        await Assert.That(selection.Selected.Count).IsEqualTo(selected ? 1 : 0);
        if (!selected)
        {
            await Assert.That(selection.Excluded[0].Reason).StartsWith(type + " " + value);
        }
    }

    [Test]
    [Arguments("fn-parse-json", "DEF-44 (#56)")]
    [Arguments("fn-json-doc", "DEF-44 (#56)")]
    [Arguments("fn-json-to-xml", "DEF-45 (#57)")]
    [Arguments("fn-xml-to-json", "DEF-45 (#57)")]
    [Arguments("fn-serialize", "DEF-46 (#58)")]
    [Arguments("misc-UCACollation", "DEF-43 (#55)")]
    [Arguments("fn-transform", "DEF-47 (#59)")]
    [Arguments("fn-load-xquery-module", "DEF-48 (#60)")]
    [Arguments("fn-collection", "DEF-49 (#61)")]
    [Arguments("fn-uri-collection", "DEF-49 (#61)")]
    [Arguments("fn-collation-key", "DEF-68 (#80)")]
    [Arguments("fn-default-language", "DEF-69 (#81)")]
    [Arguments("fn-parse-ietf-date", "DEF-73 (#85)")]
    [Arguments("fn-random-number-generator", "DEF-74 (#86)")]
    public async Task Select_ExcludedTestSet_ExcludesEveryCaseWithTheDeferredFeature(string testSet, string deferred)
    {
        var selection = SelectNamed(Qt3SpecLevel.XPath31, testSet, TestCase("a") + TestCase("b"));

        await Assert.That(selection.Selected).IsEmpty();
        await Assert.That(selection.Excluded.Count).IsEqualTo(2);
        await Assert.That(selection.Excluded[0].Reason).StartsWith("test set " + testSet);
        await Assert.That(selection.Excluded[0].Reason).Contains(deferred);
    }

    [Test]
    public async Task Select_SimilarTestSetName_IsSelected()
    {
        var selection = SelectNamed(Qt3SpecLevel.XPath31, "fn-parse-xml", TestCase("a"));

        await Assert.That(selection.Selected.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments("fn:parse-json('[]')", "DEF-44 (#56)")]
    [Arguments("fn:function-lookup(xs:QName('fn:json-doc'), 1)", "DEF-44 (#56)")]
    [Arguments("fn:json-to-xml('[]')", "DEF-45 (#57)")]
    [Arguments("fn:xml-to-json(.)", "DEF-45 (#57)")]
    [Arguments("fn:serialize(1)", "DEF-46 (#58)")]
    [Arguments("fn:compare('a', 'b', 'http://www.w3.org/2013/collation/UCA?lang=de')", "DEF-43 (#55)")]
    [Arguments("fn:transform(map{})", "DEF-47 (#59)")]
    [Arguments("fn:load-xquery-module('urn:x')", "DEF-48 (#60)")]
    [Arguments("fn:collection('urn:x')", "DEF-49 (#61)")]
    [Arguments("fn:uri-collection()", "DEF-49 (#61)")]
    [Arguments("fn:collation-key('a')", "DEF-68 (#80)")]
    [Arguments("fn:default-language()", "DEF-69 (#81)")]
    [Arguments("fn:parse-ietf-date('x')", "DEF-73 (#85)")]
    [Arguments("fn:random-number-generator()", "DEF-74 (#86)")]
    [Arguments(
        "fn:compare('a', 'A', 'http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive')",
        "DEF-76 (#88)"
    )]
    public async Task Select_CaseThatMentionsAnExcludedFunction_IsExcludedWithTheDeferredFeature(
        string query,
        string deferred
    )
    {
        var selection = Select(Qt3SpecLevel.XPath31, "", QueryCase("c1", query));

        await Assert.That(selection.Selected).IsEmpty();
        await Assert.That(selection.Excluded[0].Reason).Contains(deferred);
    }

    [Test]
    public async Task Select_CaseThatMentionsAnExcludedFunctionInTheExpectedResult_IsExcluded()
    {
        var selection = Select(
            Qt3SpecLevel.XPath31,
            "",
            "<test-case name=\"c1\"><test>1</test><result><assert-eq>fn:serialize(1)</assert-eq></result></test-case>"
        );

        await Assert.That(selection.Selected).IsEmpty();
        await Assert.That(selection.Excluded[0].Reason).Contains("DEF-46 (#58)");
    }

    [Test]
    public async Task Select_CaseThatMentionsAnExcludedFunctionOnlyInADescription_IsSelected()
    {
        var selection = Select(
            Qt3SpecLevel.XPath31,
            "",
            "<test-case name=\"c1\"><description>Uses fn:serialize.</description><test>1</test><result><assert-true/></result></test-case>"
        );

        await Assert.That(selection.Selected.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments("fn:parse-xml('<a/>')")]
    [Arguments("fn:default-collation()")]
    [Arguments("fn:doc-available('x')")]
    public async Task Select_CaseOfAFunctionThatLooksSimilar_IsSelected(string query)
    {
        var selection = Select(Qt3SpecLevel.XPath31, "", QueryCase("c1", query));

        await Assert.That(selection.Selected.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Exclusions_EveryEntry_HasAReasonAndAKey()
    {
        foreach (var entry in Qt3Xp3Exclusions.All)
        {
            await Assert.That(entry.Key).IsNotNullOrWhiteSpace();
            await Assert.That(entry.Reason).IsNotNullOrWhiteSpace();
            await Assert.That(entry.Describe()).StartsWith(entry.Reason);
        }
    }

    [Test]
    public async Task Exclusions_EveryDeferredFeatureOfTheIssue_IsTheReasonOfAnEntryWithItsIssueNumber()
    {
        var issues = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["DEF-40"] = 52,
            ["DEF-41"] = 53,
            ["DEF-42"] = 54,
            ["DEF-43"] = 55,
            ["DEF-44"] = 56,
            ["DEF-45"] = 57,
            ["DEF-46"] = 58,
            ["DEF-47"] = 59,
            ["DEF-48"] = 60,
            ["DEF-49"] = 61,
            ["DEF-68"] = 80,
            ["DEF-69"] = 81,
            ["DEF-73"] = 85,
            ["DEF-74"] = 86,
            ["DEF-76"] = 88,
        };
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in Qt3Xp3Exclusions.All)
        {
            if (entry.DefId is null)
            {
                continue;
            }

            _ = used.Add(entry.DefId);
            await Assert.That(DeferredFeatureIds).Contains(entry.DefId);
            await Assert.That(entry.Issue).IsEqualTo(issues[entry.DefId]);
        }

        await Assert.That(used).IsEquivalentTo(DeferredFeatureIds);
    }

    [Test]
    public async Task Exclusions_EntriesWithoutADeferredFeature_AreTheThreeRulesOfTheXPath20Selection()
    {
        var without = new List<string>();
        foreach (var entry in Qt3Xp3Exclusions.All)
        {
            if (entry.DefId is null)
            {
                without.Add(entry.Key);
            }
        }

        await Assert
            .That(without)
            .IsEquivalentTo(["xml-version 1.1", "xsd-version 1.1", "feature non_unicode_codepoint_collation"]);
    }

    [Test]
    public async Task Exclusions_Keys_AreUniquePerKind()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in Qt3Xp3Exclusions.All)
        {
            await Assert.That(keys.Add(entry.Kind + ":" + entry.Key)).IsTrue();
        }
    }

    [Test]
    public async Task Manifest_IsPinnedToTheSuiteTheCountsWereRecordedFor() =>
        await Assert.That(Qt3Suite.Manifest.Sha256).IsEqualTo(PinnedSuiteChecksum);

    [Test]
    [Arguments(Qt3SpecLevel.XPath30, SelectedXp30Cases)]
    [Arguments(Qt3SpecLevel.XPath31, SelectedXp31Cases)]
    public async Task Selection_Count_IsFixedForThePinnedSuite(
        Qt3SpecLevel level,
        int selected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Xp3Selections.GetSelectionAsync(level, cancellationToken);

        await Assert.That(selection.Selected.Count).IsEqualTo(selected);
        await Assert.That(selection.Selected.Count + selection.Excluded.Count).IsEqualTo(TotalCases);
    }

    [Test]
    [Arguments(Qt3SpecLevel.XPath30)]
    [Arguments(Qt3SpecLevel.XPath31)]
    public async Task Selection_CountPerTestSet_EqualsTheRecordedCount(
        Qt3SpecLevel level,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Xp3Selections.GetSelectionAsync(level, cancellationToken);

        var actual = Qt3Xp3Selections.FormatCounts(selection);

        await Assert
            .That(actual)
            .IsEqualTo(await File.ReadAllTextAsync(Qt3Xp3Selections.CountsFileOf(level), cancellationToken))
            .Because("the recorded per test set counts of " + level);
    }

    [Test]
    [Arguments(Qt3SpecLevel.XPath30)]
    [Arguments(Qt3SpecLevel.XPath31)]
    public async Task Selection_NoSelectedCase_DeclaresADependencyOfTheExclusionList(
        Qt3SpecLevel level,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Xp3Selections.GetSelectionAsync(level, cancellationToken);
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var testCase in selection.Selected)
        {
            _ = selected.Add(testCase.Id);
        }
        var offending = new List<string>();
        foreach (var testSet in Qt3CatalogReader.Read(await Qt3Suite.GetRootAsync(cancellationToken)))
        {
            foreach (var testCase in testSet.Cases)
            {
                if (!selected.Contains(testCase.Id))
                {
                    continue;
                }

                foreach (var dependency in testSet.Dependencies.Concat(testCase.Dependencies))
                {
                    foreach (var entry in Qt3Xp3Exclusions.All)
                    {
                        if (entry.Kind == Qt3ExclusionKind.Dependency && Qt3Xp3Exclusions.Matches(entry, dependency))
                        {
                            offending.Add(testCase.Id + ": " + entry.Key);
                        }
                    }
                }
            }
        }

        await Assert.That(offending).IsEmpty();
    }

    [Test]
    [Arguments(Qt3SpecLevel.XPath30)]
    [Arguments(Qt3SpecLevel.XPath31)]
    public async Task Selection_EveryExclusion_CarriesAReason(Qt3SpecLevel level, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Xp3Selections.GetSelectionAsync(level, cancellationToken);
        var blank = new List<string>();
        foreach (var exclusion in selection.Excluded)
        {
            if (string.IsNullOrWhiteSpace(exclusion.Reason))
            {
                blank.Add(exclusion.CaseId);
            }
        }

        await Assert.That(blank).IsEmpty();
    }

    [Test]
    public async Task Selection_Xp31_ExcludesTheJsonAndSerializationTestSets(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await Qt3Xp3Selections.GetSelectionAsync(Qt3SpecLevel.XPath31, cancellationToken);

        await Assert.That(selection.SelectedTestSets).DoesNotContain("fn-parse-json");
        await Assert.That(selection.SelectedTestSets).DoesNotContain("fn-json-doc");
        await Assert.That(selection.SelectedTestSets).DoesNotContain("fn-json-to-xml");
        await Assert.That(selection.SelectedTestSets).DoesNotContain("fn-xml-to-json");
        await Assert.That(selection.SelectedTestSets).DoesNotContain("fn-serialize");
    }

    [Test]
    public async Task Selection_Xp20_CountIsUnchanged(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var selection = await Qt3Suite.GetSelectionAsync(cancellationToken);

        await Assert.That(selection.Selected.Count).IsEqualTo(15288);
    }
}
