namespace NetEvolve.Schemas.Xsd.Tests.Integration.EInvoicing;

/// <summary>A case that does not pass yet, linked to the issue that covers it.</summary>
/// <param name="Id">The case id: <c>&lt;set name&gt;/&lt;file name&gt;</c>, or <c>&lt;set name&gt;/schemas</c> for the build.</param>
/// <param name="Issue">The number of the issue that covers the failure.</param>
/// <param name="Reason">What fails.</param>
public sealed record EInvoicingKnownFailure(string Id, int Issue, string Reason);
