namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage3Documents;

/// <summary>One sample invoice with its expected result.</summary>
/// <param name="Document">The path of the invoice relative to the suite root.</param>
/// <param name="Valid">Whether the invoice is valid: no failed assertion and no successful report.</param>
/// <param name="Rules">The ids of the assertions that fire, compared as a set.</param>
public sealed record Stage3Document(string Document, bool Valid, IReadOnlyList<string> Rules);
