namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>fn:substring-after</c> (Functions and Operators 1.0, 7.5.5): the part of the first string after the first occurrence of the second;
/// the whole first string when the second is zero-length, and the zero-length string when the second does not occur.
/// </summary>
internal sealed class SubstringAfterFunction : SearchFunction
{
    /// <summary>Initializes a new instance of the <see cref="SubstringAfterFunction"/> class.</summary>
    public SubstringAfterFunction()
        : base("substring-after") { }

    /// <inheritdoc />
    protected override void Search(ref XPathFunctionCall invocation, string text, string value)
    {
        var index = text.AsSpan().IndexOf(value.AsSpan(), StringComparison.Ordinal);
        if (index < 0)
        {
            Return(ref invocation, string.Empty);
        }
        else
        {
            Return(ref invocation, value.Length == 0 ? text : text.Substring(index + value.Length));
        }
    }
}
