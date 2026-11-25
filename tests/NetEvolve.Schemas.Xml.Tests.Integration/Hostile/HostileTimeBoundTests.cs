namespace NetEvolve.Schemas.Xml.Tests.Integration.Hostile;

/// <summary>
/// The time bound of the hostile corpus counts the processor time a load uses, not the time it waits for a processor:
/// a loaded machine slows a load down without making the library slow.
/// </summary>
public class HostileTimeBoundTests
{
    [Test]
    [NotInParallel]
    public async Task RunBounded_WithWorkThatWaitsLongerThanTheBoundWithoutUsingTheProcessor_DoesNotTimeOut(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var timedOut = await HostileInputs.RunBoundedAsync(
            () => Task.Delay(TimeSpan.FromMilliseconds(2500), cancellationToken),
            TimeSpan.FromMilliseconds(2000),
            cancellationToken
        );

        await Assert.That(timedOut).IsFalse();
    }

    [Test]
    [NotInParallel]
    public async Task RunBounded_WithWorkThatUsesTheProcessorLongerThanTheBound_TimesOut(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stop = new CancellationTokenSource();
        var timedOut = await HostileInputs.RunBoundedAsync(
            () =>
            {
                while (!stop.IsCancellationRequested)
                {
                    Thread.SpinWait(1000);
                }

                return Task.CompletedTask;
            },
            TimeSpan.FromMilliseconds(300),
            cancellationToken
        );
#if NET8_0_OR_GREATER
        await stop.CancelAsync();
#else
        stop.Cancel();
#endif

        await Assert.That(timedOut).IsTrue();
    }

    [Test]
    [NotInParallel]
    public async Task RunBounded_WithWorkThatNeverEndsWithoutUsingTheProcessor_TimesOutOnTheWallClock(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var stop = new CancellationTokenSource();
        var timedOut = await HostileInputs.RunBoundedAsync(
            () => Task.Delay(Timeout.InfiniteTimeSpan, stop.Token),
            TimeSpan.FromMilliseconds(30),
            cancellationToken
        );
#if NET8_0_OR_GREATER
        await stop.CancelAsync();
#else
        stop.Cancel();
#endif

        await Assert.That(timedOut).IsTrue();
    }
}
