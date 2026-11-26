namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage3Documents;

/// <summary>A case that does not pass yet, linked to the issue that covers it.</summary>
/// <param name="Id">The case id: <c>&lt;rule set name&gt;/&lt;file name&gt;</c>.</param>
/// <param name="Issue">The number of the issue that covers the failure.</param>
/// <param name="Reason">What fails.</param>
public sealed record Stage3KnownFailure(string Id, int Issue, string Reason);
