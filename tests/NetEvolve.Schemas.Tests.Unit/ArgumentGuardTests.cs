namespace NetEvolve.Schemas.Tests.Unit;

using NetEvolve.Arguments;

public class ArgumentGuardTests
{
    [Test]
    public async Task ThrowIfLessThan_ValueBelowRange_ThrowsArgumentOutOfRangeException()
    {
        var offset = -2;

        await Assert.That(() => Argument.ThrowIfLessThan(offset, -1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ThrowIfLessThan_ValueInRange_ReturnsWithoutThrowing()
    {
        var offset = -1;

        await Assert.That(() => Argument.ThrowIfLessThan(offset, -1)).ThrowsNothing();
    }

    // Regression for #464: the guard is executed from the netstandard build of src on the net8.0 and net9.0 legs.
    [Test]
    public async Task PolyfillProbe_RequirePositive_RunsGuardFromLibraryBuild()
    {
        await Assert.That(PolyfillProbe.RequirePositive(1)).IsEqualTo(1);
        await Assert.That(() => PolyfillProbe.RequirePositive(0)).Throws<ArgumentOutOfRangeException>();
    }
}
