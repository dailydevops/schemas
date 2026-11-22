namespace NetEvolve.Schemas.Schematron.Pipeline;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The problems of the stages of one build, as the issues of one <see cref="SchemaCompilationException"/> (design #137 AP-26): every
/// problem is an error, and the collector keeps the first <c>MaxIssues</c> of them.
/// </summary>
internal sealed class BuildProblems : IProblemSink
{
    private readonly ValidationIssueCollector _collector;

    internal BuildProblems(ValidationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        _collector = new ValidationIssueCollector(limits);
    }

    /// <summary>Gets a value indicating whether a problem was reported; a stage that reported one stops the later stages.</summary>
    internal bool HasProblems { get; private set; }

    /// <inheritdoc />
    public void Add(in SchematronProblem problem)
    {
        HasProblems = true;
        _collector.Add(
            new ValidationIssue(
                problem.Code,
                IssueSeverity.Error,
                problem.Message,
                problem.Location,
                problem.Constraint
            )
        );
    }

    /// <summary>Creates the exception that carries every issue of the build.</summary>
    /// <returns>The exception.</returns>
    internal SchemaCompilationException ToException() => new(_collector.ToResult().Issues);
}
