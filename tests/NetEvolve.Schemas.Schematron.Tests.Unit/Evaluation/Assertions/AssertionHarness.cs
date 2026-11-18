namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Everything one assertion test needs: the fake binding, the real mapper over a collector, a recording caller sink, and the evaluator of the run.</summary>
internal sealed class AssertionHarness : IDisposable
{
    internal const string Source = "file:///instance.xml";

    private readonly IQueryRun _run;

    internal AssertionHarness(
        SchematronDiagnostic[]? diagnostics = null,
        SchematronProperty[]? properties = null,
        string? ruleSubject = null,
        string[]? variables = null,
        int maxTextLength = 1000,
        int maxIssues = 100
    )
    {
        Binding = new AssertionTestBinding();
        Binder = new RecordingSlotBinder();
        Sink = new RecordingReportSink();
        Problems = new FakeProblemSink();
        Collector = new ValidationIssueCollector(ValidationLimits.Default.WithMaxIssues(maxIssues));
        Mapper = new IssueMapper(Collector, Sink);
        Schema = AssertionModel.Schema(diagnostics, properties);
        Rule = new RuleScope(
            AssertionModel.Rule(subject: ruleSubject),
            [],
            new QueryStaticScope([], variables ?? [], null, SchematronEdition.Iso2016)
        );
        _run = Binding.BeginRun(null!, default);
        Evaluator = new AssertionEvaluator(_run, Binder, Mapper, Problems, Source, maxTextLength);
    }

    internal AssertionTestBinding Binding { get; }

    internal RecordingSlotBinder Binder { get; }

    internal RecordingReportSink Sink { get; }

    internal FakeProblemSink Problems { get; }

    internal ValidationIssueCollector Collector { get; }

    internal IssueMapper Mapper { get; }

    internal SchematronSchema Schema { get; }

    internal RuleScope Rule { get; }

    internal AssertionEvaluator Evaluator { get; }

    internal IQueryRun Run => _run;

    /// <summary>Compiles an assertion in the rule scope; the test fails with the problem text when it does not compile.</summary>
    internal CompiledAssertion Compile(SchematronAssertion assertion, AssertionPosition? position = null)
    {
        var at = position ?? new AssertionPosition(1, 1, 1);
        if (!AssertionCompiler.TryCompile(assertion, Rule, Schema, Binding, in at, Problems, out var compiled))
        {
            throw new InvalidOperationException(
                "The assertion does not compile: " + string.Join("; ", Problems.Problems.ConvertAll(p => p.Message))
            );
        }

        return compiled;
    }

    /// <summary>Evaluates an assertion for the node a key names, with position and size 1.</summary>
    internal AssertionOutcome Evaluate(CompiledAssertion assertion, string nodeKey = "order1")
    {
        var focus = new QueryFocus(AssertionTree.Node(nodeKey), 1, 1);
        return Evaluator.Evaluate(assertion, in focus);
    }

    internal static XdmItem NodeItem(string key) => XdmItem.FromNode(AssertionTree.Node(key));

    public void Dispose()
    {
        Evaluator.Dispose();
        _run.Dispose();
    }
}
