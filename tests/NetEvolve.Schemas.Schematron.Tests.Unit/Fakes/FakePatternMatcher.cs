namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

internal sealed class FakePatternMatcher : IPatternMatcher
{
    internal int MatchCalls { get; private set; }

    public bool TryCompile(
        ReadOnlySpan<char> pattern,
        PatternUse use,
        QueryStaticScope scope,
        out CompiledPattern compiled,
        out SchematronProblem problem
    )
    {
        compiled = new CompiledPattern(QueryNodeKinds.Element, [new XPathQName(string.Empty, pattern.ToString())]);
        problem = default;
        return true;
    }

    public QueryStatus Matches(CompiledPattern compiled, IQueryRun run, in XPathNode candidate, out bool matched)
    {
        MatchCalls++;
        matched = true;
        return QueryStatus.Ok;
    }
}
