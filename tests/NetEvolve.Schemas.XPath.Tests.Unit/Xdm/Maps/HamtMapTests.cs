namespace NetEvolve.Schemas.XPath.Tests.Unit.Xdm.Maps;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Maps;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The persistent hash trie behind the map item: lookups, updates that leave the source unchanged, and keys with one hash.</summary>
public class HamtMapTests
{
    private static XdmItem Key(int number) => XdmItem.FromInteger(number);

    private static XdmSequence Value(int number) => XdmSequence.Create(XdmItem.FromInteger(number));

    // Doubles that differ by less than the precision of xs:float have the same hash and are different keys.
    private static XdmItem Colliding(int number) => XdmItem.FromDouble(1d + (number * 1e-12));

    private static HamtMap Build(int count, System.Func<int, XdmItem> key)
    {
        var map = HamtMap.Empty;
        for (var i = 0; i < count; i++)
        {
            map = map.Put(key(i), Value(i));
        }

        return map;
    }

    [Test]
    public async Task TryGet_AbsentKeyInOccupiedSlot_ReturnsNotFoundAndEmptyValue(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var map = Build(3000, Key);

        for (var i = 3000; i < 6000; i++)
        {
            var found = map.TryGet(Key(i), out var value);

            await Assert.That(found).IsFalse();
            await Assert.That(value.Length).IsEqualTo(0);
        }
    }

    [Test]
    public async Task Empty_HasNoEntries(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(HamtMap.Empty.Count).IsEqualTo(0);
        await Assert.That(HamtMap.Empty.TryGet(Key(1), out _)).IsFalse();
        await Assert.That(HamtMap.Empty.Remove(Key(1))).IsSameReferenceAs(HamtMap.Empty);
        await Assert.That(HamtMap.Empty.Entries().Length).IsEqualTo(0);
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(33)]
    [Arguments(1000)]
    public async Task Put_ThenGet_FindsEveryEntry(int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var map = Build(count, Key);

        await Assert.That(map.Count).IsEqualTo(count);
        for (var i = 0; i < count; i++)
        {
            await Assert
                .That(map.TryGet(Key(i), out var value) && value[0].TryGetInteger(out var n) && n == i)
                .IsTrue();
        }

        await Assert.That(map.TryGet(Key(count), out _)).IsFalse();
        await Assert.That(map.Entries().Length).IsEqualTo(count);
        await Assert.That(map.Entries().Select(static e => e.Key).Distinct().Count()).IsEqualTo(count);
    }

    [Test]
    public async Task Put_OnAnExistingKey_ReplacesTheEntryAndLeavesTheSourceUnchanged(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var source = Build(100, Key);
        var changed = source.Put(Key(7), Value(700));

        await Assert.That(changed.Count).IsEqualTo(100);
        await Assert
            .That(changed.TryGet(Key(7), out var after) && after[0].TryGetInteger(out var a) && a == 700)
            .IsTrue();
        await Assert
            .That(source.TryGet(Key(7), out var before) && before[0].TryGetInteger(out var b) && b == 7)
            .IsTrue();
    }

    [Test]
    public async Task Put_OnANewKey_LeavesTheSourceUnchanged(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var source = Build(100, Key);
        var grown = source.Put(Key(1000), Value(1));

        await Assert.That(source.Count).IsEqualTo(100);
        await Assert.That(source.TryGet(Key(1000), out _)).IsFalse();
        await Assert.That(grown.Count).IsEqualTo(101);
        await Assert.That(grown.TryGet(Key(1000), out _)).IsTrue();
    }

    [Test]
    public async Task Remove_DropsTheEntryAndLeavesTheSourceUnchanged(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var source = Build(100, Key);
        var reduced = source.Remove(Key(7));

        await Assert.That(reduced.Count).IsEqualTo(99);
        await Assert.That(reduced.TryGet(Key(7), out _)).IsFalse();
        await Assert.That(reduced.TryGet(Key(8), out _)).IsTrue();
        await Assert.That(source.Count).IsEqualTo(100);
        await Assert.That(source.TryGet(Key(7), out _)).IsTrue();
        await Assert.That(source.Remove(Key(1000))).IsSameReferenceAs(source);
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(50)]
    [Arguments(1000)]
    public async Task Remove_EveryEntry_LeavesTheEmptyMap(int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var map = Build(count, Key);
        for (var i = 0; i < count; i++)
        {
            map = map.Remove(Key(i));

            await Assert.That(map.Count).IsEqualTo(count - i - 1);
            await Assert.That(map.TryGet(Key(i), out _)).IsFalse();
        }

        await Assert.That(map).IsSameReferenceAs(HamtMap.Empty);
    }

    [Test]
    public async Task KeysWithTheSameHash_AreKeptApart(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(MapKeys.Hash(Colliding(1))).IsEqualTo(MapKeys.Hash(Colliding(2)));
        await Assert.That(MapKeys.SameKey(Colliding(1), Colliding(2))).IsFalse();

        var map = Build(5, Colliding);

        await Assert.That(map.Count).IsEqualTo(5);
        for (var i = 0; i < 5; i++)
        {
            await Assert
                .That(map.TryGet(Colliding(i), out var value) && value[0].TryGetInteger(out var n) && n == i)
                .IsTrue();
        }

        await Assert.That(map.TryGet(Colliding(5), out _)).IsFalse();
        await Assert.That(map.Entries().Length).IsEqualTo(5);
    }

    [Test]
    public async Task KeysWithTheSameHash_CanBeReplacedAndRemoved(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var map = Build(5, Colliding).Put(Colliding(3), Value(30));

        await Assert.That(map.Count).IsEqualTo(5);
        await Assert
            .That(map.TryGet(Colliding(3), out var value) && value[0].TryGetInteger(out var n) && n == 30)
            .IsTrue();

        var reduced = map.Remove(Colliding(0));

        await Assert.That(reduced.Count).IsEqualTo(4);
        await Assert.That(reduced.TryGet(Colliding(0), out _)).IsFalse();
        await Assert.That(reduced.TryGet(Colliding(1), out _)).IsTrue();
        await Assert.That(map.TryGet(Colliding(0), out _)).IsTrue();
        await Assert.That(reduced.Remove(Colliding(9))).IsSameReferenceAs(reduced);

        for (var i = 1; i < 5; i++)
        {
            reduced = reduced.Remove(Colliding(i));
        }

        await Assert.That(reduced).IsSameReferenceAs(HamtMap.Empty);
    }

    [Test]
    public async Task KeysWithTheSameHash_NextToOtherKeys_AreKeptApart(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var map = Build(300, Key).Put(Colliding(1), Value(1)).Put(Colliding(2), Value(2));
        map = map.Remove(Key(5)).Remove(Colliding(1));

        await Assert.That(map.Count).IsEqualTo(300);
        await Assert.That(map.TryGet(Colliding(2), out _)).IsTrue();
        await Assert.That(map.TryGet(Colliding(1), out _)).IsFalse();
        await Assert.That(map.TryGet(Key(6), out _)).IsTrue();
    }
}
