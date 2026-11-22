namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Selections;

/// <summary>One entry of the exclusion list of the XPath 3.0 and 3.1 selections.</summary>
/// <param name="Kind">What the entry matches.</param>
/// <param name="Key">The dependency, the test set name, or the regular expression.</param>
/// <param name="Reason">Why the cases are out of scope.</param>
/// <param name="DefId">The deferred feature that is the reason, for example <c>DEF-44</c>; <see langword="null"/> when no deferred feature is.</param>
/// <param name="Issue">The issue number of the deferred feature.</param>
internal sealed record Qt3ExclusionEntry(
    Qt3ExclusionKind Kind,
    string Key,
    string Reason,
    string? DefId = null,
    int? Issue = null
)
{
    /// <summary>Gets the reason with the deferred feature, <c>&lt;reason&gt;: DEF-44 (#56)</c>.</summary>
    public string Describe() => DefId is null ? Reason : Reason + ": " + DefId + " (#" + Issue + ")";
}
