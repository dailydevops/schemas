namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.SchemaLocation;

using System.Threading;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Tree;

public class TreeHintTests
{
    private const string Hint = "xsi:schemaLocation=\"urn:h h.xsd\"";

    [Test]
    [Arguments("<h:root><h:a>1</h:a></h:root>")]
    [Arguments("<h:root><h:a>x</h:a></h:root>")]
    [Arguments("<h:root><h:b/></h:root>")]
    public async Task ValidateTree_Hint_GivesTheResultOfTheStreamingPath(string content, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var validator = HintFixture.Build(HintFixture.Resolver());
        var instance = HintFixture.BaseRoot(Hint, content);

        var streamed = HintFixture.Run(validator, instance);
        var tree = validator.ValidateTree(TreeFixture.Load(instance), token);

        await Assert.That(TreeFixture.Describe(tree.Result)).IsEqualTo(TreeFixture.Describe(streamed));
        await Assert.That(tree.Result.IsValid).IsEqualTo(content == "<h:root><h:a>1</h:a></h:root>");
    }

    [Test]
    public async Task ValidateTree_HintsOff_IgnoresTheHintAndAccessesNoLocation(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var resolver = HintFixture.Resolver();
        var validator = HintFixture.Build(resolver, hints: false);

        var tree = validator.ValidateTree(
            TreeFixture.Load(HintFixture.BaseRoot(Hint, "<h:root><h:b/></h:root>")),
            token
        );

        await Assert.That(tree.Result.IsValid).IsTrue();
        await Assert.That(resolver.Requests).IsEmpty();
    }
}
