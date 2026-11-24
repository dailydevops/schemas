namespace NetEvolve.Schemas.Schematron.Pipeline;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The pattern matcher of a compiled schema as the build sees it: it forwards to the matcher and records the text of every rule context
/// with the <see cref="CompilingBinding"/>, so the contexts count as compiled expressions and their names are interned.
/// </summary>
internal sealed class CompilingMatcher : IPatternMatcher
{
    private readonly IPatternMatcher _inner;
    private readonly CompilingBinding _recorder;

    internal CompilingMatcher(IPatternMatcher inner, CompilingBinding recorder)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(recorder);

        _inner = inner;
        _recorder = recorder;
    }

    public bool TryCompile(
        ReadOnlySpan<char> pattern,
        PatternUse use,
        QueryStaticScope scope,
        out CompiledPattern compiled,
        out SchematronProblem problem
    )
    {
        _recorder.Record(pattern, scope);
        return _inner.TryCompile(pattern, use, scope, out compiled, out problem);
    }

    public QueryStatus Matches(CompiledPattern compiled, IQueryRun run, in XPathNode candidate, out bool matched) =>
        _inner.Matches(compiled, run, in candidate, out matched);
}
