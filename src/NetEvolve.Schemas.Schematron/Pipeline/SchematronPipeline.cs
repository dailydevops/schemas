namespace NetEvolve.Schemas.Schematron.Pipeline;

using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Loading.Inclusion;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// The composition of the stages that turn a schema source into the compiled schema (SCH-55, #311, design #137 AP-26 and #150 EP-14):
/// parse (the tree of the Xml package, <c>xi:include</c>, the object model, and the edition), include and extends (resolve), expand
/// (abstract rules and patterns), then compile (the binding, the phase, the scopes, and the rules). The evaluation of an instance and
/// the mapping of its events to issues is <see cref="ValidationRun"/>.
/// </summary>
/// <remarks>
/// <para>
/// A stage reports all of its problems; a stage that reported one stops the later stages, because their input would be invalid. The
/// problems of the build are the issues of one <see cref="SchemaCompilationException"/>, up to <c>MaxIssues</c>; two builds of one input
/// give the same issues. The token is looked at between the stages and by the stages that read.
/// </para>
/// <para>
/// The edition is detected by the parse, which reports an unknown value (<c>SchematronEditionDetector.TryDetect</c>); calling
/// <c>Detect</c> again would report it twice, so the pipeline reads the edition from the model.
/// </para>
/// </remarks>
internal sealed class SchematronPipeline : ISchematronPipeline
{
    /// <summary>Gets the pipeline of the builder; it holds no state.</summary>
    internal static SchematronPipeline Instance { get; } = new();

    /// <inheritdoc />
    public CompiledSchema Load(SchemaSource source, BuildSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(settings);

        var problems = new BuildProblems(settings.Limits);
        var parser = new SchematronModelParser(settings, cancellationToken);
        ThrowIfFailed(parser.TryParse(source, problems, out var parsed), problems);

        cancellationToken.ThrowIfCancellationRequested();
        var resolver = new ModelResolver();
        var loader = new ModuleLoader(settings.Resolver, settings.Limits);
        ThrowIfFailed(resolver.TryResolve(parsed, loader, problems, out var resolved), problems);

        cancellationToken.ThrowIfCancellationRequested();
        var expander = new ModelExpander();
        ThrowIfFailed(expander.TryExpand(resolved, settings, problems, out var effective), problems);

        cancellationToken.ThrowIfCancellationRequested();
        var compiler = new SchematronSchemaCompiler(PipelineBindings.Registry);
        ThrowIfFailed(compiler.TryCompile(effective, settings, problems, out var compiled), problems);

        return compiled;
    }

    // A stage that reported a problem stops the build, whatever it returned; the check does not short-circuit, so it is one path.
    private static void ThrowIfFailed(bool succeeded, BuildProblems problems)
    {
        if (!succeeded | problems.HasProblems)
        {
            throw problems.ToException();
        }
    }
}
