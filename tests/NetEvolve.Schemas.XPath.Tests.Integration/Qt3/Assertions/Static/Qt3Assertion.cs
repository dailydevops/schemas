namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

/// <summary>The expected result of a case: the element name of the assertion, its code, and its text.</summary>
/// <param name="Kind">The local name of the assertion element, for example <c>assert-true</c> or <c>any-of</c>.</param>
/// <param name="ErrorCode">The <c>code</c> attribute of an <c>error</c> assertion.</param>
/// <param name="Value">The text of the assertion element: the expression, the count, or the string value.</param>
/// <param name="NormalizeSpace">The <c>normalize-space</c> attribute of an <c>assert-string-value</c> assertion.</param>
/// <param name="File">The path of the file attribute of an <c>assert-xml</c> assertion; the expected XML is then in that file.</param>
/// <param name="IgnorePrefixes">The <c>ignore-prefixes</c> attribute of an <c>assert-xml</c> assertion.</param>
internal sealed record Qt3Assertion(
    string Kind,
    string? ErrorCode = null,
    string Value = "",
    bool NormalizeSpace = false,
    string? File = null,
    bool IgnorePrefixes = false
)
{
    /// <summary>Gets a value indicating whether the static checker decides this kind from the result alone.</summary>
    public bool IsActive => Kind is "error" or "assert-true" or "assert-false" or "assert-empty";

    /// <summary>Gets a value indicating whether the evaluated checker decides this kind, which may ask the engine to evaluate an expression.</summary>
    public bool IsEvaluated => Kind is "assert" or "assert-eq" or "assert-count" or "assert-string-value";

    /// <summary>Gets a value indicating whether the structural checker decides this kind: it compares the result with an expression, a sequence type, or serialized XML.</summary>
    public bool IsStructural => Kind is "assert-deep-eq" or "assert-permutation" or "assert-type" or "assert-xml";
}
