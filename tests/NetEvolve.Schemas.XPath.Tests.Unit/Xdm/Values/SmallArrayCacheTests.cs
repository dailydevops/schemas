namespace NetEvolve.Schemas.XPath.Tests.Unit.Xdm.Values;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The per-thread stack of small arrays that path evaluation rents from instead of the shared pool.</summary>
public class SmallArrayCacheTests
{
    [Test]
    public async Task Rent_AfterReturn_GivesTheSameClearedArray()
    {
        var first = SmallArrayCache<object>.Rent(3);
        first[0] = new object();
        first[2] = new object();

        SmallArrayCache<object>.Return(first, 3);
        var second = SmallArrayCache<object>.Rent(4);

        await Assert.That(second).IsSameReferenceAs(first);
        await Assert.That(second[0]).IsNull();
        await Assert.That(second[2]).IsNull();
        SmallArrayCache<object>.Return(second, 0);
    }

    [Test]
    public async Task Rent_MoreSmallArraysThanTheStackHolds_AllocatesTheRest()
    {
        var rented = new int[12][];
        for (var i = 0; i < rented.Length; i++)
        {
            rented[i] = SmallArrayCache<int>.Rent(1);
        }

        for (var i = 0; i < rented.Length; i++)
        {
            SmallArrayCache<int>.Return(rented[i], 1);
        }

        await Assert.That(rented[0]).IsNotSameReferenceAs(rented[1]);
        await Assert.That(rented[11].Length).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Rent_LargerThanASmallArray_ComesFromTheSharedPool()
    {
        var large = SmallArrayCache<string>.Rent(100);
        large[99] = "x";

        SmallArrayCache<string>.Return(large, 100);
        var again = SmallArrayCache<string>.Rent(100);

        await Assert.That(large.Length).IsGreaterThanOrEqualTo(100);
        await Assert.That(again[99]).IsNull();
        SmallArrayCache<string>.Return(again, 0);
    }
}
