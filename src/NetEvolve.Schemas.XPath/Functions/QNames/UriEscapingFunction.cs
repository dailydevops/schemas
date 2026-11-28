namespace NetEvolve.Schemas.XPath.Functions.QNames;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The base of <c>fn:encode-for-uri</c>, <c>fn:iri-to-uri</c>, and <c>fn:escape-html-uri</c> (<c>xs:string?</c> in, <c>xs:string</c> out): the empty sequence gives
/// the zero-length string, and a string without a character to escape comes back as the same string.
/// </summary>
internal sealed class UriEscapingFunction : QNameFunction
{
    private readonly Func<string, string> _escape;

    /// <summary>Initializes a new instance of the <see cref="UriEscapingFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <param name="escape">The escaping of a string.</param>
    /// <exception cref="ArgumentNullException"><paramref name="localName"/> or <paramref name="escape"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="localName"/> is empty.</exception>
    public UriEscapingFunction(string localName, Func<string, string> escape)
        : base(localName, 1, 1, [OptionalString])
    {
        ArgumentNullException.ThrowIfNull(escape);

        _escape = escape;
    }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        invocation.Result = XdmSequence.Create(XdmItem.FromString(_escape(Text(invocation.Arguments[0]))));
        return XPathEvaluationStatus.Ok;
    }
}
