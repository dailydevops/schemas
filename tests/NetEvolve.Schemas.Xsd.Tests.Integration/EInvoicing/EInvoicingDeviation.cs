namespace NetEvolve.Schemas.Xsd.Tests.Integration.EInvoicing;

/// <summary>A document whose verdict differs from the verdict of System.Xml.Schema on purpose, because the baseline is wrong.</summary>
/// <param name="Id">The case id: <c>&lt;set name&gt;/&lt;relative path&gt;</c>.</param>
/// <param name="Reason">Why the verdict of this library is the correct one.</param>
public sealed record EInvoicingDeviation(string Id, string Reason);
