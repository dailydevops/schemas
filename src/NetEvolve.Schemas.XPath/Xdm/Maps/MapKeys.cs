namespace NetEvolve.Schemas.XPath.Xdm.Maps;

using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.XPath.Evaluation.Temporal;
using NetEvolve.Schemas.XPath.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The equality of two map keys, <c>op:same-key</c> (Functions and Operators 3.1, 17.1.1), and the hash that agrees with it: both go
/// through the same canonical form, so two keys that are the same key always have the same hash.
/// </summary>
/// <remarks>
/// <para>
/// Numbers are the same key when they are equal after type promotion, and NaN is the same key as NaN. <c>xs:string</c>,
/// <c>xs:untypedAtomic</c>, and <c>xs:anyURI</c> are one group compared by codepoint. Two date and time values of the same type are the
/// same key when they have the same instant and either both or neither have a timezone, so a value without a timezone is never the same
/// key as a value with one (the implicit timezone plays no part). The three duration types are compared by their months and seconds,
/// binary values by type and octets, and QNames by their expanded names. Limitation: comparing and hashing a date and time key that has a timezone allocates (the timeline value is a decimal); a key without a timezone does not. Values of different families are different keys.
/// </para>
/// </remarks>
internal static class MapKeys
{
    /// <summary>Determines whether two atomic values are the same key.</summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns><see langword="true"/> when the keys are the same key.</returns>
    public static bool SameKey(in XdmItem left, in XdmItem right)
    {
        if (left.TryGetTemporal(out var a))
        {
            return right.TryGetTemporal(out var b) && left.Kind == right.Kind && SameMoment(in a, in b);
        }

        if (left.TryGetDuration(out var x))
        {
            return right.TryGetDuration(out var y) && x.Equals(y);
        }

        if (left.TryGetBinary(out var p))
        {
            return right.TryGetBinary(out var q) && left.Kind == right.Kind && p.Equals(q);
        }

        if (left.TryGetQName(out var m))
        {
            return right.TryGetQName(out var n) && m!.Equals(n);
        }

        return AtomicEquality.AreEqual(in left, in right, nanEqual: true);
    }

    private static bool SameMoment(in XsdDateTime a, in XsdDateTime b)
    {
        var zoned = a.TimezoneOffsetMinutes.HasValue;
        if (zoned != b.TimezoneOffsetMinutes.HasValue)
        {
            return false;
        }

        // Without a timezone the fields decide and nothing is allocated; with one, both values go to the timeline.
        return zoned ? TemporalValues.Instant(in a, 0).CompareTo(TemporalValues.Instant(in b, 0)) == 0 : a.Equals(b);
    }

    private static int MomentHash(in XsdDateTime value) =>
        value.TimezoneOffsetMinutes.HasValue
            ? unchecked(((int)value.Kind * 31) + TemporalValues.Instant(in value, 0).GetHashCode())
            : value.GetHashCode();

    /// <summary>Gets the hash of a key; the same for every key that <see cref="SameKey"/> finds the same.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The hash code.</returns>
    public static int Hash(in XdmItem key)
    {
        if (key.TryGetTemporal(out var value))
        {
            return MomentHash(in value);
        }

        if (key.TryGetDuration(out var duration))
        {
            return duration.GetHashCode();
        }

        if (key.TryGetBinary(out var binary))
        {
            return unchecked(((int)key.Kind * 31) + binary.GetHashCode());
        }

        return key.TryGetQName(out var name) ? name!.GetHashCode() : AtomicEquality.HashCode(in key);
    }
}
