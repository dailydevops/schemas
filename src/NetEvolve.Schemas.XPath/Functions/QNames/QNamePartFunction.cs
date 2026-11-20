namespace NetEvolve.Schemas.XPath.Functions.QNames;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The base of <c>fn:prefix-from-QName</c>, <c>fn:local-name-from-QName</c>, and <c>fn:namespace-uri-from-QName</c> (Functions and
/// Operators 1.0, 11.2): each reads one part of an <c>xs:QName?</c> argument; the empty sequence gives the empty sequence.
/// </summary>
internal abstract class QNamePartFunction : QNameFunction
{
    /// <summary>Initializes a new instance of the <see cref="QNamePartFunction"/> class.</summary>
    /// <param name="localName">The local name.</param>
    /// <exception cref="System.ArgumentException"><paramref name="localName"/> is empty.</exception>
    protected QNamePartFunction(string localName)
        : base(localName, 1, 1, [OptionalQName]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">The argument is a value that is no QName (XPTY0004).</exception>
    public sealed override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var argument = invocation.Arguments[0];
        if (argument.IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        if (!argument[0].TryGetQName(out var qname))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        invocation.Result = Part(qname!);
        return XPathEvaluationStatus.Ok;
    }

    /// <summary>Reads the part.</summary>
    /// <param name="qname">The QName.</param>
    /// <returns>The part: one item or the empty sequence.</returns>
    protected abstract XdmSequence Part(XdmQName qname);
}
