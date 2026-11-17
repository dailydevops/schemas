#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas.Schematron for the public result types (design #137 section 2)

namespace NetEvolve.Schemas.Schematron;

/// <summary>Receives the events of one validation run. Invoked on the validating thread only, one run at a time.</summary>
public interface ISchematronReportSink
{
    /// <summary>The run begins.</summary>
    /// <param name="info">Title, phase, schema version, edition, namespace prefixes, and the instance system identifier.</param>
    void Begin(in SchematronRunInfo info);

    /// <summary>A pattern or group is active.</summary>
    /// <param name="pattern">The pattern.</param>
    void ActivePattern(in SchematronPatternEvent pattern);

    /// <summary>A rule fired for a context node.</summary>
    /// <param name="rule">The rule.</param>
    void FiredRule(in SchematronRuleEvent rule);

    /// <summary>A failed assert or a successful report.</summary>
    /// <param name="finding">The finding.</param>
    void Finding(in SchematronFinding finding);

    /// <summary>A run-level or pattern-scoped error.</summary>
    /// <param name="errorEvent">The error.</param>
    void ErrorOccurred(in SchematronErrorEvent errorEvent);

    /// <summary>The run ends.</summary>
    /// <param name="summary">The summary.</param>
    void Finish(in SchematronRunSummary summary);
}
