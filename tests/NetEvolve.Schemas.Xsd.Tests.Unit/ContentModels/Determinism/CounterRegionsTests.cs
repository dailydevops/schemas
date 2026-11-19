namespace NetEvolve.Schemas.Xsd.Tests.Unit.ContentModels.Determinism;

using System;
using System.Globalization;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.ContentModels.Determinism;

public class CounterRegionsTests
{
    private const long Unbounded = AutomatonParticle.Unbounded;

    [Test]
    [Arguments(2L, 3L, 3L, "1;2;3")]
    [Arguments(3L, 5L, 5L, "1;2;3;4;5")]
    [Arguments(2L, 2L, 2L, "1;2")]
    [Arguments(1L, 2L, 2L, "1;2")]
    [Arguments(1L, 5L, 5L, "1,3;4;5")]
    [Arguments(1000L, 1_000_000_000L, 1_000_000_000L, "1,998;999;1000,999999998;999999999;1000000000")]
    [Arguments(3L, Unbounded, 3L, "1;2;3")]
    [Arguments(1000L, Unbounded, 1000L, "1,998;999;1000")]
    [Arguments(1L, Unbounded, 4L, "1,4")]
    public async Task Classes_SmallBoundsCollapse_AndHugeBoundsKeepAtMostFive(
        long min,
        long limit,
        long cap,
        string expected
    )
    {
        var regions = new CounterRegions(min, limit, cap);
        var text = new string[regions.Count];
        for (var i = 0; i < text.Length; i++)
        {
            text[i] =
                regions.Low(i) == regions.High(i)
                    ? regions.Low(i).ToString(CultureInfo.InvariantCulture)
                    : string.Create(CultureInfo.InvariantCulture, $"{regions.Low(i)},{regions.High(i)}");
        }

        await Assert.That(string.Join(";", text)).IsEqualTo(expected);
        await Assert.That(regions.Count).IsLessThanOrEqualTo(CounterRegions.MaxCount);
    }

    [Test]
    public async Task Successors_ReachExactlyTheClassesOfTheNextValue()
    {
        // min 1000, max one billion: [1,998] [999] [1000,999999998] [999999999] [1000000000].
        var regions = new CounterRegions(1000, 1_000_000_000, 1_000_000_000);

        regions.Successors(0, out var lowFirst, out var lowLast);
        regions.Successors(1, out var belowMinFirst, out var belowMinLast);
        regions.Successors(2, out var midFirst, out var midLast);
        regions.Successors(3, out var belowMaxFirst, out var belowMaxLast);
        regions.Successors(4, out var maxFirst, out var maxLast);

        await Assert.That((lowFirst, lowLast)).IsEqualTo((0, 1));
        await Assert.That((belowMinFirst, belowMinLast)).IsEqualTo((2, 2));
        await Assert.That((midFirst, midLast)).IsEqualTo((2, 3));
        await Assert.That((belowMaxFirst, belowMaxLast)).IsEqualTo((4, 4));
        await Assert.That((maxFirst, maxLast)).IsEqualTo((4, 4));
    }

    [Test]
    public async Task IndexOf_FindsTheClassOfAValue_AndSaturatesAboveTheCap()
    {
        var regions = new CounterRegions(2, 3, 3);

        await Assert.That(regions.IndexOf(1)).IsEqualTo(0);
        await Assert.That(regions.IndexOf(2)).IsEqualTo(1);
        await Assert.That(regions.IndexOf(3)).IsEqualTo(2);
        await Assert.That(regions.IndexOf(4)).IsEqualTo(2);
    }

    [Test]
    public async Task Constructor_RejectsACapBelowOne() =>
        await Assert.That(() => new CounterRegions(1, 2, 0)).Throws<ArgumentOutOfRangeException>();
}
