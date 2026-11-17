namespace NetEvolve.Schemas.Schematron.Patterns;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>Stub of the pattern matcher until the implementation commit.</summary>
internal sealed class PatternMatcher : IPatternMatcher
{
    internal PatternMatcher(IQueryBinding binding) => Argument.ThrowIfNull(binding);

    internal static IReadOnlyList<decimal> DefaultPriorities(CompiledPattern compiled) =>
        throw NotImplementedFailure.For("pattern");

    public bool TryCompile(
        ReadOnlySpan<char> pattern,
        PatternUse use,
        QueryStaticScope scope,
        out CompiledPattern compiled,
        out SchematronProblem problem
    ) => throw NotImplementedFailure.For("pattern");

    public QueryStatus Matches(CompiledPattern compiled, IQueryRun run, in XPathNode candidate, out bool matched) =>
        throw NotImplementedFailure.For("pattern");
}
