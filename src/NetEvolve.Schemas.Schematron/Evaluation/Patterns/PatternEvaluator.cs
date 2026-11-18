namespace NetEvolve.Schemas.Schematron.Evaluation.Patterns;

using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>Evaluates the active patterns of one run over the instance. Not thread-safe: one per run.</summary>
internal sealed class PatternEvaluator
{
    internal PatternEvaluator(
        IPatternMatcher matcher,
        IQueryRun run,
        SlotBinder binder,
        AssertionEvaluator assertions,
        IssueMapper events,
        EvaluationProblems problems,
        CancellationToken cancellationToken
    ) => throw NotImplementedFailure.For(GetType().Name);

    /// <summary>Evaluates every pattern in order, each in one pass over the instance.</summary>
    internal SchematronRunSummary Evaluate(IReadOnlyList<CompiledRuleSet> sets, in XPathNode document) =>
        throw NotImplementedFailure.For(GetType().Name);
}
