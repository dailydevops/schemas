namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Identity;

using System.Collections.Generic;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Formats;

/// <summary>What a run of the identity step reported, where the script expected issues, and how many issues existed before <c>EndDocument</c>.</summary>
internal sealed class IdentityRun
{
    internal IdentityRun(
        ValidationIssueCollector collector,
        IReadOnlyList<DocumentLocation> expected,
        int countBeforeEnd
    )
    {
        Collector = collector;
        Result = collector.ToResult();
        Expected = expected;
        CountBeforeEnd = countBeforeEnd;
    }

    internal ValidationIssueCollector Collector { get; }

    internal ValidationResult Result { get; }

    internal IReadOnlyList<ValidationIssue> Issues => Result.Issues;

    /// <summary>Gets the locations of the <c>~</c> markers of the script, in script order.</summary>
    internal IReadOnlyList<DocumentLocation> Expected { get; }

    /// <summary>Gets the number of issues reported before <c>EndDocument</c> ran.</summary>
    internal int CountBeforeEnd { get; }
}
