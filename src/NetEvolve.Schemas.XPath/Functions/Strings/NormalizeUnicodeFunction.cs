namespace NetEvolve.Schemas.XPath.Functions.Strings;

using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>
/// <c>fn:normalize-unicode</c> (Functions and Operators 1.0, 7.4.5): the argument in the normalization form of the second argument,
/// NFC by default. The form name is trimmed of white space and compared without regard to the case of the ASCII letters; the
/// zero-length form means no normalization; any other form, the fully-normalized form included (DEF-75, #87), is FOCH0003, which is
/// raised before the argument is read, so it is also raised for the empty sequence.
/// </summary>
internal sealed class NormalizeUnicodeFunction : TextFunction
{
    /// <summary>Initializes a new instance of the <see cref="NormalizeUnicodeFunction"/> class.</summary>
    public NormalizeUnicodeFunction()
        : base("normalize-unicode", 1, 2, [OptionalString, OneString]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var form = UnicodeNormalizationForm.Nfc;
        var none = false;
        if (invocation.Arguments.Length > 1)
        {
            var name = Text(invocation.Arguments[1]).AsSpan();
            none = IsBlank(name);
            if (!none && !UnicodeNormalization.TryParseForm(name, out form))
            {
                throw XPathErrors.Exception(XPathErrorCodes.FOCH0003);
            }
        }

        var text = Text(invocation.Arguments[0]);
        Return(ref invocation, none ? text : UnicodeNormalization.Normalize(text, form, invocation.Context.Limits));
        return XPathEvaluationStatus.Ok;
    }

    private static bool IsBlank(System.ReadOnlySpan<char> name)
    {
        foreach (var unit in name)
        {
            if (!WhitespaceNormalizer.IsXmlSpace(unit))
            {
                return false;
            }
        }

        return true;
    }
}
