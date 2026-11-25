namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using System.Globalization;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Testing.Readme;
using NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Validating;

/// <summary>Pins the conformance section of src/NetEvolve.Schemas.Xml/README.md to the run of the reader over the pinned suite.</summary>
[Explicit]
[Category(ConformanceCategory.Name)]
public class XmlConfReadmeTests
{
    private const string Readme = "src/NetEvolve.Schemas.Xml/README.md";

    /// <summary>The cases the filter drops, by the start of the reason of the filter, with the label and the reason of the readme.</summary>
    private static readonly (string FilterReason, string Label, string Reason)[] ExcludedClasses =
    [
        ("XML 1.1 case;", "XML 1.1", "XML 1.1 is not supported."),
        ("Namespaces in XML 1.1 case;", "Namespaces in XML 1.1", "XML 1.1 is not supported."),
        (
            "Applies only to XML 1.0 editions one to four;",
            "XML 1.0 editions one to four only",
            "The reader follows the name rules of the Fifth Edition."
        ),
    ];

    /// <summary>The cases that fail, by class: the label and the reason of the readme and the case ids.</summary>
    private static readonly (string Label, string Reason, string[] Ids)[] KnownFailures =
    [
        (
            "Namespace processing off",
            "The case needs namespace processing switched off; the reader has no such mode.",
            [
                "eduni-errata-4e/x-ibm-1-0.5-valid-P04-ibm04v01.xml",
                "eduni-errata-4e/x-ibm-1-0.5-valid-P05-ibm05v01.xml",
                "eduni-errata-4e/x-ibm-1-0.5-valid-P05-ibm05v02.xml",
                "eduni-errata-4e/x-ibm-1-0.5-valid-P05-ibm05v03.xml",
                "eduni-errata-4e/x-ibm-1-0.5-valid-P05-ibm05v05.xml",
                "oasis/o-p04pass1",
                "oasis/o-p05pass1",
                "oasis/o-p08pass1",
                "xmltest/valid-sa-012",
            ]
        ),
        (
            "Document missing from the archive",
            "The case names a document that the pinned archive does not contain.",
            [
                "eduni-namespaces-misc/hst-bh-001",
                "eduni-namespaces-misc/hst-bh-002",
                "eduni-namespaces-misc/hst-bh-003",
                "eduni-namespaces-misc/hst-bh-004",
                "eduni-namespaces-misc/hst-bh-005",
                "eduni-namespaces-misc/hst-bh-006",
                "eduni-namespaces-misc/hst-lhs-007",
                "eduni-namespaces-misc/hst-lhs-008",
                "eduni-namespaces-misc/hst-lhs-009",
            ]
        ),
        (
            "Name above the default limit",
            "The document has a name longer than the default `MaxNameLength` and is rejected with XML1105.",
            ["ibm/ibm-valid-P85-ibm85v01.xml", "ibm/ibm-valid-P87-ibm87v01.xml"]
        ),
        (
            "Version other than 1.0",
            "The document declares version 1.7, which the Fifth Edition allows; the reader accepts 1.0 only and reports XML0010.",
            ["eduni-errata-4e/x-rmt-008b"]
        ),
        (
            "Entity used before its declaration",
            "A default value refers to an entity that is declared later and is accepted ([#878](https://github.com/dailydevops/schemas/issues/878)).",
            ["ibm/ibm-not-wf-P68-ibm68n07.xml", "xmltest/not-wf-sa-180"]
        ),
    ];

    /// <summary>The cases the validation run does not run, by class: the label and the reason of the readme and the case ids.</summary>
    private static readonly (string Label, string Reason, string[] Ids)[] NotRun =
    [
        (
            "Namespace processing off",
            "The case needs namespace processing switched off; the reader has no such mode.",
            [
                "eduni-errata-4e/x-ibm-1-0.5-valid-P04-ibm04v01.xml",
                "eduni-errata-4e/x-ibm-1-0.5-valid-P05-ibm05v01.xml",
                "eduni-errata-4e/x-ibm-1-0.5-valid-P05-ibm05v02.xml",
                "eduni-errata-4e/x-ibm-1-0.5-valid-P05-ibm05v03.xml",
                "eduni-errata-4e/x-ibm-1-0.5-valid-P05-ibm05v05.xml",
                "oasis/o-p04pass1",
                "oasis/o-p05pass1",
                "oasis/o-p08pass1",
                "xmltest/valid-sa-012",
            ]
        ),
        (
            "Document missing from the archive",
            "The case names a document that the pinned archive does not contain.",
            ["eduni-namespaces-misc/hst-bh-005", "eduni-namespaces-misc/hst-bh-006"]
        ),
        (
            "Name above the default limit",
            "The document has a name longer than the default `MaxNameLength` and is rejected with XML1105.",
            ["ibm/ibm-valid-P85-ibm85v01.xml", "ibm/ibm-valid-P87-ibm87v01.xml"]
        ),
        (
            "Version other than 1.0",
            "The document declares version 1.7, which the Fifth Edition allows; the reader accepts 1.0 only and reports XML0010.",
            ["eduni-errata-4e/x-rmt-008b"]
        ),
    ];

    /// <summary>The cases the validation run fails, by class: the label and the reason of the readme and the case ids.</summary>
    private static readonly (string Label, string Reason, string[] Ids)[] ValidationFailures =
    [
        (
            "Parse-time anomaly not recorded",
            "The declaration parser records no parse-time anomaly ([#608](https://github.com/dailydevops/schemas/issues/608)), so the nesting and uniqueness constraints are not reported.",
            [
                "eduni-errata-2e/rmt-e2e-14",
                "ibm/ibm-invalid-P45-ibm45i01.xml",
                "ibm/ibm-invalid-P49-ibm49i01.xml",
                "ibm/ibm-invalid-P50-ibm50i01.xml",
                "ibm/ibm-invalid-P51-ibm51i01.xml",
                "sun/el04",
                "xmltest/invalid--002",
                "xmltest/invalid--005",
                "xmltest/invalid--006",
                "xmltest/invalid-not-sa-022",
            ]
        ),
        (
            "Skipped entity reference",
            "The reader has no event for an entity reference ([#647](https://github.com/dailydevops/schemas/issues/647)), so the constraint is not reported.",
            ["eduni-errata-2e/rmt-e2e-15a", "eduni-errata-3e/rmt-e3e-13"]
        ),
        (
            "Colon in an ID value",
            "ID values are checked against Name only ([#925](https://github.com/dailydevops/schemas/issues/925)): a colon is accepted.",
            ["eduni-namespaces-1.0/rmt-ns10-045", "eduni-namespaces-1.0/rmt-ns10-046"]
        ),
        (
            "Standalone normalization clause",
            "The clause skips enumerated and notation attributes ([#924](https://github.com/dailydevops/schemas/issues/924)), so Standalone Document Declaration is not reported.",
            ["sun/inv-not-sa05", "sun/inv-not-sa06"]
        ),
    ];

    [Test]
    public async Task Readme_StatesTheRateOfTheReaderRunAndTheExcludedClasses(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var results = await XmlConfRunner.RunAsync(
            selection,
            new PipelineXmlConfReader(XmlConfSuite.SuiteRoot),
            cancellationToken
        );
        var readme = ReadmeSamples.ReadText(Readme);

        var passed = results.Count(r => r.Outcome == CaseOutcome.Pass);
        var failed = results.Count(r => r.Outcome == CaseOutcome.Fail);
        var excluded = results.Count(r => r.Outcome == CaseOutcome.Skipped);
        var applicable = passed + failed;
        // Truncated to one decimal, as the conformance summary does, so that only a run without a failure shows 100.0%.
        var tenths = (long)passed * 1000 / applicable;
        var rate = FormattableString.Invariant($"{tenths / 10}.{tenths % 10}%");

        await Assert.That(readme).Contains("## Conformance");
        await Assert.That(readme).Contains($"The suite has {Number(applicable + excluded)} cases");
        await Assert.That(readme).Contains($"{Number(applicable)} of them apply");
        await Assert
            .That(readme)
            .Contains($"**Rate: {rate}** ({Number(passed)} of {Number(applicable)} applicable cases)");
        await Assert.That(readme).Contains($"{Number(excluded)} cases are excluded");

        foreach (var group in selection.Excluded.GroupBy(e => e.Reason, StringComparer.Ordinal))
        {
            var (_, label, reason) = ExcludedClasses.Single(c =>
                group.Key.StartsWith(c.FilterReason, StringComparison.Ordinal)
            );
            await Assert.That(readme).Contains($"| {label} | {Number(group.Count())} | {reason} |");
        }

        await Assert
            .That(selection.Excluded.Select(e => e.Reason).Distinct(StringComparer.Ordinal).Count())
            .IsEqualTo(ExcludedClasses.Length);
    }

    [Test]
    public async Task Readme_NamesEveryClassOfTheKnownFailures(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var baselined = XmlConfSuite
            .LoadBaselines(selection)
            .Values.SelectMany(b => b.Ids)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert
            .That(baselined)
            .IsEquivalentTo(KnownFailures.SelectMany(c => c.Ids).OrderBy(name => name, StringComparer.Ordinal));
        foreach (var (label, reason, ids) in KnownFailures)
        {
            await Assert.That(readme).Contains($"| {label} | {Number(ids.Length)} | {reason} |");
        }

        await Assert.That(readme).Contains($"{Number(baselined.Count)} cases fail");
    }

    [Test]
    public async Task Readme_StatesTheRateOfTheDtdValidationRunAndItsGaps(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XmlConfSuite.LoadSelectionAsync(cancellationToken);
        var results = await ValidatingXmlConfRunner.RunAsync(
            selection,
            XmlConfSuite.SuiteRoot,
            ValidatingXmlConfGaps.Excluded,
            cancellationToken
        );
        var readme = ReadmeSamples.ReadText(Readme);

        var passed = results.Count(r => r.Outcome == CaseOutcome.Pass);
        var failed = results.Count(r => r.Outcome == CaseOutcome.Fail);
        var notRun = results.Count(r => r.Outcome == CaseOutcome.Skipped);
        var applicable = passed + failed;
        var tenths = (long)passed * 1000 / applicable;
        var rate = FormattableString.Invariant($"{tenths / 10}.{tenths % 10}%");

        await Assert
            .That(readme)
            .Contains($"**Validation rate: {rate}** ({Number(passed)} of {Number(applicable)} applicable cases)");
        await Assert.That(readme).Contains($"the {Number(results.Count)} valid and invalid cases");
        await Assert.That(readme).Contains($"{Number(notRun)} cases are not run");
        await Assert.That(readme).Contains($"{Number(failed)} cases fail, all of them invalid documents");

        // The classes of the readme partition the two tables of the gaps.
        await Assert.That(NotRun.Sum(c => c.Ids.Length)).IsEqualTo(ValidatingXmlConfGaps.Excluded.Count);
        await Assert
            .That(NotRun.SelectMany(c => c.Ids).OrderBy(id => id, StringComparer.Ordinal))
            .IsEquivalentTo(ValidatingXmlConfGaps.Excluded.Keys.OrderBy(id => id, StringComparer.Ordinal));
        foreach (var (label, reason, ids) in NotRun)
        {
            await Assert.That(readme).Contains($"| {label} | {Number(ids.Length)} | {reason} |");
        }

        await Assert
            .That(ValidationFailures.SelectMany(c => c.Ids).OrderBy(id => id, StringComparer.Ordinal))
            .IsEquivalentTo(ValidatingXmlConfGaps.Known.Keys.OrderBy(id => id, StringComparer.Ordinal));
        foreach (var (label, reason, ids) in ValidationFailures)
        {
            await Assert.That(readme).Contains($"| {label} | {Number(ids.Length)} | {reason} |");
        }
    }

    private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
}
