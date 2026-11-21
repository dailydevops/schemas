namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.QNames;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Numerics;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

/// <summary>
/// Runs the QT3 cases of <see cref="Qt3QNameSelection"/> through the compiled expressions and checks each against its assertion, and
/// proves that the selection and its exclusions together are exactly the applicable cases of the twelve test sets: the cases that the
/// XPath 2.0 rule of the QT3 runner selects, which the baselines of <c>tests/conformance/baselines/qt3</c> list.
/// </summary>
public class Qt3QNameSelectionTests
{
    private static readonly string[] TestSets =
    [
        "fn-resolve-QName",
        "fn-QName",
        "fn-prefix-from-QName",
        "fn-local-name-from-QName",
        "fn-namespace-uri-from-QName",
        "fn-namespace-uri-for-prefix",
        "fn-in-scope-prefixes",
        "fn-resolve-uri",
        "fn-base-uri",
        "fn-static-base-uri",
        "fn-encode-for-uri",
        "fn-iri-to-uri",
    ];

    public static IEnumerable<Func<(string Id, string Expression, string Expectation)>> Cases()
    {
        foreach (var entry in Qt3QNameSelection.Cases)
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
    public async Task SelectionAndExclusions_AreListedOnceWithTheirReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, expression, expectation) in Qt3QNameSelection.Cases)
        {
            await Assert.That(seen.Add(id)).IsTrue().Because($"{id} is listed twice");
            await Assert.That(expression).IsNotEmpty();
            await Assert.That(expectation).IsNotEmpty();
        }

        foreach (var (id, reason) in Qt3QNameSelection.Exclusions)
        {
            await Assert.That(seen.Add(id)).IsTrue().Because($"{id} is listed twice or both selected and excluded");
            await Assert.That(reason).IsNotEmpty();
        }
    }

    [Test]
    public async Task Exclusions_NameAnIssueOrAWorkPackageOfTheBacklog()
    {
        foreach (var (id, reason) in Qt3QNameSelection.Exclusions)
        {
            // Every reason names the issue that closes it: a work package (XP-nn), a deferred feature (DEF-nn), or a design decision, with its number.
            await Assert.That(reason.Contains('#', StringComparison.Ordinal)).IsTrue().Because($"{id}: {reason}");
        }
    }

    [Test]
    public async Task Selection_HasACaseInEveryTestSet()
    {
        var sets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, _, _) in Qt3QNameSelection.Cases)
        {
            _ = sets.Add(id.Substring(0, id.IndexOf('/', StringComparison.Ordinal)));
        }

        foreach (var set in TestSets)
        {
            await Assert.That(sets.Contains(set)).IsTrue().Because($"{set} has no case in the selection");
        }
    }
}
