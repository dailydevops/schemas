namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The base of <c>fn:contains</c>, <c>fn:starts-with</c>, and <c>fn:ends-with</c>: the result is a boolean. Allocates nothing.</summary>
internal abstract class SearchTestFunction : SearchFunction
{
    /// <summary>Initializes a new instance of the <see cref="SearchTestFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="localName"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    protected SearchTestFunction(string localName)
        : base(localName) { }

    /// <inheritdoc />
    protected sealed override void Search(ref XPathFunctionCall invocation, string text, string value) =>
        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(Test(text.AsSpan(), value.AsSpan())));

    /// <summary>Tests the text against the value.</summary>
    /// <param name="text">The first argument.</param>
    /// <param name="value">The second argument.</param>
    /// <returns>The result of the function.</returns>
    protected abstract bool Test(ReadOnlySpan<char> text, ReadOnlySpan<char> value);
}
