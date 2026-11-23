namespace NetEvolve.Schemas.Tests.Architecture.BannedSymbols;

public class ArgumentGuardBanTests
{
    [Test]
    [Arguments("ArgumentNullException")]
    [Arguments("ArgumentOutOfRangeException")]
    [Arguments("ArgumentException")]
    public async Task Build_HandWrittenArgumentGuardInSrc_ReportsRS0030Error(
        string exceptionType,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var source =
            $"internal static class P {{ internal static void M(int value) => throw new System.{exceptionType}(nameof(value)); }}";

        var (exitCode, output) = await ProbeBuild.BuildAsync("P.cs", source, cancellationToken).ConfigureAwait(false);

        await Assert.That(exitCode).IsNotEqualTo(0).Because(output);
        await Assert.That(output).Contains("error RS0030");
    }
}
