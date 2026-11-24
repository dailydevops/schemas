namespace NetEvolve.Schemas.Benchmarks.Schematron.Core;

using NetEvolve.Schemas.Schematron;

/// <summary>A report sink that only counts fired rules, to prove that the library and the baseline do the same work.</summary>
internal sealed class FiredRuleCounter : ISchematronReportSink
{
    /// <summary>Gets the number of fired rules.</summary>
    public int FiredRules { get; private set; }

    /// <inheritdoc />
    public void Begin(in SchematronRunInfo info) { }

    /// <inheritdoc />
    public void ActivePattern(in SchematronPatternEvent pattern) { }

    /// <inheritdoc />
    public void FiredRule(in SchematronRuleEvent rule) => FiredRules++;

    /// <inheritdoc />
    public void Finding(in SchematronFinding finding) { }

    /// <inheritdoc />
    public void ErrorOccurred(in SchematronErrorEvent errorEvent) { }

    /// <inheritdoc />
    public void Finish(in SchematronRunSummary summary) { }
}
