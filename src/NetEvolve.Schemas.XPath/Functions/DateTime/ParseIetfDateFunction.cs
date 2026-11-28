namespace NetEvolve.Schemas.XPath.Functions.DateTime;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:parse-ietf-date</c> (Functions and Operators 3.1, 9.7.1): parses the date and time formats of internet protocols
/// (RFC 5322 and its predecessors) into an <c>xs:dateTime</c> with a time zone. The empty sequence gives the empty sequence. The
/// grammar is read by <see cref="IetfDateParser"/> and does not depend on the culture. The function exists for XPath 3.0 and 3.1.
/// </summary>
internal sealed class ParseIetfDateFunction : DateTimeFunction
{
    /// <summary>Initializes a new instance of the <see cref="ParseIetfDateFunction"/> class.</summary>
    public ParseIetfDateFunction()
        : base("parse-ietf-date", 1, 1, [new XPathParameterType(XPathItemType.String, Occurrence.ZeroOrOne)]) { }

    /// <summary>Gets the language versions in which the function is visible: XPath 3.0 and 3.1.</summary>
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <inheritdoc />
    /// <exception cref="XPathException">The input does not match the grammar, or is not a valid date and time (FORG0010).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        if (arguments[0].IsEmpty)
        {
            return XPathEvaluationStatus.Ok;
        }

        _ = arguments[0][0].TryGetString(out var text);
        if (!IetfDateParser.TryParse(text!, out var lexical))
        {
            throw XPathErrors.Exception(XPathErrorCodes.FORG0010);
        }

        var parsed = XsdDateTime.Parse(lexical, XsdDateTimeKind.DateTime, XsdVersion.V10);
        if (!parsed.Success)
        {
            throw XPathErrors.Exception(XPathErrorCodes.FORG0010);
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromDateTime(parsed.Value));
        return XPathEvaluationStatus.Ok;
    }
}
