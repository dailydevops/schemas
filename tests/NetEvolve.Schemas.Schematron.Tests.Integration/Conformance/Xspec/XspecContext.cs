namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

/// <summary>The instance document of a scenario: a file, or the serialized element written inline.</summary>
/// <param name="Path">The full path of the instance file, or <see langword="null"/> for an inline instance.</param>
/// <param name="Xml">The serialized inline instance, or <see langword="null"/> for an instance file.</param>
internal sealed record XspecContext(string? Path, string? Xml);
