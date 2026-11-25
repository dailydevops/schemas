namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Paths;

using System;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static PathFixture;

/// <summary>The errors of path expressions: XPDY0002, XPTY0018, XPTY0019, XPTY0020, XPDY0050, and the static errors of steps.</summary>
public class PathErrorTests
{
    private static async Task AssertRaised(XPathException? error, string code)
    {
        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Diagnostic.QName).IsEqualTo(new XPathErrorCode(code));
    }

    [Test]
    [Arguments("child::b")]
    [Arguments("/")]
    [Arguments("/a")]
    [Arguments("b/c")]
    [Arguments("..")]
    [Arguments("@id")]
    [Arguments(".//i")]
    [Arguments("//b")]
    [Arguments("self::node()")]
    public async Task Path_WithoutAContextItem_RaisesXpdy0002(string expression) =>
        await AssertRaised(Raised(expression), "XPDY0002");

    [Test]
    [Arguments("child::b")]
    [Arguments("/")]
    [Arguments("@id")]
    [Arguments("..")]
    [Arguments("//b")]
    [Arguments("descendant::node()")]
    [Arguments("b/c")]
    public async Task Path_WhoseContextItemIsAnAtomicValue_RaisesXpty0020(string expression) =>
        await AssertRaised(ArithmeticFixture.Raised(() => EvaluateWithAtomicContext(expression)), "XPTY0020");

    [Test]
    [Arguments("$atoms/b")]
    [Arguments("/a/(1)/b")]
    [Arguments("$mixed/self::node()")]
    [Arguments("(1, 2)/b")]
    [Arguments("/a/b/1/..")]
    public async Task NonFinalStep_ReturningAnAtomicValue_RaisesXpty0019(string expression) =>
        await AssertRaised(Raised(expression, "a"), "XPTY0019");

    [Test]
    [Arguments("/a/(b, 1)")]
    [Arguments("/a/b/(., 1)")]
    [Arguments("/a/(1, b)")]
    public async Task LastStep_ReturningNodesAndAtomicValues_RaisesXpty0018(string expression) =>
        await AssertRaised(Raised(expression, "a"), "XPTY0018");

    // The root of a tree that has no document node: / is fn:root(.) treat as document-node().
    [Test]
    public async Task LeadingSlash_WhenTheRootIsNotADocumentNode_RaisesXpdy0050()
    {
        var tree = new FragmentTree();
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(tree.At(2)));

        var error = ArithmeticFixture.Raised(() => CompileOrThrow("/").Evaluate(context));

        await AssertRaised(error, "XPDY0050");
    }

    [Test]
    [Arguments("namespace::b", "XPST0010", 0)]
    [Arguments("a/namespace::b", "XPST0010", 2)]
    [Arguments("child::schema-element(x)", "XPST0008", 7)]
    [Arguments("attribute::schema-attribute(x)", "XPST0008", 11)]
    [Arguments("child::schema-element(q:x)", "XPST0081", 22)]
    [Arguments("attribute::schema-attribute(q:x)", "XPST0081", 28)]
    [Arguments("child::document-node(schema-element(q:x))", "XPST0081", 36)]
    [Arguments("child::element(b, xs:bogus)", "XPST0051", 18)]
    [Arguments("q:b", "XPST0081", 0)]
    [Arguments("a/q:*", "XPST0081", 2)]
    public async Task StepThatCannotBeCompiled_RaisesTheStaticErrorWithItsLocation(
        string expression,
        string code,
        int offset
    )
    {
        var compiled = Compile(expression, out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(new XPathErrorCode(code));
        await Assert.That(diagnostic.Offset).IsEqualTo(offset);
    }

    // A tree whose element at index 1 is the root: every other node is below it.
    private sealed class FragmentTree : CountingTree
    {
        public FragmentTree()
            : base(4) { }

        protected internal override int GetParent(int index) => index == 1 ? NoNode : 1;
    }
}
