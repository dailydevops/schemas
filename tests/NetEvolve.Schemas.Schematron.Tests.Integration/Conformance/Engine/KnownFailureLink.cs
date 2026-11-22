namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

/// <summary>The bug issue and the reason of one known failure of the baseline.</summary>
/// <param name="CaseId">The case id as in the baseline file.</param>
/// <param name="Issue">The number of the issue that tracks the failure.</param>
/// <param name="Reason">Why the case fails.</param>
internal sealed record KnownFailureLink(string CaseId, int Issue, string Reason);
