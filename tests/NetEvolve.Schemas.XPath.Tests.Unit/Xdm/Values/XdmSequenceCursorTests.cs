namespace NetEvolve.Schemas.XPath.Tests.Unit.Xdm.Values;

using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class XdmSequenceCursorTests
{
    [Test]
    public async Task Open_OverAnEmptySequence_HasNoItem()
    {
        using var cursor = XdmSequence.Empty.Open();

        await Assert.That(cursor.MoveNext()).IsFalse();
    }

    [Test]
    public async Task Open_OverASingleItem_WalksTheItem()
    {
        using var cursor = XdmSequence.Create(XdmItem.FromInteger(7)).Open();

        await Assert.That(cursor.MoveNext()).IsTrue();
        await Assert.That(cursor.Current.TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(7L);
        await Assert.That(cursor.MoveNext()).IsFalse();
    }

    [Test]
    public async Task Open_OverAnArraySequence_WalksEveryItemAndIsNotLazy()
    {
        var sequence = XdmSequence.Create([XdmItem.FromInteger(1), XdmItem.FromInteger(2)]);
        var seen = new List<long>();

        using (var cursor = sequence.Open())
        {
            while (cursor.MoveNext())
            {
                _ = cursor.Current.TryGetInteger(out var value);
                seen.Add(value);
            }
        }

        await Assert.That(sequence.IsUnreadLazy).IsFalse();
        await Assert.That(seen).IsEquivalentTo([1L, 2L]);
    }
}
