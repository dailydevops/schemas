namespace NetEvolve.Schemas.XPath.Tests.Integration.Compatibility;

using System.Runtime.InteropServices;
using System.Text;
using NetEvolve.Schemas.Conformance.Reporting;

/// <summary>
/// The corpus against the live baseline, System.Xml.XPath of the runtime that <see cref="CompatibilityExpectations.BaselineRuntime"/> names.
/// It is explicit and runs in the conformance workflow, where the library and the baseline must give
/// the same result for every case that <see cref="ListedDifferences"/> does not list. Set <c>SCHEMAS_XPATH_COMPAT_RECORD</c> to a file path and
/// run <see cref="Record"/> to write the recorded results again after a change of the corpus.
/// </summary>
public class CompatibilityBaselineTests
{
    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    public async Task Baseline_RunsOnTheRuntimeThatTheExpectationsNameAndStillGivesTheRecordedResults()
    {
        await Assert.That(RuntimeInformation.FrameworkDescription).StartsWith(".NET 10.0");

        foreach (var testCase in CompatibilityCorpus.Cases)
        {
            await Assert
                .That(BaselineEngine.Evaluate(testCase.Document, testCase.Expression))
                .IsEqualTo(CompatibilityExpectations.All[testCase.Key])
                .Because(testCase.Key);
        }
    }

    [Test]
    [Explicit]
    [Category(ConformanceCategory.Name)]
    [MethodDataSource(typeof(CompatibilityDifferentialTests), nameof(CompatibilityDifferentialTests.Cases))]
    public async Task Expression_GivesTheSameResultAsTheBaselineUnlessListed(string document, string expression)
    {
        var key = new CorpusCase(document, expression).Key;
        var ours = CompatibilityEngine.Evaluate(document, expression, compatibility: true);
        var baseline = BaselineEngine.Evaluate(document, expression);

        if (ListedDifferences.All.TryGetValue(key, out var listed))
        {
            await Assert.That(ours).IsEqualTo(listed.Ours);
            await Assert.That(baseline).IsNotEqualTo(ours).Because("a listed difference must still differ: " + key);
        }
        else
        {
            await Assert.That(ours).IsEqualTo(baseline);
        }
    }

    [Test]
    [Explicit]
    public async Task Record(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = Environment.GetEnvironmentVariable("SCHEMAS_XPATH_COMPAT_RECORD");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var text = new StringBuilder();
        foreach (var testCase in CompatibilityCorpus.Cases)
        {
            var result = BaselineEngine.Evaluate(testCase.Document, testCase.Expression);
            _ = text.Append("            [\"")
                .Append(Escape(testCase.Key))
                .Append("\"] = \"")
                .Append(Escape(result))
                .Append("\",\n");
        }

        await File.WriteAllTextAsync(path, text.ToString(), cancellationToken);
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
