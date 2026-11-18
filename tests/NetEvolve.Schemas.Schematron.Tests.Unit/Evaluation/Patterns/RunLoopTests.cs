namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns;

using System.Threading;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternModel;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Patterns.PatternTree;

/// <summary>Cancellation and the issue limit of the run loop (design #150 EP-16, #137 OQ-1).</summary>
public class RunLoopTests
{
    private static PatternElement Wide(int children)
    {
        var content = new object[children];
        for (var i = 0; i < children; i++)
        {
            content[i] = E("item");
        }

        return E("r", content);
    }

    [Test]
    public async Task Cancellation_AlreadyCancelled_ThrowsBeforeAnyEvent()
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        var schema = Schema(Pattern("p", Rule("r1", "a", Fails("x"))));
        using var h = new PatternHarness(schema, E("a"), cancellationToken: source.Token);

        await Assert.That(() => _ = h.Evaluate()).Throws<OperationCanceledException>();

        await Assert.That(h.Log.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Cancellation_BetweenPatterns_StopsBeforeTheNextPattern()
    {
        using var source = new CancellationTokenSource();
        var schema = Schema(Pattern("p1", Rule("r1", "a", Fails("x"))), Pattern("p2", Rule("r2", "a", Fails("y"))));
        using var h = new PatternHarness(schema, E("a"), cancellationToken: source.Token);
        h.Matcher.OnMatch = _ => source.Cancel();

        await Assert.That(() => _ = h.Evaluate()).Throws<OperationCanceledException>();

        await Assert.That(h.Log.Events).DoesNotContain("pattern:p2");
    }

    [Test]
    public async Task Cancellation_IsCheckedEvery256Nodes()
    {
        using var source = new CancellationTokenSource();
        var schema = Schema(Pattern("p", Rule("r1", "item", Holds())));
        using var h = new PatternHarness(schema, Wide(1_000), cancellationToken: source.Token);
        h.Matcher.OnMatch = calls =>
        {
            if (calls == 1)
            {
                source.Cancel();
            }
        };

        await Assert.That(() => _ = h.Evaluate()).Throws<OperationCanceledException>();

        // The token is looked at once per 256 visited nodes, so the run does not go on to the end of the 1,000 children.
        await Assert.That(h.Matcher.MatchCalls).IsLessThanOrEqualTo(256);
        await Assert.That(h.Matcher.MatchCalls).IsGreaterThan(0);
    }

    [Test]
    public async Task Run_Cancelled_DeliversNoFinish()
    {
        using var source = new CancellationTokenSource();
        var schema = Schema(Pattern("p", Rule("r1", "a", Fails("x"))));
        using var h = new PatternHarness(schema, E("a"), cancellationToken: source.Token);
        h.Matcher.OnMatch = _ => source.Cancel();

        try
        {
            _ = h.Evaluate();
        }
        catch (OperationCanceledException)
        {
            // Expected: the run ends without an end event.
        }

        await Assert.That(h.Log.Events).DoesNotContain("finish");
    }

    [Test]
    public async Task IssueCap_FirstDroppedError_StopsRun_AndReportsStopCode()
    {
        var schema = Schema(
            Pattern("p1", Rule("r1", "*", Fails("x"), Fails("y"))),
            Pattern("p2", Rule("r2", "*", Fails("z")))
        );
        using var h = new PatternHarness(schema, E("a", E("b")), maxIssues: 1);

        var summary = h.Evaluate();

        // x is kept, y is the first dropped error: the run ends at once, with the finding delivered and no further pattern.
        await Assert.That(summary.IsTruncated).IsTrue();
        await Assert.That(summary.StopCode).IsEqualTo(SchematronDiagnosticCodes.IssueLimitStop);
        await Assert.That(h.Log.Sequence).IsEqualTo("pattern:p1 fired:r1 failed:x failed:y");
        await Assert.That(h.Collector.Count).IsEqualTo(1);
    }

    [Test]
    public async Task IssueCap_StopCode_IsNes3502_AndNeverAnIssue()
    {
        var schema = Schema(Pattern("p", Rule("r1", "*", Fails("x"), Fails("y"))));
        using var h = new PatternHarness(schema, E("a"), maxIssues: 1);

        var summary = h.Evaluate();

        await Assert.That(summary.StopCode.ToString()).IsEqualTo("NES3502");
        await Assert.That(h.Collector.ToResult().Issues.Count).IsEqualTo(1);
        await Assert.That(h.Collector.ToResult().Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.AssertionFailed);
    }

    [Test]
    public async Task IssueCap_WarningOnlyOverflow_RunsToEnd()
    {
        var schema = Schema(Pattern("p", Rule("r1", "*", Reports("w"))));
        using var h = new PatternHarness(schema, E("a", E("b"), E("c")), maxIssues: 1);

        var summary = h.Evaluate();

        // OQ-1: only a dropped error stops the run; dropped warnings do not.
        await Assert.That(summary.IsTruncated).IsFalse();
        await Assert.That(h.Log.Fired).IsEqualTo("r1,r1,r1");
    }

    [Test]
    public async Task IssueCap_NothingDropped_IsNotTruncated()
    {
        var schema = Schema(Pattern("p", Rule("r1", "*", Fails("x"))));
        using var h = new PatternHarness(schema, E("a"), maxIssues: 1);

        var summary = h.Evaluate();

        await Assert.That(summary.IsTruncated).IsFalse();
        await Assert.That(summary.StopCode).IsEqualTo(default(DiagnosticCode));
    }

    [Test]
    public async Task IssueCap_DroppedContextError_StopsTheRun_LikeADroppedAssert()
    {
        var schema = Schema(Pattern("p", Rule("r1", "err:a", Fails("x"))));
        using var h = new PatternHarness(schema, E("r", E("a"), E("a")), maxIssues: 1);

        var summary = h.Evaluate();

        await Assert.That(summary.IsTruncated).IsTrue();
        await Assert.That(summary.StopCode).IsEqualTo(SchematronDiagnosticCodes.IssueLimitStop);
        await Assert.That(h.Log.Errors.Count).IsEqualTo(2);
    }
}
