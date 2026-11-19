namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Schemas.Xml.Text.CharClasses;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// <c>fn:codepoints-to-string</c> (Functions and Operators 1.0, 7.2.3): the string of the code points of the argument. A value that is not
/// a character of XML (production 2: a control character other than tab, line feed, and carriage return, a surrogate, U+FFFE, U+FFFF,
/// or a value above U+10FFFF) is FOCH0001.
/// </summary>
internal sealed class CodepointsToStringFunction : TextFunction
{
    private const int InitialBuffer = 256;

    /// <summary>Initializes a new instance of the <see cref="CodepointsToStringFunction"/> class.</summary>
    public CodepointsToStringFunction()
        : base("codepoints-to-string", 1, 1, [Integers]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var codePoints = invocation.Arguments[0];
        Span<char> initial = stackalloc char[InitialBuffer];
        var builder = new TextBuilder(initial, invocation.Context.Limits);
        try
        {
            for (var i = 0; i < codePoints.Count; i++)
            {
                // An integer beyond 64 bits has no value to read and is out of range as well.
                if (
                    !codePoints[i].TryGetInteger(out var value)
                    || value < 0
                    || value > 0x10FFFF
                    || !XmlCharClass.IsChar((int)value)
                )
                {
                    throw XPathErrors.Exception(XPathErrorCodes.FOCH0001);
                }

                builder.AppendCodePoint((int)value);
            }

            Return(ref invocation, builder.ToString());
            return XPathEvaluationStatus.Ok;
        }
        finally
        {
            builder.Dispose();
        }
    }
}
