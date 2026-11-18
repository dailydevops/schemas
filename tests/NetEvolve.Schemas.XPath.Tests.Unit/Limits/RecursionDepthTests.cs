namespace NetEvolve.Schemas.XPath.Tests.Unit.Limits;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using static LimitsFixture;

/// <summary>
/// The recursion depth budget. The executable tree is as deep as the syntax tree it is lowered from, and the evaluator recurses once
/// per level, so the depth is checked while lowering (the compiler recurses too) and again when a compiled expression is evaluated
/// under other limits. The depth of <c>1+1+...+1</c> equals its number of terms.
/// </summary>
public class RecursionDepthTests
{
    [Test]
    public async Task Compile_ExactlyAtTheLimit_Succeeds()
    {
        var limits = XPathLimits.Default.WithMaxRecursionDepth(50);

        var compiled = Compile(Chain(50), limits, out var diagnostic);

        await Assert.That(compiled).IsNotNull();
        await Assert.That(diagnostic.Code).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Compile_OneLevelBeyondTheLimit_EndsInTheCodedDiagnostic()
    {
        var limits = XPathLimits.Default.WithMaxRecursionDepth(50);

        var compiled = Compile(Chain(51), limits, out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathLimitErrors.RecursionDepthExceeded);
        await Assert.That(diagnostic.Kind).IsEqualTo(XPathErrorKind.Engine);
    }

    [Test]
    public async Task Compile_BeyondTheLimit_NamesTheLimitAndTheValue_AndLocatesTheNode()
    {
        var limits = XPathLimits.Default.WithMaxRecursionDepth(3);

        // The innermost literal is the fourth level, at offset 0.
        _ = Compile("1+1+1+1", limits, out var diagnostic);

        await Assert.That(diagnostic.Message).Contains("MaxRecursionDepth");
        await Assert.That(diagnostic.Message).Contains("3");
        await Assert.That(diagnostic.Offset).IsEqualTo(0);
        await Assert.That(diagnostic.Length).IsEqualTo(1);
    }

    [Test]
    public async Task Compile_ADeepChain_EndsInTheDiagnostic_NotInAStackOverflow()
    {
        // Before the budget, 10,000 terms overflowed the stack of the compiler; the parser accepts the chain because it does not nest.
        var compiled = Compile(Chain(100_000), null, out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathLimitErrors.RecursionDepthExceeded);
    }

    [Test]
    public async Task Compile_WithoutLimits_UsesTheDefaultDepth()
    {
        var atDefault = Compile(Chain(256), null, out _);
        var beyondDefault = Compile(Chain(257), null, out var diagnostic);

        await Assert.That(atDefault).IsNotNull();
        await Assert.That(beyondDefault).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathLimitErrors.RecursionDepthExceeded);
    }

    [Test]
    public async Task Evaluate_ACompiledExpressionDeeperThanTheContextLimit_EndsInTheDiagnostic()
    {
        var compiled = CompileOrThrow(Chain(10));
        using var context = NewContext(XPathLimits.Default.WithMaxRecursionDepth(9));

        var raised = ArithmeticFixture.Raised(() => compiled.Evaluate(context));

        await Assert.That(raised).IsNotNull();
        await Assert.That(raised!.Diagnostic.QName).IsEqualTo(XPathLimitErrors.RecursionDepthExceeded);
        await Assert.That(raised.Diagnostic.Message).Contains("MaxRecursionDepth");
        await Assert.That(raised.Diagnostic.Message).Contains("9");
    }

    [Test]
    public async Task Evaluate_ExactlyAtTheContextLimit_Succeeds()
    {
        var compiled = CompileOrThrow(Chain(10));
        using var context = NewContext(XPathLimits.Default.WithMaxRecursionDepth(10));

        var result = compiled.Evaluate(context);

        await Assert.That(result.Count).IsEqualTo(1);
        _ = result[0].TryGetInteger(out var sum);
        await Assert.That(sum).IsEqualTo(10L);
    }

    [Test]
    public async Task Compile_PredicatesAndPaths_CountTowardsTheDepth()
    {
        var limits = XPathLimits.Default.WithMaxRecursionDepth(3);

        var shallow = Compile("child::b[1]", limits, out _);
        var deep = Compile("child::b[(1+1)+1]", limits, out var diagnostic);

        await Assert.That(shallow).IsNotNull();
        await Assert.That(deep).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathLimitErrors.RecursionDepthExceeded);
    }
}
