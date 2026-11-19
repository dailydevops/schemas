namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Derived;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Casting.Other;
using NetEvolve.Schemas.XPath.Casting.Temporal;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The built-in derived types of the casting tests and a text form of an item that tells the type annotation and the value apart.</summary>
internal static class DerivedFixture
{
    /// <summary>The derived integer types with their inclusive bounds, <see langword="null"/> when a side is unbounded.</summary>
    public static readonly (string Name, BigInteger? Min, BigInteger? Max)[] IntegerTypes =
    [
        ("nonPositiveInteger", null, 0),
        ("negativeInteger", null, -1),
        ("long", long.MinValue, long.MaxValue),
        ("int", int.MinValue, int.MaxValue),
        ("short", short.MinValue, short.MaxValue),
        ("byte", sbyte.MinValue, sbyte.MaxValue),
        ("nonNegativeInteger", 0, null),
        ("unsignedLong", 0, ulong.MaxValue),
        ("unsignedInt", 0, uint.MaxValue),
        ("unsignedShort", 0, ushort.MaxValue),
        ("unsignedByte", 0, byte.MaxValue),
        ("positiveInteger", 1, null),
    ];

    /// <summary>The derived string types.</summary>
    public static readonly string[] StringTypes =
    [
        "normalizedString",
        "token",
        "language",
        "Name",
        "NCName",
        "ID",
        "IDREF",
        "ENTITY",
        "NMTOKEN",
    ];

    /// <summary>The value of every bound of the integer types and of the value just outside it, with whether the value is inside.</summary>
    public static IEnumerable<Func<(string Type, string Value, bool Inside)>> Bounds()
    {
        foreach (var (name, min, max) in IntegerTypes)
        {
            if (min is { } lower)
            {
                var low = (name, lower.ToString(CultureInfo.InvariantCulture), true);
                var below = (name, (lower - 1).ToString(CultureInfo.InvariantCulture), false);
                yield return () => low;
                yield return () => below;
            }

            if (max is { } upper)
            {
                var high = (name, upper.ToString(CultureInfo.InvariantCulture), true);
                var above = (name, (upper + 1).ToString(CultureInfo.InvariantCulture), false);
                yield return () => high;
                yield return () => above;
            }
        }
    }

    /// <summary>The type annotation by name and the value of the single item of the result, or <c>count:n</c> for another cardinality.</summary>
    public static string Show(string expression)
    {
        var result = CastFixture.Evaluate(expression);
        if (result.Count != 1)
        {
            return "count:" + result.Count;
        }

        var item = result[0];
        _ =
            NumberText.TryFormat(item, out var text)
            || TemporalConversions.TryFormat(item, out text)
            || OtherConversions.TryFormat(item, out text);
        return XdmTypes.NameOf(item.TypeId) + ":" + text;
    }

    /// <summary>Whether <c>castable as</c> of the expression is true.</summary>
    public static bool Castable(string expression, string type)
    {
        var result = CastFixture.Evaluate("(" + expression + ") castable as xs:" + type);
        _ = result[0].TryGetBoolean(out var value);
        return value;
    }
}
