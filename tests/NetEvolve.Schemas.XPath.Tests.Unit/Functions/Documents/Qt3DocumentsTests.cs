namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Documents;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Arguments;

/// <summary>
/// Runs the QT3 cases of <see cref="Qt3DocumentsSelection"/> that the integration runner cannot decide, and proves that the exclusions are
/// a superset of the cases that stay in the baselines of the five test sets (the baselines may shrink), each with a reason that names the issue that closes it.
/// </summary>
public class Qt3DocumentsTests
{
    private static readonly string[] TestSets = ["fn-doc", "fn-doc-available", "fn-id", "fn-idref", "fn-collection"];

    public static IEnumerable<Func<(string Id, string Expression, string Accepted)>> Cases()
    {
        foreach (var entry in Qt3DocumentsSelection.Cases)
        {
            var captured = entry;
            yield return () => captured;
        }
    }

    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task Case_EndsInAnAcceptedOutcome(string id, string expression, string accepted)
    {
        Argument.ThrowIfNull(accepted);
        var result = DocumentsHarness.Run(expression);

        await Assert
            .That(accepted.Split('|'))
            .Contains(result)
            .Because($"{id}: {expression} gave {result}, expected one of {accepted}");
    }

    [Test]
    public async Task Cases_AreExcludedFromTheRunnerBaselines_BecauseTheRunnerSkipsThem()
    {
        var excluded = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, _) in Qt3DocumentsSelection.Exclusions)
        {
            _ = excluded.Add(id);
        }

        foreach (var (id, _, _) in Qt3DocumentsSelection.Cases)
        {
            await Assert
                .That(excluded.Contains(id))
                .IsTrue()
                .Because($"{id} is decided here and skipped by the runner");
        }
    }

    [Test]
    public async Task Exclusions_AreListedOnceWithAReasonThatNamesAnIssue()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, reason) in Qt3DocumentsSelection.Exclusions)
        {
            await Assert.That(seen.Add(id)).IsTrue().Because($"{id} is listed twice");
            await Assert.That(reason.Contains('#', StringComparison.Ordinal)).IsTrue().Because($"{id}: {reason}");
        }
    }

    [Test]
    public async Task Exclusions_CoverEveryCaseThatStaysInTheBaselinesOfTheFiveTestSets(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var baselines = new HashSet<string>(StringComparer.Ordinal);
        foreach (var testSet in TestSets)
        {
            var path = Path.Combine(RepositoryRoot(), "tests", "conformance", "baselines", "qt3", testSet + ".txt");
            foreach (var line in await File.ReadAllLinesAsync(path, cancellationToken))
            {
                if (line.Length > 0)
                {
                    _ = baselines.Add(line);
                }
            }
        }

        var excluded = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, _) in Qt3DocumentsSelection.Exclusions)
        {
            _ = excluded.Add(id);
        }

        var unexplained = new List<string>(baselines);
        _ = unexplained.RemoveAll(excluded.Contains);

        await Assert.That(string.Join(", ", unexplained)).IsEmpty().Because("baseline cases without an exclusion");
    }

    private static string RepositoryRoot()
    {
        for (
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent
        )
        {
            if (File.Exists(Path.Combine(directory.FullName, "Schemas.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("The repository root with Schemas.slnx was not found.");
    }
}
