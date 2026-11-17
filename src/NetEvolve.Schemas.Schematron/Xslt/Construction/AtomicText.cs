namespace NetEvolve.Schemas.Schematron.Xslt.Construction;

using System.Globalization;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The string value of an atomic item, for adjacent atomic values in tree content (XSLT 2.0 section 5.7.2).</summary>
/// <remarks>
/// Limitation: only string, untyped atomic, boolean, integer, decimal, date and time, and duration items convert. Double,
/// float, and the remaining kinds need the XPath casting rules, which the XPath package does not offer yet; they report a
/// problem instead of a guessed lexical form.
/// </remarks>
internal static class AtomicText
{
    internal static bool TryGet(in XdmItem item, out string text)
    {
        switch (item.Kind)
        {
            case XdmItemKind.XsString:
                _ = item.TryGetString(out var s);
                text = s!;
                return true;
            case XdmItemKind.UntypedAtomic:
                _ = item.TryGetUntypedAtomic(out var u);
                text = u!;
                return true;
            case XdmItemKind.Boolean:
                _ = item.TryGetBoolean(out var b);
                text = b ? "true" : "false";
                return true;
            case XdmItemKind.XsInteger:
                _ = item.TryGetInteger(out var i);
                text = i.ToString(CultureInfo.InvariantCulture);
                return true;
            case XdmItemKind.XsDecimal:
                _ = item.TryGetDecimal(out var d);
                text = d.ToString();
                return true;
            case XdmItemKind.DateTime:
            case XdmItemKind.Date:
            case XdmItemKind.Time:
                _ = item.TryGetDateTime(out var t);
                text = t.ToString();
                return true;
            case XdmItemKind.Duration:
            case XdmItemKind.YearMonthDuration:
            case XdmItemKind.DayTimeDuration:
                _ = item.TryGetDuration(out var u2);
                text = u2.ToString(
                    item.Kind == XdmItemKind.YearMonthDuration ? DurationKind.YearMonth
                    : item.Kind == XdmItemKind.DayTimeDuration ? DurationKind.DayTime
                    : DurationKind.Duration
                );
                return true;
            default:
                text = string.Empty;
                return false;
        }
    }
}
