namespace NetEvolve.Schemas.Schematron.Xslt.Keys;

using System;
using System.Globalization;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Turns a key value into the text that the index compares with ordinal equality: the codepoint collation for strings, and the value
/// equality of <c>eq</c> for numbers and booleans (XSLT 2.0 section 16.3). A node is atomized first. A string, an untyped value, and
/// an <c>xs:anyURI</c> compare as strings; every numeric type compares as one number; a string never equals a number.
/// </summary>
/// <remarks>
/// Limitations: numbers other than <c>xs:integer</c> compare as <c>xs:double</c>, so two values that differ only beyond the 53 bits of a
/// double are one key; the types date, time, duration, and QName are not key values yet and are reported as not supported (#778).
/// </remarks>
internal static class XsltKeyValue
{
    // The largest magnitude below which every integral double has an exact long value.
    private const double ExactIntegerLimit = 9007199254740992d;

    /// <summary>Converts an item to its key text.</summary>
    /// <param name="item">The item; a node is atomized.</param>
    /// <param name="key">The key text when the result is <see cref="XsltKeyValueStatus.Ok"/>.</param>
    /// <returns>The status.</returns>
    internal static XsltKeyValueStatus TryGetKey(in XdmItem item, out string key)
    {
        key = string.Empty;
        if (item.TryGetNode(out var node))
        {
            return TryGetKey(node.TypedValue(), out key);
        }

        if (item.TryGetString(out var text) || item.TryGetUntypedAtomic(out text) || item.TryGetAnyUri(out text))
        {
            key = "S" + text;
            return XsltKeyValueStatus.Ok;
        }

        if (item.TryGetInteger(out var integer))
        {
            key = "N" + integer.ToString(CultureInfo.InvariantCulture);
            return XsltKeyValueStatus.Ok;
        }

        if (item.TryGetBoolean(out var boolean))
        {
            key = boolean ? "B1" : "B0";
            return XsltKeyValueStatus.Ok;
        }

        return item.Kind switch
        {
            XdmItemKind.XsDecimal when item.TryGetDecimal(out var number) => Number(
                double.Parse(number.ToString(), CultureInfo.InvariantCulture),
                out key
            ),
            XdmItemKind.XsDouble when item.TryGetDouble(out var number) => Number(number, out key),
            XdmItemKind.XsFloat when item.TryGetFloat(out var number) => Number(number, out key),
            _ => XsltKeyValueStatus.Unsupported,
        };
    }

    private static XsltKeyValueStatus Number(double number, out string key)
    {
        key = string.Empty;
        if (double.IsNaN(number))
        {
            return XsltKeyValueStatus.NeverEqual;
        }

        // An integral value takes the text of the xs:integer of the same value, so 1, 1.0, and 1e0 are one key.
        key =
            "N"
            + (
                Math.Abs(number) < ExactIntegerLimit && number == Math.Floor(number)
                    ? ((long)number).ToString(CultureInfo.InvariantCulture)
                    : number.ToString("R", CultureInfo.InvariantCulture)
            );
        return XsltKeyValueStatus.Ok;
    }
}
