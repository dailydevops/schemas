namespace NetEvolve.Schemas.XPath.Tests.Unit.Xdm.Values;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class XdmValueTests
{
    [Test]
    public async Task Singleton_CreateAndEnumerate_AllocatesNothing()
    {
        var item = XdmItem.FromInteger(7);
        long sum = 0;

        AllocationAssert.NoAllocation(() =>
        {
            var sequence = XdmSequence.Create(item);
            foreach (var current in sequence)
            {
                _ = current.TryGetInteger(out var value);
                sum += value;
            }
        });

        await Assert.That(sum).IsGreaterThan(0L);
    }

    [Test]
    public async Task Empty_IsTheSameWhereverObtained_AndEnumeratesNothing()
    {
        var created = XdmSequence.Create(ReadOnlySpan<XdmItem>.Empty);
        var concatenated = XdmSequence.Concat(XdmSequence.Empty, XdmSequence.Empty);

        await Assert.That(XdmSequence.Empty == default).IsTrue();
        await Assert.That(created == XdmSequence.Empty).IsTrue();
        await Assert.That(concatenated == XdmSequence.Empty).IsTrue();
        await Assert.That(XdmSequence.Empty.Count).IsEqualTo(0);

        var enumerator = XdmSequence.Empty.GetEnumerator();
        await Assert.That(enumerator.MoveNext()).IsFalse();
        AllocationAssert.NoAllocation(() => XdmSequence.Empty.GetEnumerator().MoveNext());
    }

    [Test]
    public async Task Atomic_PerPrimitiveType_RoundTripsValueAndReportsType()
    {
        var dec = new XsdDecimal(12345, 2);
        var dateTime = XsdDateTime.Parse("2024-05-06T07:08:09Z", XsdDateTimeKind.DateTime, XsdVersion.V11).Value;
        var date = XsdDateTime.Parse("2024-05-06", XsdDateTimeKind.Date, XsdVersion.V11).Value;
        var time = XsdDateTime.Parse("07:08:09", XsdDateTimeKind.Time, XsdVersion.V11).Value;
        var duration = XsdDuration.Parse("P1Y2M3DT4H5M6S").Value;
        var yearMonth = XsdDuration.Parse("P1Y2M", DurationKind.YearMonth).Value;
        var dayTime = XsdDuration.Parse("P3DT4H", DurationKind.DayTime).Value;

        await Assert.That(XdmItem.FromBoolean(true).TryGetBoolean(out var b) && b).IsTrue();
        await Assert.That(XdmItem.FromBoolean(true).TypeId).IsEqualTo(XdmTypeId.Boolean);
        await Assert.That(XdmItem.FromInteger(-5).TryGetInteger(out var i) && i == -5).IsTrue();
        await Assert.That(XdmItem.FromString("x").TryGetString(out var s) && s == "x").IsTrue();
        await Assert.That(XdmItem.FromDouble(1.5).TryGetDouble(out var d) && d == 1.5).IsTrue();
        await Assert.That(XdmItem.FromFloat(2.5f).TryGetFloat(out var f) && f == 2.5f).IsTrue();
        await Assert.That(XdmItem.FromFloat(2.5f).Kind).IsEqualTo(XdmItemKind.XsFloat);
        await Assert.That(XdmItem.FromFloat(2.5f).TypeId).IsEqualTo(XdmTypeId.XsFloat);
        await Assert.That(XdmItem.FromFloat(float.PositiveInfinity).TryGetFloat(out var inf) && float.IsInfinity(inf)).IsTrue();

        var decimalItem = XdmItem.FromDecimal(dec);
        await Assert.That(decimalItem.Kind).IsEqualTo(XdmItemKind.XsDecimal);
        await Assert.That(decimalItem.TypeId).IsEqualTo(XdmTypeId.XsDecimal);
        await Assert.That(decimalItem.TryGetDecimal(out var dv) && dv == dec).IsTrue();

        await AssertDateTime(dateTime, XdmItemKind.DateTime, XdmTypeId.DateTime);
        await AssertDateTime(date, XdmItemKind.Date, XdmTypeId.Date);
        await AssertDateTime(time, XdmItemKind.Time, XdmTypeId.Time);
        await AssertDuration(duration, DurationKind.Duration, XdmItemKind.Duration, XdmTypeId.Duration);
        await AssertDuration(yearMonth, DurationKind.YearMonth, XdmItemKind.YearMonthDuration, XdmTypeId.YearMonthDuration);
        await AssertDuration(dayTime, DurationKind.DayTime, XdmItemKind.DayTimeDuration, XdmTypeId.DayTimeDuration);
    }

    private static async Task AssertDateTime(XsdDateTime value, XdmItemKind kind, XdmTypeId typeId)
    {
        var item = XdmItem.FromDateTime(value);
        await Assert.That(item.Kind).IsEqualTo(kind);
        await Assert.That(item.TypeId).IsEqualTo(typeId);
        await Assert.That(item.TryGetDateTime(out var back) && back == value).IsTrue();
        await Assert.That(item.TryGetDecimal(out _)).IsFalse();
    }

    private static async Task AssertDuration(XsdDuration value, DurationKind durationKind, XdmItemKind kind, XdmTypeId typeId)
    {
        var item = XdmItem.FromDuration(value, durationKind);
        await Assert.That(item.Kind).IsEqualTo(kind);
        await Assert.That(item.TypeId).IsEqualTo(typeId);
        await Assert.That(item.TryGetDuration(out var back) && back == value).IsTrue();
        await Assert.That(item.TryGetDateTime(out _)).IsFalse();
    }

    [Test]
    public async Task Atomic_EqualValues_AreEqualWithEqualHash()
    {
        var left = XdmItem.FromDecimal(new XsdDecimal(100, 2));
        var right = XdmItem.FromDecimal(new XsdDecimal(1, 0));

        await Assert.That(left == right).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        await Assert.That(left == XdmItem.FromDecimal(new XsdDecimal(2, 0))).IsFalse();
    }

    [Test]
    public async Task Atomic_Factories_RejectInvalidArguments()
    {
        await Assert.That(() => XdmItem.FromDuration(default, (DurationKind)99)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => XdmItem.FromDuration(default, (DurationKind)(-1))).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task UntypedAtomic_IsDistinctFromString()
    {
        var untyped = XdmItem.FromUntypedAtomic("a");
        var text = XdmItem.FromString("a");

        await Assert.That(untyped.Kind).IsEqualTo(XdmItemKind.UntypedAtomic);
        await Assert.That(untyped.TypeId).IsNotEqualTo(text.TypeId);
        await Assert.That(untyped == text).IsFalse();
        await Assert.That(untyped.TryGetString(out _)).IsFalse();
        await Assert.That(text.TryGetUntypedAtomic(out _)).IsFalse();
        await Assert.That(untyped.TryGetUntypedAtomic(out var value) && value == "a").IsTrue();
    }

    [Test]
    public async Task Lazy_OverCountingSource_IsEnumeratedOnceWhenReadTwice()
    {
        var source = new CountingSource();
        var sequence = XdmSequence.FromLazy(source);

        var first = Sum(sequence);
        var second = Sum(sequence);

        await Assert.That(first).IsEqualTo(6L);
        await Assert.That(second).IsEqualTo(6L);
        await Assert.That(sequence.Count).IsEqualTo(3);
        await Assert.That(sequence.Materialize().Count).IsEqualTo(3);
        await Assert.That(source.Enumerations).IsEqualTo(1);
    }

    [Test]
    public async Task Lazy_NotRead_DoesNotTouchTheSource()
    {
        var source = new CountingSource();
        _ = XdmSequence.FromLazy(source);

        await Assert.That(source.Enumerations).IsEqualTo(0);
    }

    [Test]
    public async Task Concat_FlattensAndNeverNests()
    {
        var left = XdmSequence.Create([XdmItem.FromInteger(1), XdmItem.FromInteger(2)]);
        var right = XdmSequence.Create(XdmItem.FromInteger(3));

        var all = XdmSequence.Concat(XdmSequence.Concat(left, XdmSequence.Empty), right);

        await Assert.That(all.Count).IsEqualTo(3);
        await Assert.That(Sum(all)).IsEqualTo(6L);
        await Assert.That(all[2] == XdmItem.FromInteger(3)).IsTrue();
        await Assert.That(XdmSequence.Concat(XdmSequence.Empty, right) == right).IsTrue();
        await Assert.That(XdmSequence.Concat(left, XdmSequence.FromLazy(new CountingSource())).Count).IsEqualTo(5);
    }

    private static long Sum(XdmSequence sequence)
    {
        long sum = 0;
        foreach (var item in sequence)
        {
            _ = item.TryGetInteger(out var value);
            sum += value;
        }

        return sum;
    }

    private sealed class CountingSource : IEnumerable<XdmItem>
    {
        public int Enumerations { get; private set; }

        public IEnumerator<XdmItem> GetEnumerator()
        {
            Enumerations++;
            yield return XdmItem.FromInteger(1);
            yield return XdmItem.FromInteger(2);
            yield return XdmItem.FromInteger(3);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
