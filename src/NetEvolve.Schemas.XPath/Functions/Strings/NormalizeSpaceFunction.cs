namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Limits;

/// <summary>
/// <c>fn:normalize-space</c> (Functions and Operators 1.0, 7.4.6): the argument with leading and trailing white space removed and every
/// inner run of white space replaced by one space. White space is the four characters of XML (space, tab, line feed, carriage return);
/// without an argument the string value of the context item is normalized. A string that needs no change is returned as it is.
/// </summary>
internal sealed class NormalizeSpaceFunction : TextFunction
{
    private const int InitialBuffer = 256;

    /// <summary>Initializes a new instance of the <see cref="NormalizeSpaceFunction"/> class.</summary>
    public NormalizeSpaceFunction()
        : base("normalize-space", 0, 1, [OptionalString]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var text = invocation.Arguments.Length == 0 ? ContextText(ref invocation) : Text(invocation.Arguments[0]);
        Return(ref invocation, IsNormalized(text) ? text : Normalize(text, invocation.Context.Limits));
        return XPathEvaluationStatus.Ok;
    }

    // Normalized: no white space but single spaces between other characters.
    private static bool IsNormalized(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (!WhitespaceNormalizer.IsXmlSpace(text[i]))
            {
                continue;
            }

            if (text[i] != ' ' || i == 0 || i == text.Length - 1 || WhitespaceNormalizer.IsXmlSpace(text[i + 1]))
            {
                return false;
            }
        }

        return true;
    }

    private static string Normalize(string text, XPathLimits limits)
    {
        Span<char> initial = stackalloc char[InitialBuffer];
        var builder = new TextBuilder(initial, limits);
        try
        {
            var pending = false;
            foreach (var unit in text)
            {
                if (WhitespaceNormalizer.IsXmlSpace(unit))
                {
                    pending = builder.Length > 0;
                    continue;
                }

                if (pending)
                {
                    builder.Append(' ');
                    pending = false;
                }

                builder.Append(unit);
            }

            return builder.ToString();
        }
        finally
        {
            builder.Dispose();
        }
    }
}
