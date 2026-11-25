namespace NetEvolve.Schemas.Xsd.Tests.Integration.Readme;

using System.Globalization;
using NetEvolve.Schemas.Conformance.Reporting;
using NetEvolve.Schemas.Testing.Readme;
using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;
using NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>
/// The conformance statement of src/NetEvolve.Schemas.Xsd/README.md equals the value of the conformance run: the applicable cases of the
/// schema tests and the instance tests of XSD 1.0 come from the pinned suite, the known failures from the committed lists that the run
/// keeps equal to the baseline, and the excluded cases from the selection.
/// </summary>
/// <remarks>Like the conformance suites, the class is explicit and selected by its category, because the selection needs the fetched suite.</remarks>
[Explicit]
[Category(ConformanceCategory.Name)]
public class XsdReadmeConformanceTests
{
    private const string Readme = "src/NetEvolve.Schemas.Xsd/README.md";
    private const string SchemaFailures =
        "tests/NetEvolve.Schemas.Xsd.Tests.Integration/SchemaTests/known-failures.txt";
    private const string InstanceFailures =
        "tests/NetEvolve.Schemas.Xsd.Tests.Integration/InstanceTests/known-failures.txt";

    [Test]
    public async Task Readme_StatesTheRowOfTheSchemaTests(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var applicable = await Applicable(XstsKind.Schema, cancellationToken);
        var failed = Failures(SchemaFailures);

        await Assert.That(failed).IsGreaterThan(0);
        await Assert.That(ReadmeSamples.ReadText(Readme)).Contains(Row("Schema tests", applicable, failed));
    }

    [Test]
    public async Task Readme_StatesTheRowOfTheInstanceTests(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var applicable = await Applicable(XstsKind.Instance, cancellationToken);
        var failed = Failures(InstanceFailures);

        await Assert.That(failed).IsGreaterThan(0);
        await Assert.That(ReadmeSamples.ReadText(Readme)).Contains(Row("Instance tests", applicable, failed));
    }

    [Test]
    public async Task Readme_StatesTheTotalAndTheExcludedCasesOfXsd10(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);
        var applicable = selection.Selected.Count(c => c.Version == XstsVersions.V10);
        var failed = Failures(SchemaFailures) + Failures(InstanceFailures);
        var excluded = selection.Excluded.Count(c => c.Id.StartsWith(XstsVersions.V10 + "/", StringComparison.Ordinal));
        var readme = ReadmeSamples.ReadText(Readme);

        await Assert.That(readme).Contains(Row("Both", applicable, failed));
        await Assert.That(readme).Contains($"{excluded.ToString(CultureInfo.InvariantCulture)} excluded cases");
    }

    private static async Task<int> Applicable(XstsKind kind, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selection = await XstsSuite.LoadSelectionAsync(cancellationToken);
        return selection.Selected.Count(c => c.Version == XstsVersions.V10 && c.Case.Kind == kind);
    }

    // One line per case; the lines that start with a hash are the header.
    private static int Failures(string relativePath) =>
        ReadmeSamples
            .ReadText(relativePath)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(line => line[0] != '#');

    // The rate as the job summary prints it: passed / applicable, truncated, never rounded up, to one decimal.
    private static string Row(string label, int applicable, int failed)
    {
        var passed = applicable - failed;
        var tenths = passed * 1000L / applicable;
        return string.Format(
            CultureInfo.InvariantCulture,
            "| {0} | {1} | {2} | {3} | {4}.{5}% |",
            label,
            applicable,
            passed,
            failed,
            tenths / 10,
            tenths % 10
        );
    }
}
