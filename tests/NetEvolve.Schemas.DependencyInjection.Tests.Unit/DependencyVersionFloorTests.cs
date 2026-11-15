using Microsoft.Extensions.DependencyInjection;

namespace NetEvolve.Schemas.DependencyInjection.Tests.Unit;

public class DependencyVersionFloorTests
{
    // Version floor per target framework (issue #418): net8.0 to net10.0 use the 10.x line, net11.0 uses 11.x.
    [Test]
    public async Task Abstractions_Major_Matches_TargetFramework()
    {
#if NET11_0_OR_GREATER
        const int expected = 11;
#else
        const int expected = 10;
#endif

        var actual = typeof(IServiceCollection).Assembly.GetName().Version!.Major;

        await Assert.That(actual).IsEqualTo(expected);
    }
}
