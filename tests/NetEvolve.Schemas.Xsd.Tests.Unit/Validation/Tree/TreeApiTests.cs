namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Tree;

using System;
using System.Threading;

public class TreeApiTests
{
    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(6)]
    public async Task ValidateTree_Document_GivesTheIssuesOfTheStreamingPath(
        int index,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = TreeFixture.Streaming();
        var instance = TreeFixture.Instances[index];

        var streamed = validator.Validate(instance.AsSpan(), TreeFixture.InstanceId, cancellationToken);
        var tree = validator.ValidateTree(TreeFixture.Load(instance), cancellationToken);

        await Assert.That(TreeFixture.Describe(tree.Result)).IsEqualTo(TreeFixture.Describe(streamed));
    }

    [Test]
    public async Task ValidateTree_Element_GivesTheIssuesOfADocumentThatContainsOnlyTheElement(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = TreeFixture.Streaming();
        var input = TreeFixture.Load("<root><item>1</item><group><bad/></group></root>");
        var element = TreeFixture.Element(input, "group");

        var subtree = validator.ValidateTree(input, element, cancellationToken);
        var own = validator.ValidateTree(TreeFixture.OwnDocument(element), cancellationToken);

        await Assert.That(subtree.Result.IsValid).IsFalse();
        await Assert.That(TreeFixture.Describe(subtree.Result)).IsEqualTo(TreeFixture.Describe(own.Result));
    }

    [Test]
    public async Task ValidateTree_CancelledToken_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var source = new CancellationTokenSource();
#if NET10_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif
        var input = TreeFixture.Load("<root/>");

        await Assert
            .That(() => TreeFixture.Streaming().ValidateTree(input, source.Token))
            .Throws<OperationCanceledException>();
    }
}
