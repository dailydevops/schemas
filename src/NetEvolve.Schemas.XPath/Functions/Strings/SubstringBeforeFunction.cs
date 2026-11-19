namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>fn:substring-before</c> (Functions and Operators 1.0, 7.5.4): the part of the first string before the first occurrence of the
/// second; the zero-length string when the second is zero-length or does not occur.
/// </summary>
internal sealed class SubstringBeforeFunction : SearchFunction
{
    /// <summary>Initializes a new instance of the <see cref="SubstringBeforeFunction"/> class.</summary>
    public SubstringBeforeFunction()
        : base("substring-before") { }

    /// <inheritdoc />
    protected override void Search(ref XPathFunctionCall invocation, string text, string value)
    {
        var index = text.AsSpan().IndexOf(value.AsSpan(), StringComparison.Ordinal);
        Return(ref invocation, index <= 0 ? string.Empty : text.Substring(0, index));
    }
}
