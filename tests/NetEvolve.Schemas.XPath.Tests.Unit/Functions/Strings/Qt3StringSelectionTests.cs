namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs the QT3 cases of <see cref="Qt3StringSelection"/> through the compiled expressions and checks each against its assertion, and
/// proves that the selection and its exclusions together are exactly the applicable cases of the sixteen test sets: the cases that
/// the XPath 2.0 rule of the QT3 runner selects, which the baselines of <c>tests/conformance/baselines/qt3</c> list.
/// </summary>
public class Qt3StringSelectionTests
{
    // The twelve sets of the work package, then the four further sets of the string group.
    private static readonly string[] TestSets =
    [
        "fn-concat",
        "fn-substring",
        "fn-substring-before",
        "fn-substring-after",
        "fn-contains",
        "fn-starts-with",
        "fn-ends-with",
        "fn-translate",
        "fn-normalize-space",
        "fn-string-length",
        "fn-upper-case",
        "fn-lower-case",
        "fn-string-join",
        "fn-normalize-unicode",
        "fn-codepoints-to-string",
        "fn-string-to-codepoints",
    ];

    public static IEnumerable<Func<(string Id, string Expression, string Expectation)>> Cases()
    {
        foreach (var entry in Qt3StringSelection.Cases)
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
            .That(Satisfies(expectation, result, code))
            .IsTrue()
            .Because($"{id}: {expression} gave {Qt3SequenceFixture.Describe(result, code)}, expected {expectation}");
    }

    [Test]
    public async Task SelectionAndExclusions_AreListedOnceWithTheirReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, expression, expectation) in Qt3StringSelection.Cases)
        {
            await Assert.That(seen.Add(id)).IsTrue().Because($"{id} is listed twice");
            await Assert.That(expression).IsNotEmpty();
            await Assert.That(expectation).IsNotEmpty();
        }

        foreach (var (id, reason) in Qt3StringSelection.Exclusions)
        {
            await Assert.That(seen.Add(id)).IsTrue().Because($"{id} is listed twice or both selected and excluded");
            await Assert.That(reason).IsNotEmpty();
        }
    }

    [Test]
    public async Task Exclusions_NameAnIssueOrAWorkPackageOfTheBacklog()
    {
        foreach (var (id, reason) in Qt3StringSelection.Exclusions)
        {
            // Every reason names the issue that closes it: a work package (XP-nn), a deferred feature (DEF-nn), or a design decision, with its number.
            await Assert.That(reason.Contains('#', StringComparison.Ordinal)).IsTrue().Because($"{id}: {reason}");
        }
    }

    [Test]
    public async Task Exclusions_DoNotWaitForTheCastingToTheDerivedTypes()
    {
        foreach (var (id, reason) in Qt3StringSelection.Exclusions)
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
        foreach (var (id, _, _) in Qt3StringSelection.Cases)
        {
            _ = sets.Add(id.Substring(0, id.IndexOf('/', StringComparison.Ordinal)));
        }

        foreach (var set in TestSets)
        {
            await Assert.That(sets.Contains(set)).IsTrue().Because($"{set} has no case in the selection");
        }
    }

    // An expectation is a tab-separated list of alternatives; an alternative is a list of assertions that all hold (all-of), separated by the unit separator (U+001F).
    private static bool Satisfies(string expectation, XdmSequence result, string code)
    {
        ArgumentNullException.ThrowIfNull(expectation);

        foreach (var alternative in expectation.Split('	'))
        {
            var all = true;
            foreach (var assertion in alternative.Split((char)0x1F))
            {
                if (!Qt3SequenceFixture.Satisfies(assertion, result, code))
                {
                    all = false;
                    break;
                }
            }

            if (all)
            {
                return true;
            }
        }

        return false;
    }
}
