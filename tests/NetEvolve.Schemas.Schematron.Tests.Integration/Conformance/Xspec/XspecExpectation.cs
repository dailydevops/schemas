namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

/// <summary>One declarative expectation of an XSpec scenario.</summary>
/// <param name="Kind">The kind of the expectation.</param>
/// <param name="Id">The expected id of the assertion, or of its rule or pattern when the assertion has none.</param>
/// <param name="Role">The expected role of the assertion, or of its rule or pattern when the assertion has none.</param>
/// <param name="Location">The XPath expression that selects the node the assertion must be located at.</param>
/// <param name="Count">The exact number of matches, if the expectation states one.</param>
/// <param name="Text">The normalized text one of the message parts must equal, if the expectation has content.</param>
/// <param name="Namespaces">The namespace bindings in scope of the expectation, for the location expression.</param>
internal sealed record XspecExpectation(
    XspecExpectKind Kind,
    string? Id,
    string? Role,
    string? Location,
    int? Count,
    string? Text,
    IReadOnlyDictionary<string, string> Namespaces
);
