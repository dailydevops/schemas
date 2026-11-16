namespace NetEvolve.Schemas.Tests.Architecture.BannedSymbols;

public class BannedSymbolsProbeTests
{
    private const string LinqSource =
        "internal static class P { internal static int[] M(int[] values) => System.Linq.Enumerable.ToArray(values); }";

    [Test]
    [Arguments("P.cs")]
    [Arguments("Interop/P.cs")]
    public async Task Build_LinqInSrc_ReportsRS0030Error(string file, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (exitCode, output) = await ProbeBuild.BuildAsync(file, LinqSource, cancellationToken).ConfigureAwait(false);

        await Assert.That(exitCode).IsNotEqualTo(0).Because(output);
        await Assert.That(output).Contains("error RS0030");
    }
}
