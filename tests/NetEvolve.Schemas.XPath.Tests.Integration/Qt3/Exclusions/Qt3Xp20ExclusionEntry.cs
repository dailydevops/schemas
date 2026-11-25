namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Exclusions;

/// <summary>One excluded case of the XPath 2.0 selection, as listed in <c>Qt3/Exclusions/Xp20</c>.</summary>
/// <param name="CaseId">The case id, <c>&lt;test-set&gt;/&lt;case-name&gt;</c>.</param>
/// <param name="Dependencies">The unmet catalog dependencies, <c>; </c>-separated, as the dependency filter reports them.</param>
/// <param name="Reason">Why the case is out of scope.</param>
/// <param name="DefIds">The deferred features that are the reason, <c>,</c>-separated, for example <c>DEF-40</c>; empty when none is.</param>
internal sealed record Qt3Xp20ExclusionEntry(string CaseId, string Dependencies, string Reason, string DefIds);
