namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>One observed difference between this library and the baseline.</summary>
/// <param name="CaseId">The case id.</param>
/// <param name="Kind">The kind of difference.</param>
/// <param name="Observed">The observed difference as text; a deviation entry must repeat it exactly.</param>
internal sealed record Difference(string CaseId, DifferenceKind Kind, string Observed);
