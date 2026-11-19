namespace NetEvolve.Schemas.Tests.Unit.TargetFrameworks;

using System.Reflection;
using System.Runtime.Versioning;

public class LibraryTargetFrameworkTests
{
    // Each test leg loads the library build of its own target framework (#697: src targets net8.0 and net9.0 too).
#if NET11_0
    private const string Expected = ".NETCoreApp,Version=v11.0";
#elif NET10_0
    private const string Expected = ".NETCoreApp,Version=v10.0";
#elif NET9_0
    private const string Expected = ".NETCoreApp,Version=v9.0";
#elif NET8_0
    private const string Expected = ".NETCoreApp,Version=v8.0";
#endif

    [Test]
    public async Task LoadedLibrary_TargetFrameworkAttribute_MatchesLeg()
    {
        var attribute = typeof(PolyfillProbe).Assembly.GetCustomAttribute<TargetFrameworkAttribute>();

        await Assert.That(attribute?.FrameworkName).IsEqualTo(Expected);
    }
}
