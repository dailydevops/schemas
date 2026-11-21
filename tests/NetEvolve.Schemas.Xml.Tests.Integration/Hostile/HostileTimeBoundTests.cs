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
            () => Task.Delay(TimeSpan.FromMilliseconds(900), cancellationToken),
            TimeSpan.FromMilliseconds(300),
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
        await stop.CancelAsync();

        await Assert.That(timedOut).IsTrue();
    }
}
