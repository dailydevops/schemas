namespace NetEvolve.Schemas.Xml.Tests.Unit.Text.NameTable;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;

public class SpanInterningNameTableTests
{
    [Test]
    public async Task NameTable_TwoBuffers_EqualSpans_SameInstance()
    {
        var table = new SpanInterningNameTable(100);
        var first = "xx-item-yy".ToCharArray();
        var second = "item".ToCharArray();

        _ = table.TryIntern(first.AsSpan(3, 4), out var s1);
        _ = table.TryIntern(second, out var s2);

        await Assert.That(s2).IsEqualTo(s1);
        await Assert.That(ReferenceEquals(table.GetString(s1), table.GetString(s2))).IsTrue();
        await Assert.That(ReferenceEquals(table.Lookup(second), table.GetString(s1))).IsTrue();
    }

    [Test]
    public async Task NameTable_EmptyString_IsSymbolZero_EvenWhenFrozenOrFull()
    {
        var table = new SpanInterningNameTable(1);
        table.Freeze();

        var status = table.TryIntern([], out var symbol);

        await Assert.That(status).IsEqualTo(XmlNameTableStatus.Ok);
        await Assert.That(symbol).IsEqualTo(default(XmlSymbol));
        await Assert.That(table.GetString(default)).IsEqualTo(string.Empty);
        await Assert.That(table.Count).IsEqualTo(1);
    }

    [Test]
    public async Task NameTable_Hit_AllocatesZeroBytes()
    {
        var table = new SpanInterningNameTable(100);
        var buffer = "element".ToCharArray();
        _ = table.TryIntern(buffer, out _);

        AllocationAssert.NoAllocation(() =>
        {
            _ = table.TryIntern(buffer, out _);
            _ = table.TryGet(buffer, out _);
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task NameTable_Growth_KeepsEarlierInstances()
    {
        var table = new SpanInterningNameTable(10_000, initialCapacity: 2);
        var strings = new string[500];
        var symbols = new XmlSymbol[500];
        for (var i = 0; i < strings.Length; i++)
        {
            _ = table.TryIntern(("name" + i).AsSpan(), out symbols[i]);
            strings[i] = table.GetString(symbols[i]);
        }

        for (var i = 0; i < strings.Length; i++)
        {
            _ = table.TryGet(("name" + i).AsSpan(), out var again);
            await Assert.That(again).IsEqualTo(symbols[i]);
            await Assert.That(ReferenceEquals(table.GetString(again), strings[i])).IsTrue();
        }

        await Assert.That(table.Count).IsEqualTo(501);
    }

    [Test]
    public async Task NameTable_ForcedCollisions_StayDistinct()
    {
        var table = new SpanInterningNameTable(100, 4, static _ => 42);

        _ = table.TryIntern("a", out var a);
        _ = table.TryIntern("b", out var b);
        _ = table.TryIntern("ab", out var ab);
        _ = table.TryIntern("a", out var aAgain);

        await Assert.That(a).IsNotEqualTo(b);
        await Assert.That(ab).IsNotEqualTo(a);
        await Assert.That(aAgain).IsEqualTo(a);
        await Assert.That(table.GetString(b)).IsEqualTo("b");
        await Assert.That(table.TryGet("c", out _)).IsFalse();
    }

    [Test]
    public async Task NameTable_Frozen_ConcurrentReads()
    {
        var table = new SpanInterningNameTable(1000);
        for (var i = 0; i < 100; i++)
        {
            _ = table.TryIntern(("n" + i).AsSpan(), out _);
        }

        table.Freeze();
        var failures = 0;

        _ = Parallel.For(0, 8, _ =>
        {
            for (var i = 0; i < 2000; i++)
            {
                var name = "n" + (i % 100);
                if (
                    !table.TryGet(name.AsSpan(), out var s)
                    || !string.Equals(table.GetString(s), name, StringComparison.Ordinal)
                )
                {
                    _ = System.Threading.Interlocked.Increment(ref failures);
                }
            }
        });

        await Assert.That(failures).IsEqualTo(0);
    }

    [Test]
    public async Task NameTable_Frozen_UnknownName_ReturnsFrozen_WithoutWriting()
    {
        var table = new SpanInterningNameTable(10);
        _ = table.TryIntern("known", out var known);
        table.Freeze();

        var again = table.TryIntern("known", out var knownAgain);
        var status = table.TryIntern("other", out var other);

        await Assert.That(table.IsFrozen).IsTrue();
        await Assert.That(again).IsEqualTo(XmlNameTableStatus.Ok);
        await Assert.That(knownAgain).IsEqualTo(known);
        await Assert.That(status).IsEqualTo(XmlNameTableStatus.Frozen);
        await Assert.That(other).IsEqualTo(default(XmlSymbol));
        await Assert.That(table.Count).IsEqualTo(2);
    }

    [Test]
    public async Task NameTable_LimitExceeded_ReturnsStatus()
    {
        var table = new SpanInterningNameTable(3);

        var a = table.TryIntern("a", out _);
        var b = table.TryIntern("b", out _);
        var c = table.TryIntern("c", out var symbol);
        var hit = table.TryIntern("a", out _);

        await Assert.That(a).IsEqualTo(XmlNameTableStatus.Ok);
        await Assert.That(b).IsEqualTo(XmlNameTableStatus.Ok);
        await Assert.That(c).IsEqualTo(XmlNameTableStatus.LimitExceeded);
        await Assert.That(symbol).IsEqualTo(default(XmlSymbol));
        await Assert.That(hit).IsEqualTo(XmlNameTableStatus.Ok);
        await Assert.That(table.Count).IsEqualTo(3);
    }

    [Test]
    public async Task NameTable_GetString_ForeignSymbol_ThrowsAndTryGetStringFails()
    {
        var table = new SpanInterningNameTable(10);
        var foreign = new XmlSymbol(5);

        await Assert.That(table.TryGetString(foreign, out var text)).IsFalse();
        await Assert.That(text).IsNull();
        await Assert.That(() => table.GetString(foreign)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task NameTable_Constructor_RejectsInvalidArguments()
    {
        await Assert.That(() => new SpanInterningNameTable(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new SpanInterningNameTable(10, 0)).Throws<ArgumentOutOfRangeException>();
    }
}
