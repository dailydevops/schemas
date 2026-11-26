namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.FrenchCtc;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage3Documents;

/// <summary>Builds the rule sets that have no sample documents.</summary>
internal static class FrenchCtcRunner
{
    /// <summary>The suffix of the case id of a build-only case.</summary>
    public const string BuildCase = "build";

    /// <summary>Builds every rule set without documents.</summary>
    /// <param name="manifest">The record.</param>
    /// <param name="suiteRoot">The root of the fetched suite.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>One result per rule set without documents; the case id is <c>&lt;rule set name&gt;/build</c>.</returns>
    public static IReadOnlyList<CaseResult> RunBuildOnly(
        Stage3Manifest manifest,
        string suiteRoot,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(suiteRoot);
        var results = new List<CaseResult>();
        foreach (var ruleSet in manifest.RuleSets.Where(r => r.Documents.Count == 0))
        {
            var path = Path.Combine(suiteRoot, ruleSet.Schema);
            var id = $"{ruleSet.Name}/{BuildCase}";
            var testSet = "stage3-" + ruleSet.Name;
            try
            {
                _ = SchemaValidator.ForSchematron(File.ReadAllBytes(path), new Uri(path).AbsoluteUri).Build();
                results.Add(new CaseResult(id, testSet, CaseOutcome.Pass));
            }
            catch (Exception exception) when (exception is not (OperationCanceledException or OutOfMemoryException))
            {
                results.Add(
                    new CaseResult(id, testSet, CaseOutcome.Fail, $"{exception.GetType().Name}: {exception.Message}")
                );
            }
        }

        return results;
    }
}
