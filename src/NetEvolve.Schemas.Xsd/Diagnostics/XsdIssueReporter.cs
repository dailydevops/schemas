namespace NetEvolve.Schemas.Xsd.Diagnostics;

using NetEvolve.Schemas.Formats;

/// <summary>Turns rule violations into issues of the shared collector, so the drop policy lives once.</summary>
internal sealed class XsdIssueReporter
{
    private readonly ValidationIssueCollector _collector;

    internal XsdIssueReporter(ValidationIssueCollector collector) => _collector = collector;

    /// <summary>Reports a violation of a rule.</summary>
    /// <param name="rule">The violated rule.</param>
    /// <param name="location">The location of the violation.</param>
    /// <param name="message">The message.</param>
    /// <param name="clause">The clause path such as <c>cvc-complex-type.2.4</c>, or <see langword="null"/> for the rule id.</param>
    internal void Report(XsdRule rule, DocumentLocation location, string message, string? clause = null) =>
        _collector.Add(new ValidationIssue(rule.Code, rule.DefaultSeverity, message, location, clause ?? rule.Id));
}
