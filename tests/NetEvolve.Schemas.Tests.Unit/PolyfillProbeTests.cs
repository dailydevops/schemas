namespace NetEvolve.Schemas.Tests.Unit;

public class PolyfillProbeTests
{
    [Test]
    public async Task Sum_Span_ReturnsTotal() => await Assert.That(PolyfillProbe.Sum([1, 2, 3])).IsEqualTo(6);

    [Test]
    public async Task SumAsync_ValueTask_ReturnsTotal()
    {
        var total = await PolyfillProbe.SumAsync(2, 3).ConfigureAwait(false);

        await Assert.That(total).IsEqualTo(5);
    }

    [Test]
    public async Task RangeAsync_AsyncEnumerable_YieldsAll()
    {
        var count = 0;
        await foreach (var _ in PolyfillProbe.RangeAsync(4).ConfigureAwait(false))
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(4);
    }
}
