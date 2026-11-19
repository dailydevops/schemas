namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.ContentModels;

using System.Collections.Generic;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;

/// <summary>What a run of the content model step reported, and where the script expected the first issue.</summary>
internal sealed class ContentRun
{
    internal ContentRun(ValidationIssueCollector collector, DocumentLocation expected)
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
