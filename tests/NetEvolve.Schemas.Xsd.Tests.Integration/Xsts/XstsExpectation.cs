namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

/// <summary>One <c>expected</c> element of a test.</summary>
/// <param name="Tokens">The version tokens; all of them must apply. Empty when the element has no <c>version</c> attribute.</param>
/// <param name="Validity">The expected validity as written in the suite, for example <c>valid</c> or <c>invalid</c>.</param>
internal sealed record XstsExpectation(IReadOnlyList<string> Tokens, string Validity);
