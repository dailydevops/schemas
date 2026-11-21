namespace NetEvolve.Schemas.Xsd.Tests.Integration.SchemaTests;

using NetEvolve.Schemas.Conformance.Baseline;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>
/// Every schema test of XSD 1.0 that the pipeline still fails is listed one by one with a reason and the issue that tracks it, so
/// no failure stays in the baseline without an explanation.
/// </summary>
[ConformanceLeg]
public class KnownFailuresTests
{
    private static readonly string ListPath = Path.Combine(
        XstsSuite.RepositoryRoot,
        "tests",
        "NetEvolve.Schemas.Xsd.Tests.Integration",
        "SchemaTests",
        "known-failures.txt"
    );

    private static bool IsIssueReference(string text) =>
        string.Equals(text, "-", StringComparison.Ordinal)
        || (text.Length > 1 && text[0] == '#' && text.Skip(1).All(char.IsDigit));

    private static async Task<List<string[]>> ReadListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = new List<string[]>();
        foreach (var line in await File.ReadAllLinesAsync(ListPath, cancellationToken))
        {
            if (line.Length > 0 && line[0] != '#')
            {
                rows.Add(line.Split('\t'));
            }
        }

        return rows;
    }

    [Test]
    public async Task List_EveryLineHasATestSetACaseAnIssueAndAReason(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await ReadListAsync(cancellationToken);

        await Assert.That(rows).IsNotEmpty();
        foreach (var row in rows)
        {
            await Assert.That(row.Length).IsEqualTo(4);
            await Assert.That(row[0]).IsNotEmpty();
            await Assert.That(row[1].StartsWith("1.0/", StringComparison.Ordinal)).IsTrue();
            await Assert.That(IsIssueReference(row[2])).IsTrue();
            await Assert.That(row[3].Length).IsGreaterThan(10);
        }

        await Assert
            .That(rows.Select(r => r[0] + "|" + r[1]).Distinct(StringComparer.Ordinal).Count())
            .IsEqualTo(rows.Count);
    }

    [Test]
    public async Task List_EqualsTheFailingSchemaTestsOfXsd10InTheBaseline(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);
        var schemaTests = new HashSet<string>(
            selection
                .Selected.Where(c =>
                    c.Case.Kind == XstsKind.Schema
                    && string.Equals(c.Version, XstsVersions.V10, StringComparison.Ordinal)
                )
                .Select(c => SchemaRateReport.KeyOf(c.TestSet, c.Id)),
            StringComparer.Ordinal
        );
        var failing = new List<string>();
        foreach (var file in Directory.GetFiles(XstsSuite.BaselineDirectory, "*.txt"))
        {
            var testSet = Path.GetFileNameWithoutExtension(file);
            foreach (var id in BaselineFiles.Load(XstsSuite.BaselineDirectory, testSet).Ids)
            {
                if (schemaTests.Contains(SchemaRateReport.KeyOf(testSet, id)))
                {
                    failing.Add(SchemaRateReport.KeyOf(testSet, id));
                }
            }
        }

        var listed = (await ReadListAsync(cancellationToken)).Select(r => SchemaRateReport.KeyOf(r[0], r[1]));

        await Assert.That(listed).IsEquivalentTo(failing);
    }
}
