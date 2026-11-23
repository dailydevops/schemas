namespace NetEvolve.Schemas.Xsd.Tests.Integration.DifferentialData;

using System.Collections.Concurrent;
using NetEvolve.Schemas.Resources.FileSystem;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;
using ValidationResult = NetEvolve.Schemas.ValidationResult;

/// <summary>
/// This library as a differential engine: the schema documents of a case are built through the public entry point, strict 1.0 with
/// DTD processing for the entities of the instances and the xsi:schemaLocation hints (as the XSTS instance tests do), and the instance is validated with the result.
/// </summary>
/// <remarks>
/// Limitation: a pipeline that throws something other than a schema compilation error observes nothing; <see cref="ObserveAsync"/>
/// then returns <see langword="null"/> so that a defect never counts as a verdict.
/// </remarks>
internal sealed class OursEngine : IDifferentialEngine
{
    private readonly string _suiteRoot;
    private readonly ConcurrentDictionary<string, Task<Built>> _builds = new(StringComparer.Ordinal);

    public OursEngine(string suiteRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteRoot);
        _suiteRoot = Path.GetFullPath(suiteRoot);
    }

    public async Task<EngineResult> RunAsync(DifferentialCase differentialCase, CancellationToken cancellationToken) =>
        await ObserveAsync(differentialCase, cancellationToken)
        ?? throw new InvalidOperationException($"Case {differentialCase?.Id} cannot be observed.");

    /// <summary>Compiles the schema documents of the case and validates its instance.</summary>
    /// <param name="differentialCase">The case.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The verdicts and issue positions, or <see langword="null"/> when this library cannot observe the case.</returns>
    public async Task<EngineResult?> ObserveAsync(
        DifferentialCase differentialCase,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(differentialCase);

        var built = await _builds.GetOrAdd(
            string.Join('\n', differentialCase.SchemaPaths),
            static (_, state) => state.Engine.BuildAsync(state.Paths, CancellationToken.None),
            (Engine: this, Paths: differentialCase.SchemaPaths)
        );
        if (built.Failed)
        {
            return null;
        }

        if (built.Validator is null)
        {
            return new EngineResult(Verdict.Invalid, Verdict.NotRun, built.Issues);
        }

        if (differentialCase.InstancePath is null)
        {
            return new EngineResult(Verdict.Valid, Verdict.NotRun, []);
        }

        try
        {
            var bytes = await File.ReadAllBytesAsync(differentialCase.InstancePath, cancellationToken);
            var result = await built.Validator.ValidateAsync(
                bytes,
                new Uri(differentialCase.InstancePath).AbsoluteUri,
                cancellationToken
            );
            return new EngineResult(Verdict.Valid, result.IsValid ? Verdict.Valid : Verdict.Invalid, Errors(result));
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return null;
        }
    }

    // The build is shared by every case of the group; two cases that race for it build twice at worst and keep one result.
    private async Task<Built> BuildAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var builder = SchemaValidator
                .ForXsd()
                .WithVersion(XsdVersion.V10)
                .WithDtdProcessing(true)
                .WithSchemaLocationHints(true)
                .WithResolver(new SandboxedFileSystemResourceResolver(_suiteRoot));
            foreach (var path in paths)
            {
                _ = builder.AddSchema(await File.ReadAllBytesAsync(path, cancellationToken), new Uri(path).AbsoluteUri);
            }

            return new Built(await builder.BuildAsync(cancellationToken), [], false);
        }
        catch (SchemaCompilationException e)
        {
            return new Built(null, Errors(e.Issues), false);
        }
        catch (Exception)
        {
            return new Built(null, [], true);
        }
    }

    private static IssuePosition[] Errors(ValidationResult result) => Errors(result.Issues);

    private static IssuePosition[] Errors(IReadOnlyList<ValidationIssue> issues)
    {
        var positions = new List<IssuePosition>();
        foreach (var issue in issues)
        {
            if (issue.Severity == IssueSeverity.Error)
            {
                positions.Add(new IssuePosition(issue.Location.Line, issue.Location.Column));
            }
        }

        return [.. positions];
    }

    private sealed record Built(IXsdValidator? Validator, IReadOnlyList<IssuePosition> Issues, bool Failed);
}
