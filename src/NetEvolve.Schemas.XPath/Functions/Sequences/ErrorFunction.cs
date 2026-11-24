namespace NetEvolve.Schemas.XPath.Functions.Sequences;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// <c>fn:error</c> (Functions and Operators 1.0, 3.1.1): raises the error of the given code, <c>FOER0000</c> when there is none. The
/// description becomes the message; the error object has no carrier in <see cref="XPathException"/> and is ignored.
/// </summary>
internal sealed class ErrorFunction : SequenceFunction
{
    /// <summary>Initializes a new instance of the <see cref="ErrorFunction"/> class.</summary>
    public ErrorFunction()
        : base("error", 0, 3, [OptionalAtomic, Collation, Items]) { }

    /// <inheritdoc />
    /// <exception cref="XPathException">Always: the given code, or FOER0000; XPTY0004 when the code is no <c>xs:QName</c>.</exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        var code = XPathErrorCodes.FOER0000;
        if (arguments.Length > 0 && !arguments[0].IsEmpty)
        {
            if (!arguments[0][0].TryGetQName(out var name))
            {
                throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
            }

            code = new XPathErrorCode(name!.Name.NamespaceName ?? string.Empty, name.Name.LocalName);
        }

        // A code of the caller's own namespace is no engine condition: the function raised it, so it is a function error.
        var kind = XPathErrorCodes.KindOf(code);
        kind = kind == XPathErrorKind.Engine ? XPathErrorKind.Function : kind;
        var message = arguments.Length > 1 && arguments[1][0].TryGetString(out var description) ? description! : null;
        throw new XPathException(
            message is null
                ? new XPathDiagnostic(
                    code,
                    kind,
                    XPathErrorCodes.TryGetMessage(code, out var known) ? known : code.ToString(),
                    -1,
                    0
                )
                : new XPathDiagnostic(code, kind, message, -1, 0)
        );
    }
}
