namespace NetEvolve.Schemas.Tests.Unit;

public class ArgumentGuardTests
{
    [Test]
    public async Task ThrowIfOutOfRange_ValueBelowRange_ThrowsArgumentOutOfRangeException()
    {
        var offset = -2;

        await Assert
            .That(() => ArgumentOutOfRangeException.ThrowIfOutOfRange(offset, -1, int.MaxValue))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ThrowIfOutOfRange_ValueInRange_ReturnsWithoutThrowing()
    {
        var offset = -1;

        await Assert
            .That(() => ArgumentOutOfRangeException.ThrowIfOutOfRange(offset, -1, int.MaxValue))
            .ThrowsNothing();
    }
}
