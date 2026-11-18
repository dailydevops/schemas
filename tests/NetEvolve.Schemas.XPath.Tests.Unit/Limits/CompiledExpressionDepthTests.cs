namespace NetEvolve.Schemas.XPath.Tests.Unit.Limits;

using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using static LimitsFixture;

/// <summary>The depth a compiled expression records while it is lowered, and the location reported when an evaluation limit is lower.</summary>
public class CompiledExpressionDepthTests
{
    [Test]
    public async Task TheDepth_IsTheDepthOfTheSyntaxTree()
    {
        await Assert.That(CompileOrThrow("1").Depth).IsEqualTo(1);
        await Assert.That(CompileOrThrow("1 + 2").Depth).IsEqualTo(2);
        await Assert.That(CompileOrThrow(Chain(40)).Depth).IsEqualTo(40);
        await Assert.That(CompileOrThrow("(1 + 2) + 3").Depth).IsEqualTo(4);
    }

    [Test]
    public async Task Evaluate_BeyondTheDepthLimitOfTheContext_LocatesTheWholeExpression()
    {
        var compiled = CompileOrThrow("1 + 2 + 3");
        using var context = NewContext(NetEvolve.Schemas.XPath.Limits.XPathLimits.Default.WithMaxRecursionDepth(2));

        var raised = ArithmeticFixture.Raised(() => compiled.Evaluate(context));

        await Assert.That(raised).IsNotNull();
        await Assert.That(raised!.Diagnostic.Offset).IsEqualTo(0);
        await Assert.That(raised.Diagnostic.Length).IsEqualTo(9);
    }
}
