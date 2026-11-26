namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

using NetEvolve.Schemas.Conformance.Reporting;

/// <summary>
/// The oracle for the include and abstract-pattern expansion: the CEN EN 16931 archive ships every rule set as multi-file source and as a
/// flattened single file, and both must fire the same rules on every document of the corpus.
/// </summary>
/// <remarks>
/// Applicable cases: every case of <see cref="En16931Run.LoadCases"/> for UBL and CII, so the unit-test corpus and the official examples.
/// The comparison needs no expectation data: it compares all fired rule ids of the two schemas, not only the scope of a case. The EDIFACT
/// rule set has no XML documents in the archive and is not compared. Differences that come from the upstream preprocessing itself are
/// listed in <see cref="Exclusions"/> with a reason.
/// </remarks>
[Explicit]
[Category(ConformanceCategory.Name)]
public class En16931PreprocessedTests
{
    // Case id (syntax prefixed) -> reason the upstream preprocessing differs. Empty while both rule sets agree.
    private static readonly Dictionary<string, string> Exclusions = new(StringComparer.Ordinal);

    [Test]
    [Arguments("ubl")]
    [Arguments("cii")]
    public async Task Source_AndPreprocessedRuleSet_FireTheSameRules(string syntax, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(syntax);
        var root = await En16931Run.GetRootAsync(cancellationToken);
        var sourcePath = En16931Run.SchemaPath(root, syntax);
        var preprocessedPath = En16931Run.PreprocessedSchemaPath(root, syntax);
        var source = En16931Runner.Build(sourcePath);
        var preprocessed = En16931Runner.Build(preprocessedPath);
        var cases = En16931Run.LoadCases(root, syntax);
        var differences = new List<string>();

        foreach (var testCase in cases)
        {
            var key = $"{syntax}:{testCase.Id}";
            if (Exclusions.ContainsKey(key))
            {
                continue;
            }

            var (expectedFired, _, expectedProblem) = En16931Runner.Validate(
                source,
                sourcePath,
                testCase,
                cancellationToken
            );
            var (actualFired, _, actualProblem) = En16931Runner.Validate(
                preprocessed,
                preprocessedPath,
                testCase,
                cancellationToken
            );
            var left = expectedProblem ?? Join(expectedFired!);
            var right = actualProblem ?? Join(actualFired!);
            if (!string.Equals(left, right, StringComparison.Ordinal))
            {
                differences.Add($"{key}: source [{left}] preprocessed [{right}]");
            }
        }

        await Assert.That(cases).IsNotEmpty();
        await Assert.That(differences).IsEmpty().Because(string.Join("\n", differences));
    }

    private static string Join(HashSet<string> ids) => string.Join(", ", ids.OrderBy(id => id, StringComparer.Ordinal));
}
