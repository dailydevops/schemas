namespace NetEvolve.Schemas.Conformance.Results;

using System.Runtime.InteropServices;

/// <summary>The recorded result of one conformance case.</summary>
/// <param name="CaseId">The stable hierarchical case id.</param>
/// <param name="TestSet">The name of the test set the case belongs to.</param>
/// <param name="Outcome">The outcome of the case.</param>
/// <param name="Reason">The failure message or skip reason, if any.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct CaseResult(string CaseId, string TestSet, CaseOutcome Outcome, string? Reason = null);
