namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Numerics;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

/// <summary>
/// Runs the QT3 cases of <see cref="Qt3NumericSelection"/> through the compiled expressions and checks each against its assertion, and
/// proves that the selection and its exclusions together are exactly the applicable cases of the eight test sets: the cases that the
/// XPath 2.0 rule of the QT3 runner selects, which the baselines of <c>tests/conformance/baselines/qt3</c> list.
/// </summary>
public class Qt3NumericSelectionTests
{
    private static readonly string[] TestSets =
    [
        "fn-abs",
        "fn-ceiling",
        "fn-floor",
        "fn-round",
        "fn-round-half-to-even",
        "fn-number",
        "fn-true",
        "fn-false",
    ];

    public static IEnumerable<Func<(string Id, string Expression, string Expectation)>> Cases()
    {
        foreach (var entry in Qt3NumericSelection.Cases)
        {
            var captured = entry;
            yield return () => captured;
        }
    }

    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task Case_SatisfiesItsAssertion(string id, string expression, string expectation)
    {
        var (result, code) = Qt3SequenceFixture.Run(expression);

        await Assert
            .That(Qt3NumericFixture.Satisfies(expectation, result, code))
            .IsTrue()
            .Because($"{id}: {expression} gave {Qt3SequenceFixture.Describe(result, code)}, expected {expectation}");
    }

    [Test]
    public async Task SelectionAndExclusions_AreExactlyTheApplicableCasesOfTheEightTestSets(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var applicable = new HashSet<string>(StringComparer.Ordinal);
        var directory = Path.Combine(RepositoryRoot(), "tests", "conformance", "baselines", "qt3");
        foreach (var set in TestSets)
        {
            foreach (var line in await File.ReadAllLinesAsync(Path.Combine(directory, set + ".txt"), cancellationToken))
            {
                if (line.Length > 0)
                {
                    _ = applicable.Add(line.Trim());
                }
            }
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, expression, expectation) in Qt3NumericSelection.Cases)
        {
            await Assert.That(seen.Add(id)).IsTrue().Because($"{id} is listed twice");
            await Assert.That(applicable.Contains(id)).IsTrue().Because($"{id} is not an applicable case");
            await Assert.That(expression).IsNotEmpty();
            await Assert.That(expectation).IsNotEmpty();
        }

        foreach (var (id, reason) in Qt3NumericSelection.Exclusions)
        {
            await Assert.That(seen.Add(id)).IsTrue().Because($"{id} is listed twice or both selected and excluded");
            await Assert.That(applicable.Contains(id)).IsTrue().Because($"{id} is not an applicable case");
            await Assert.That(reason).IsNotEmpty();
        }

        await Assert.That(seen.Count).IsEqualTo(applicable.Count);
    }

    [Test]
    public async Task Exclusions_NameAnIssueOrAWorkPackageOfTheBacklog()
    {
        foreach (var (id, reason) in Qt3NumericSelection.Exclusions)
        {
            // Every reason names the issue that closes it: a work package (XP-nn), a deferred feature (DEF-nn), or a design decision, with its number.
            await Assert.That(reason.Contains('#', StringComparison.Ordinal)).IsTrue().Because($"{id}: {reason}");
        }
    }

    [Test]
    public async Task Exclusions_DoNotWaitForTheCastingToTheDerivedTypes()
    {
        foreach (var (id, reason) in Qt3NumericSelection.Exclusions)
        {
            // The casting to the derived types has landed (XP-23, #299), so no reason waits for it any more.
            await Assert.That(reason.Contains("XP-23", StringComparison.Ordinal)).IsFalse().Because($"{id}: {reason}");
            await Assert.That(reason.Contains("#299", StringComparison.Ordinal)).IsFalse().Because($"{id}: {reason}");
        }
    }

    [Test]
    public async Task Selection_HasACaseInEveryTestSet()
    {
        var sets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, _, _) in Qt3NumericSelection.Cases)
        {
            _ = sets.Add(id.Substring(0, id.IndexOf('/', StringComparison.Ordinal)));
        }

        foreach (var set in TestSets)
        {
            await Assert.That(sets.Contains(set)).IsTrue().Because($"{set} has no case in the selection");
        }
    }

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Schemas.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
