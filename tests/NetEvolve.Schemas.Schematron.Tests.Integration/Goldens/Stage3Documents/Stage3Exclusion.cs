namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.Stage3Documents;

/// <summary>A case that is left out of the pass rate, with its reason.</summary>
/// <param name="Subject">The rule set, file, or group of files that is left out.</param>
/// <param name="Reason">Why it does not apply.</param>
public sealed record Stage3Exclusion(string Subject, string Reason);
