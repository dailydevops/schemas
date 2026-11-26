namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.XRechnung;

using System.Text;
using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Conformance.Results;

/// <summary>
/// Acceptance of the scenario chain of the pinned KoSIT validator configuration on the pinned XRechnung test suite: each instance is
/// matched to its scenario and runs through the XML Schema, the CEN rules, and the XRechnung rules. The archives are fetched by their
/// pinned checksums and never committed.
/// </summary>
/// <remarks>
/// Applicable cases: every instance of the test suite, so the pass rate is 100 % of all instances and no case is excluded. An instance
/// that fails because a rule set does not build is a known failure; every other failure is a regression.
/// </remarks>
[Explicit("Fetches the pinned KoSIT archives; run through the conformance category.")]
[Category(ConformanceCategory.Name)]
public class KositScenarioGoldenTests
{
    [Test]
    public async Task Instances_RunThroughTheirScenario_AreValidOrAKnownBuildBlockedFailure(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var results = await KositScenarioRun.GetResultsAsync(cancellationToken);
        var known = results
            .Where(r =>
                r.Outcome == CaseOutcome.Fail
                && r.Reason!.Contains(XRechnungRun.BuildBlockedPrefix, StringComparison.Ordinal)
            )
            .Select(r => r.CaseId);

        var report = ConformanceReport.Compare(
            results,
            new Dictionary<string, KnownFailureBaseline>(StringComparer.Ordinal)
            {
                [KositScenarioRun.TestSet] = KnownFailureBaseline.Parse(KnownFailureBaseline.Format(known)),
            }
        );
        ConformanceRateReporter.Collect(
            KositScenarioRun.RatePackage,
            TargetFrameworkName.Of(typeof(KositScenarioGoldenTests).Assembly),
            report
        );

        await Assert.That(results).IsNotEmpty();
        await Assert.That(report.IsSuccess).IsTrue().Because(Format(report.TestSets[0], results));
    }

    [Test]
    public async Task Corpus_EveryInstanceOfTheSuite_IsOneCase(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (_, suite) = await XRechnungRun.GetRootsAsync(cancellationToken);
        var instances = XRechnungRun.Syntaxes.SelectMany(s => XRechnungRun.Instances(suite, s.Syntax)).ToList();

        var results = await KositScenarioRun.GetResultsAsync(cancellationToken);

        await Assert.That(results.Count).IsEqualTo(instances.Count);
        await Assert
            .That(results.Select(r => r.CaseId).Distinct(StringComparer.Ordinal).Count())
            .IsEqualTo(results.Count);
    }

    [Test]
    public async Task Scenarios_LoadFromTheConfiguration_ListAllEleven(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configuration = await KositScenarioRun.GetConfigurationAsync(cancellationToken);

        var scenarios = KositScenarioRun.LoadScenarios(configuration);

        await Assert.That(scenarios.Count).IsEqualTo(11);
        await Assert.That(File.Exists(Path.Combine(configuration, scenarios[0].XsdLocation))).IsTrue();
    }

    private static string Format(TestSetReport report, IReadOnlyList<CaseResult> results)
    {
        var builder = new StringBuilder();
        foreach (var id in report.Regressions)
        {
            var reason = results.First(r => string.Equals(r.CaseId, id, StringComparison.Ordinal)).Reason;
            _ = builder.Append("fails: ").Append(id).Append(": ").Append(reason).Append('\n');
        }

        return builder.ToString();
    }
}
