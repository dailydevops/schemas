namespace NetEvolve.Schemas.XPath.Formatting.Integers;

using System.Numerics;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Formatting.Numbers;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>format-integer</c> in two arities (Functions and Operators 3.1, 4.6): the integer formatted with a picture string. The empty
/// sequence gives the zero-length string. The picture is read by <see cref="IntegerPicture"/>, the digits are written by
/// <see cref="IntegerFormatter"/>.
/// </summary>
/// <remarks>
/// Limitations: the language is not used, every language is English, which the specification permits; the output does not depend on the
/// culture of the operating system.
/// </remarks>
internal sealed class FormatIntegerFunction : XPathFunction
{
    /// <summary>Initializes a new instance of the <see cref="FormatIntegerFunction"/> class.</summary>
    public FormatIntegerFunction()
        : base(
            new XPathQName(XPathStaticContext.Default.DefaultFunctionNamespace, "format-integer"),
            2,
            3,
            [
                new XPathParameterType(XPathItemType.Integer, Occurrence.ZeroOrOne),
                new XPathParameterType(XPathItemType.String, Occurrence.ExactlyOne),
                new XPathParameterType(XPathItemType.String, Occurrence.ZeroOrOne),
            ]
        ) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <inheritdoc />
    /// <exception cref="XPathException">The picture string is not valid (FODF1310).</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        if (arguments[0].IsEmpty)
        {
            invocation.Result = XdmSequence.Create(XdmItem.FromString(string.Empty));
            return XPathEvaluationStatus.Ok;
        }

        _ = arguments[1][0].TryGetString(out var text);
        if (!IntegerPicture.TryParse(text!, out var picture, out var problem))
        {
            throw FormatNumberErrors.InvalidPicture(false, problem);
        }

        // The function conversion has made the argument one xs:integer.
        _ = arguments[0][0].TryGetBigInteger(out var value);
        invocation.Result = XdmSequence.Create(XdmItem.FromString(IntegerFormatter.Format(value, picture!)));
        return XPathEvaluationStatus.Ok;
    }
}
