namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Notations;

using System.Collections.Generic;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;

/// <summary>What a run of the notation step reported, and where the script expected the first issue.</summary>
internal sealed class NotationRun
{
    internal NotationRun(ValidationIssueCollector collector, DocumentLocation expected)
    {
        Collector = collector;
        Result = collector.ToResult();
        Expected = expected;
    }

    internal ValidationIssueCollector Collector { get; }

    internal ValidationResult Result { get; }

    internal IReadOnlyList<ValidationIssue> Issues => Result.Issues;

    /// <summary>Gets the location of the <c>~</c> marker; the default location when the script has none.</summary>
    internal DocumentLocation Expected { get; }
}
