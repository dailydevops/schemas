namespace NetEvolve.Schemas.Xml.Datatypes.Ordering;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;

/// <summary>The partial order of durations (XSD 1.1 Part 2, section 3.3.6): a duration is ordered against another only when every one of the four reference dateTimes agrees.</summary>
internal static class DurationOrder
{
    private static readonly XsdDateTime[] References =
    [
        Reference(1696, 9),
        Reference(1697, 2),
        Reference(1903, 3),
        Reference(1903, 7),
    ];

    internal static XsdOrder Compare(in XsdDuration a, in XsdDuration b)
    {
        // Equal months or equal seconds decide the order without the reference dateTimes: the other part is a total order.
        if (a.Seconds.Equals(b.Seconds))
        {
            return DateTimeTimeline.From(a.Months.CompareTo(b.Months));
        }

        if (a.Months == b.Months)
        {
            return DateTimeTimeline.From(a.Seconds.CompareTo(b.Seconds));
        }

        var result = XsdOrder.Incomparable;
        for (var i = 0; i < References.Length; i++)
        {
            var order = DateTimeTimeline.Compare(
                DateTimeTimeline.Add(in a, in References[i]),
                DateTimeTimeline.Add(in b, in References[i])
            );
            if (order == XsdOrder.Incomparable || (i > 0 && order != result))
            {
                return XsdOrder.Incomparable;
            }

            result = order;
        }

        return result;
    }

    private static XsdDateTime Reference(long year, int month) =>
        XsdDateTime.Create(XsdDateTimeKind.DateTime, year, month, 1, 0, 0, XsdDecimal.Zero, 0);
}
