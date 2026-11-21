namespace NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>One accepted difference of the deviation list.</summary>
/// <param name="CaseId">The case id.</param>
/// <param name="Kind">The kind of difference.</param>
/// <param name="Observed">The observed difference exactly as the comparison reports it.</param>
/// <param name="SpecificationReference">The place in the XML Schema specification that decides the case.</param>
/// <param name="Reason">Why this library differs from the baseline.</param>
internal sealed record DeviationEntry(
    string CaseId,
    DifferenceKind Kind,
    string Observed,
    string SpecificationReference,
    string Reason
);
