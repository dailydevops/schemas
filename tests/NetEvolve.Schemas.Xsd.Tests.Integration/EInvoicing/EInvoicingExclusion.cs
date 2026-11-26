namespace NetEvolve.Schemas.Xsd.Tests.Integration.EInvoicing;

/// <summary>A document that is left out of the run, with its reason.</summary>
/// <param name="Id">The case id: <c>&lt;set name&gt;/&lt;file name&gt;</c>.</param>
/// <param name="Reason">Why it does not apply.</param>
public sealed record EInvoicingExclusion(string Id, string Reason);
