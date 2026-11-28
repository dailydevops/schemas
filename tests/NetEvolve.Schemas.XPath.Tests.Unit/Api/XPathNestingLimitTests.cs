namespace NetEvolve.Schemas.XPath.Tests.Unit.Api;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Parser;

/// <summary>The nesting bound of the parser follows <see cref="XPathStaticContext.MaxRecursionDepth"/> (#640).</summary>
public class XPathNestingLimitTests
{
    private static string Parens(int depth) => new string('(', depth) + "1" + new string(')', depth);

    [Test]
    public async Task Default_IsTheParserBound()
    {
        await Assert.That(XPathStaticContext.Default.MaxRecursionDepth).IsEqualTo(XPathParser.MaxNestingDepth);
        await Assert.That(XPathStaticContext.Default.MaxRecursionDepth).IsEqualTo(100);
    }

    [Test]
    public async Task Builder_RoundTripsTheDepth_AndToBuilderKeepsIt()
    {
        var context = XPathStaticContext.Default.ToBuilder().WithMaxRecursionDepth(7).Build();

        await Assert.That(context.MaxRecursionDepth).IsEqualTo(7);
        await Assert.That(context.ToBuilder().Build().MaxRecursionDepth).IsEqualTo(7);
    }

    [Test]
    public async Task Builder_CeilingAndOne_AreAccepted()
    {
        var low = XPathStaticContext.Default.ToBuilder().WithMaxRecursionDepth(1).Build();
        var high = XPathStaticContext
            .Default.ToBuilder()
            .WithMaxRecursionDepth(XPathLimits.MaxRecursionDepthCeiling)
            .Build();

        await Assert.That(low.MaxRecursionDepth).IsEqualTo(1);
        await Assert.That(high.MaxRecursionDepth).IsEqualTo(XPathLimits.MaxRecursionDepthCeiling);
    }

    [Test]
    public async Task Builder_ValueBelowOneOrAboveTheCeiling_IsRejected()
    {
        var builder = XPathStaticContext.Default.ToBuilder();

        await Assert.That(() => builder.WithMaxRecursionDepth(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => builder.WithMaxRecursionDepth(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => builder.WithMaxRecursionDepth(XPathLimits.MaxRecursionDepthCeiling + 1))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Parser_FollowsTheGivenBound()
    {
        var below = XPathParser.TryParse(Parens(9).AsSpan(), XPathVersion.V20, 10, out var tree, out _);
        var above = XPathParser.TryParse(Parens(11).AsSpan(), XPathVersion.V20, 10, out var none, out var diagnostic);

        await Assert.That(below).IsTrue();
        await Assert.That(tree).IsNotNull();
        await Assert.That(above).IsFalse();
        await Assert.That(none).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0003);
    }

    [Test]
    public async Task Compile_DefaultContext_KeepsTheFixedBound()
    {
        var ok = XPathExpression.TryCompile(Parens(99), XPathStaticContext.Default);
        var rejected = XPathExpression.TryCompile(Parens(101), XPathStaticContext.Default);

        await Assert.That(ok.Expression).IsNotNull();
        await Assert.That(rejected.Expression).IsNull();
        await Assert.That(rejected.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0003);
    }

    [Test]
    public async Task Compile_LowerDepth_RejectsNestingAtTheBound_WithTheNestingErrorCode()
    {
        var context = XPathStaticContext.Default.ToBuilder().WithMaxRecursionDepth(10).Build();

        var ok = XPathExpression.TryCompile(Parens(9), context);
        var rejected = XPathExpression.TryCompile(Parens(11), context);

        await Assert.That(ok.Expression).IsNotNull();
        await Assert.That(rejected.Expression).IsNull();
        await Assert.That(rejected.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0003);
    }

    [Test]
    public async Task Compile_HigherDepth_AcceptsNestingBeyondTheDefault()
    {
        var context = XPathStaticContext.Default.ToBuilder().WithMaxRecursionDepth(200).Build();

        var result = XPathExpression.TryCompile(Parens(150), context);

        await Assert.That(result.Expression).IsNotNull();
    }

    [Test]
    public async Task Compile_ExplicitDepth_AlsoBoundsTheLowering()
    {
        // A left-deep chain does not nest in the grammar, so only the lowering stops it.
        var context = XPathStaticContext.Default.ToBuilder().WithMaxRecursionDepth(5).Build();

        var result = XPathExpression.TryCompile("1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1 + 1", context);

        await Assert.That(result.Expression).IsNull();
        await Assert.That(result.Diagnostic.QName).IsEqualTo(XPathLimitErrors.RecursionDepthExceeded);
    }
}
