namespace NetEvolve.Schemas.Xsd.Diagnostics;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Components;

/// <summary>Turns rule violations into issues of the shared collector, so the drop policy lives once.</summary>
internal sealed class XsdIssueReporter
{
    private readonly ValidationIssueCollector _collector;

    internal XsdIssueReporter(ValidationIssueCollector collector)
    {
        Argument.ThrowIfNull(collector);

        _collector = collector;
    }

    /// <summary>Reports a violation of a rule.</summary>
    /// <param name="rule">The violated rule.</param>
    /// <param name="location">The location of the violation.</param>
    /// <param name="message">The message.</param>
    /// <param name="clause">The clause path such as <c>cvc-complex-type.2.4</c>, or <see langword="null"/> for the rule id.</param>
    internal void Report(XsdRule rule, DocumentLocation location, string message, string? clause = null)
    {
        Argument.ThrowIfNull(rule);
        Argument.ThrowIfNull(message);

        _collector.Add(new ValidationIssue(rule.Code, rule.DefaultSeverity, message, location, clause ?? rule.Id));
    }

    /// <summary>Adds an issue as it is, for the core codes that no rule of the catalog owns.</summary>
    /// <param name="issue">The issue.</param>
    internal void Add(ValidationIssue issue)
    {
        Argument.ThrowIfNull(issue);

        _collector.Add(issue);
    }

    /// <summary>Reports a violation of the rule with a code; a code missing from the catalog is a defect and throws.</summary>
    /// <param name="code">The code of the violated rule.</param>
    /// <param name="location">The location of the violation.</param>
    /// <param name="message">The message.</param>
    internal void Report(DiagnosticCode code, DocumentLocation location, string message) =>
        Report(XsdRuleLookup.Find(code), location, message);
}
