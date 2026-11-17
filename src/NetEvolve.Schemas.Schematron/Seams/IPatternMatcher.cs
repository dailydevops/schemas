namespace NetEvolve.Schemas.Schematron.Seams;

using System;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>The one matcher used by rule contexts and by <c>xsl:key</c>.</summary>
internal interface IPatternMatcher
{
    /// <summary>Compiles a pattern; an unsupported form is <see cref="SchematronDiagnosticCodes.PatternUnsupported"/> with a location.</summary>
    bool TryCompile(
        ReadOnlySpan<char> pattern,
        PatternUse use,
        QueryStaticScope scope,
        out CompiledPattern compiled,
        out SchematronProblem problem
    );

    /// <summary>Matches one candidate node; the problem is read via <see cref="IQueryRun.GetProblem"/>.</summary>
    QueryStatus Matches(CompiledPattern compiled, IQueryRun run, in XPathNode candidate, out bool matched);
}
