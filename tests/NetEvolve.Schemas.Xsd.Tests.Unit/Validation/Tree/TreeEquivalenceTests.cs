namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Tree;

using System;
using System.Collections.Generic;
using System.Threading;

public class TreeEquivalenceTests
{
    public static IEnumerable<Func<string>> Documents()
    {
        foreach (var instance in TreeFixture.Instances)
        {
            var captured = instance;
            yield return () => captured;
        }
    }

    [Test]
    [MethodDataSource(nameof(Documents))]
    public async Task Document_GivesTheSameIssuesAsTheStreamingPath(
        string instance,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var streamed = TreeFixture.Streaming().Validate(instance.AsSpan(), TreeFixture.InstanceId, cancellationToken);

        var tree = TreeFixture.Validator().Validate(TreeFixture.Load(instance).Document, cancellationToken);

        await Assert.That(TreeFixture.Describe(tree.Result)).IsEqualTo(TreeFixture.Describe(streamed));
    }

    [Test]
    public async Task Document_InvalidInstance_ReportsIssuesWithTheirLocation(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tree = TreeFixture
            .Validator()
            .Validate(TreeFixture.Load(TreeFixture.Instances[1]).Document, cancellationToken);

        await Assert.That(tree.Result.IsValid).IsFalse();
        await Assert.That(tree.Result.Issues[0].Location.Source).IsEqualTo(TreeFixture.InstanceId);
        await Assert.That(tree.Result.Issues[0].Location.Line).IsEqualTo(2);
    }

    [Test]
    public async Task Document_ValidInstance_HasNoIssues(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tree = TreeFixture
            .Validator()
            .Validate(TreeFixture.Load(TreeFixture.Instances[0]).Document, cancellationToken);

        await Assert.That(tree.Result.IsValid).IsTrue();
        await Assert.That(tree.Result.Issues.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("<root><item>1</item><group ref=\"nope\"><inner>x</inner></group></root>", "group")]
    [Arguments("<root><item>1</item><group id=\"g\"><inner>1</inner></group></root>", "group")]
    [Arguments("<root><item>1</item><group><bad/></group></root>", "group")]
    [Arguments("<root xmlns:p=\"urn:p\"><item p:x=\"1\">z</item></root>", "item")]
    public async Task Subtree_GivesTheSameIssuesAsADocumentThatContainsOnlyTheSubtree(
        string instance,
        string local,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tree = TreeFixture.Load(instance);
        var element = TreeFixture.Element(tree, local);
        var validator = TreeFixture.Validator();

        var subtree = validator.Validate(element, cancellationToken);
        var own = validator.Validate(TreeFixture.OwnDocument(element).Document, cancellationToken);

        await Assert.That(TreeFixture.Describe(subtree.Result)).IsEqualTo(TreeFixture.Describe(own.Result));
    }

    [Test]
    public async Task Subtree_RunsTheListenersOfTheWholeRun_SoAnUnresolvedIdrefIsReported(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tree = TreeFixture.Load("<root><item>1</item><group ref=\"nope\"><inner>1</inner></group></root>");

        var result = TreeFixture.Validator().Validate(TreeFixture.Element(tree, "group"), cancellationToken);

        await Assert.That(result.Result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validate_CancelledToken_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var source = new CancellationTokenSource();
#if NET8_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif
        var document = TreeFixture.Load(TreeFixture.Instances[0]).Document;

        await Assert
            .That(() => TreeFixture.Validator().Validate(document, source.Token))
            .Throws<OperationCanceledException>();
    }
}
