namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Everything one pattern evaluation test needs: the fake matcher and binding, the real slot binder, assertion evaluator, issue
/// mapper, and problem sink over a collector, a recording caller sink, and the compiled rule sets of the schema.
/// </summary>
internal sealed class PatternHarness : IDisposable
{
    internal const string Source = "file:///instance.xml";

    private readonly PatternTestRun _run;

    internal PatternHarness(
        SchematronSchema schema,
        PatternElement document,
        int maxIssues = 100,
        string? namespacePrefix = null,
        CancellationToken cancellationToken = default
    )
        : this(schema, PatternTree.Build(document, namespacePrefix), maxIssues, cancellationToken) { }

    internal PatternHarness(
        SchematronSchema schema,
        XPathNode document,
        int maxIssues = 100,
        CancellationToken cancellationToken = default
    )
    {
        Schema = schema;
        Document = document;
        Log = new EventLog();
        Calls = [];
        Binding = new AssertionTestBinding();
        Matcher = new ScriptedMatcher(Calls);
        Collector = new ValidationIssueCollector(ValidationLimits.Default.WithMaxIssues(maxIssues));
        Mapper = new IssueMapper(Collector, Log);
        Problems = new EvaluationProblems(Collector, Mapper, Source);
        _run = new PatternTestRun(Binding, Calls);

        var compileProblems = new FakeProblemSink();
        if (!PhaseSelector.TryResolve(schema, "#ALL", compileProblems, out var selection))
        {
            throw new InvalidOperationException("The phase of the test schema does not resolve.");
        }

        if (!ScopePlanner.TryPlan(schema, selection, Binding, compileProblems, out var plan))
        {
            throw new InvalidOperationException("The scopes of the test schema do not plan.");
        }

        Plan = plan;
        var sets = new List<CompiledRuleSet>();
        foreach (var scope in plan.Patterns)
        {
            if (!RuleSetCompiler.TryCompile(schema, scope, Matcher, Binding, compileProblems, out var set))
            {
                throw new InvalidOperationException(
                    "The rules do not compile: "
                        + string.Join("; ", compileProblems.Problems.ConvertAll(p => p.Message))
                );
            }

            sets.Add(set);
        }

        Sets = sets;
        Binder = new SlotBinder(plan, _run, new QueryFocus(document, 1, 1), Problems);
        Assertions = new AssertionEvaluator(_run, Binder, Mapper, Problems, Source, 1000);
        Evaluator = new PatternEvaluator(Matcher, _run, Binder, Assertions, Mapper, Problems, cancellationToken);
    }

    internal SchematronSchema Schema { get; }

    internal IQueryRun Run => _run;

    internal XPathNode Document { get; }

    internal EventLog Log { get; }

    /// <summary>Gets the calls of the matcher and the variable binding in order: <c>match:text</c>, <c>bind:slot</c>.</summary>
    internal List<string> Calls { get; }

    internal AssertionTestBinding Binding { get; }

    internal ScriptedMatcher Matcher { get; }

    internal ValidationIssueCollector Collector { get; }

    internal IssueMapper Mapper { get; }

    internal EvaluationProblems Problems { get; }

    internal ScopePlan Plan { get; }

    internal IReadOnlyList<CompiledRuleSet> Sets { get; }

    internal SlotBinder Binder { get; }

    internal AssertionEvaluator Assertions { get; }

    internal PatternEvaluator Evaluator { get; }

    /// <summary>Evaluates the run; a ref struct summary cannot leave the method, so its two members do.</summary>
    internal PatternRunResult Evaluate()
    {
        var summary = Evaluator.Evaluate(Sets, Document);
        return new PatternRunResult(summary.IsTruncated, summary.StopCode);
    }

    public void Dispose()
    {
        Assertions.Dispose();
        _run.Dispose();
    }
}
