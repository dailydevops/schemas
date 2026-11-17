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

    [Test]
    [Arguments("ArgumentNullException.ThrowIfNull(value)")]
    [Arguments("ArgumentException.ThrowIfNullOrEmpty(value)")]
    [Arguments("ArgumentException.ThrowIfNullOrWhiteSpace(value)")]
    [Arguments("ArgumentOutOfRangeException.ThrowIfNegative(count)")]
    [Arguments("ArgumentOutOfRangeException.ThrowIfZero(count)")]
    [Arguments("ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count)")]
    [Arguments("ArgumentOutOfRangeException.ThrowIfEqual(count, 1)")]
    [Arguments("ArgumentOutOfRangeException.ThrowIfNotEqual(count, 1)")]
    [Arguments("ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 1)")]
    [Arguments("ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(count, 1)")]
    [Arguments("ArgumentOutOfRangeException.ThrowIfLessThan(count, 1)")]
    [Arguments("ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(count, 1)")]
    public async Task Build_BclStyleThrowIfInSrc_ReportsRS0030Error(string call, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var source =
            $"internal static class P {{ internal static void M(string value, int count) {{ _ = value; _ = count; System.{call}; }} }}";

        var (exitCode, output) = await ProbeBuild.BuildAsync("P.cs", source, cancellationToken).ConfigureAwait(false);

        await Assert.That(exitCode).IsNotEqualTo(0).Because(output);
        await Assert.That(output).Contains("error RS0030");
    }
}
