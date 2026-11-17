namespace NetEvolve.Schemas.Tests.Unit.Validation;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;

public class TruncationContractTests
{
    private static ValidationIssue Issue(IssueSeverity severity, string message = "m") =>
        new(new DiagnosticCode(1), severity, message, default);

    private static ValidationIssueCollector Collector(int maxIssues) =>
        new(ValidationLimits.Default.WithMaxIssues(maxIssues));

    [Test]
    public async Task Constructor_NullLimits_ThrowsArgumentNullException() =>
        await Assert.That(() => new ValidationIssueCollector(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Add_NullIssue_ThrowsArgumentNullException() =>
        await Assert.That(() => Collector(2).Add(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Stop_NullIssue_ThrowsArgumentNullException() =>
        await Assert.That(() => Collector(2).Stop(null!)).Throws<ArgumentNullException>();

    [Test]
    [Arguments(IssueSeverity.Information)]
    [Arguments(IssueSeverity.Warning)]
    public async Task Stop_NonErrorSeverity_ThrowsArgumentException(IssueSeverity severity) =>
        await Assert.That(() => Collector(2).Stop(Issue(severity))).Throws<ArgumentException>();

    [Test]
    public async Task ToResult_BelowMaxIssues_IsNotTruncated()
    {
        var collector = Collector(3);
        collector.Add(Issue(IssueSeverity.Warning));
        collector.Add(Issue(IssueSeverity.Warning));

        var result = collector.ToResult();

        await Assert.That(result.IsTruncated).IsFalse();
        await Assert.That(result.Issues.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ToResult_ExactlyMaxIssues_IsNotTruncated()
    {
        var collector = Collector(2);
        collector.Add(Issue(IssueSeverity.Warning));
        collector.Add(Issue(IssueSeverity.Error));

        var result = collector.ToResult();

        await Assert.That(result.IsTruncated).IsFalse();
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task ToResult_WarningOnlyOverflow_IsTruncatedAndStaysValid()
    {
        var collector = Collector(2);
        collector.Add(Issue(IssueSeverity.Warning));
        collector.Add(Issue(IssueSeverity.Information));
        collector.Add(Issue(IssueSeverity.Warning));

        var result = collector.ToResult();

        await Assert.That(result.IsTruncated).IsTrue();
        await Assert.That(result.HasDroppedErrors).IsFalse();
        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Issues.Count).IsEqualTo(2);
        await Assert.That(collector.Count).IsEqualTo(2);
    }

    [Test]
    public async Task ToResult_DroppedError_KeepsFirstIssuesAndIsInvalid()
    {
        var collector = Collector(2);
        var first = Issue(IssueSeverity.Warning, "first");
        var second = Issue(IssueSeverity.Warning, "second");
        collector.Add(first);
        collector.Add(second);
        collector.Add(Issue(IssueSeverity.Error, "dropped"));

        var result = collector.ToResult();

        await Assert.That(result.Issues.Count).IsEqualTo(2);
        await Assert.That(result.Issues[0]).IsSameReferenceAs(first);
        await Assert.That(result.Issues[1]).IsSameReferenceAs(second);
        await Assert.That(result.IsTruncated).IsTrue();
        await Assert.That(result.HasDroppedErrors).IsTrue();
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Stop_BeyondCap_AppendsStopIssueAndSetsTruncated()
    {
        var collector = Collector(2);
        collector.Add(Issue(IssueSeverity.Warning));
        collector.Add(Issue(IssueSeverity.Warning));
        var stop = Issue(IssueSeverity.Error, "stop");

        collector.Stop(stop);

        var result = collector.ToResult();
        await Assert.That(collector.IsStopped).IsTrue();
        await Assert.That(collector.Count).IsEqualTo(3);
        await Assert.That(result.Issues.Count).IsEqualTo(3);
        await Assert.That(result.Issues[2]).IsSameReferenceAs(stop);
        await Assert.That(result.IsTruncated).IsTrue();
        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Add_AfterStop_HasNoEffect()
    {
        var collector = Collector(5);
        collector.Stop(Issue(IssueSeverity.Error));

        collector.Add(Issue(IssueSeverity.Error));

        await Assert.That(collector.Count).IsEqualTo(1);
        await Assert.That(collector.ToResult().HasDroppedErrors).IsFalse();
    }

    [Test]
    public async Task Stop_Twice_KeepsOnlyFirstStopIssue()
    {
        var collector = Collector(5);
        collector.Stop(Issue(IssueSeverity.Error));

        collector.Stop(Issue(IssueSeverity.Error));

        await Assert.That(collector.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ToResult_CalledTwice_GivesEqualSnapshots()
    {
        var collector = Collector(2);
        collector.Add(Issue(IssueSeverity.Warning));

        var first = collector.ToResult();
        collector.Add(Issue(IssueSeverity.Error));
        var second = collector.ToResult();

        await Assert.That(first.Issues.Count).IsEqualTo(1);
        await Assert.That(second.Issues.Count).IsEqualTo(2);
        await Assert.That(collector.ToResult().Issues.Count).IsEqualTo(second.Issues.Count);
        await Assert.That(collector.ToResult().IsValid).IsEqualTo(second.IsValid);
    }

    [Test]
    public async Task ToResult_NoIssues_IsValid()
    {
        var result = Collector(1).ToResult();

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Issues.Count).IsEqualTo(0);
    }
}
