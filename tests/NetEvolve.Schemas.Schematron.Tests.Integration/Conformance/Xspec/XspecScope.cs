namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

/// <summary>What the scenarios of one level of an XSpec file inherit from their ancestors.</summary>
/// <param name="BaseDirectory">The directory that relative references of the file resolve against.</param>
/// <param name="SchemaPath">The full path of the Schematron schema.</param>
/// <param name="Phase">The phase to validate with, if any.</param>
/// <param name="Context">The instance document, if one is in scope.</param>
/// <param name="Skip">Why everything in scope is left out, or <see langword="null"/> when it runs.</param>
/// <param name="Path">The labels of the enclosing scenarios, separated by spaces.</param>
internal sealed record XspecScope(
    string BaseDirectory,
    string SchemaPath,
    string? Phase,
    XspecContext? Context,
    string? Skip,
    string Path
);
