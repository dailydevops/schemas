namespace NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;

internal sealed class FakeEvaluationEvents(bool wantsLocationPath) : IEvaluationEvents
{
    private readonly FakeReportSink _inner = new();

    internal List<string> Events => _inner.Events;

    public bool WantsLocationPath { get; } = wantsLocationPath;

    public void Begin(in SchematronRunInfo info) => _inner.Begin(in info);

    public void ActivePattern(in SchematronPatternEvent pattern) => _inner.ActivePattern(in pattern);

    public void FiredRule(in SchematronRuleEvent rule) => _inner.FiredRule(in rule);

    public void Finding(in SchematronFinding finding) => _inner.Finding(in finding);

    public void ErrorOccurred(in SchematronErrorEvent errorEvent) => _inner.ErrorOccurred(in errorEvent);

    public void Finish(in SchematronRunSummary summary) => _inner.Finish(in summary);
}
