namespace NetEvolve.Schemas.Schematron.Xslt.Modules;

using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Passes the problems of the compile of one declaration on and names the module in the location of a problem that has no source: a
/// loaded module has no document URI in its tree, so the instructions locate their problems by line and column only.
/// </summary>
internal sealed class XsltModuleProblemSink : IProblemSink
{
    private readonly IProblemSink _inner;
    private readonly string? _systemId;

    internal XsltModuleProblemSink(IProblemSink inner, string? systemId)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
        _systemId = systemId;
    }

    /// <inheritdoc />
    public void Add(in SchematronProblem problem)
    {
        if (problem.Location.Source is not null)
        {
            _inner.Add(in problem);
            return;
        }

        var located = new SchematronProblem(
            problem.Code,
            problem.Message,
            new DocumentLocation(_systemId, problem.Location.Line, problem.Location.Column),
            problem.Constraint
        );
        _inner.Add(in located);
    }
}
