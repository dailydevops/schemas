namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

/// <summary>The expected result of a case: the element name of the assertion and, for <c>error</c>, its code.</summary>
/// <param name="Kind">The local name of the assertion element, for example <c>assert-true</c> or <c>any-of</c>.</param>
/// <param name="ErrorCode">The <c>code</c> attribute of an <c>error</c> assertion.</param>
/// <param name="Value">The text of the assertion element: the expression, the count, or the string value.</param>
/// <param name="NormalizeSpace">The <c>normalize-space</c> attribute of an <c>assert-string-value</c> assertion.</param>
internal sealed record Qt3Assertion(
    string Kind,
    string? ErrorCode = null,
    string Value = "",
    bool NormalizeSpace = false
)
{
    /// <summary>Gets a value indicating whether the static runner checks this kind; all others are skipped.</summary>
    public bool IsActive => Kind is "error" or "assert-true" or "assert-false" or "assert-empty";

    /// <summary>Gets a value indicating whether the evaluated checker decides this kind; not decided yet.</summary>
    public bool IsEvaluated => false;
}
