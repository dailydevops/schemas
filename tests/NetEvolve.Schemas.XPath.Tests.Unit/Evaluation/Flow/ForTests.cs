namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static FlowFixture;

/// <summary>
/// The for expression (XPath 2.0, 3.7): the return expression is evaluated once per binding, with one or more range variables, in binding
/// order; an inner variable shadows an outer one of the same name and the binding is undone afterwards.
/// </summary>
public class ForTests
{
    [Test]
    [Arguments("for $x in (1, 2, 3) return $x * 2", "integer:2, integer:4, integer:6")]
    [Arguments("for $x in () return 1", "()")]
    [Arguments("for $x in (1, 2) return ()", "()")]
    [Arguments("for $x in 1 to 3 return $x * 2", "integer:2, integer:4, integer:6")]
    [Arguments("for $x in 3 to 1 return $x", "()")]
    [Arguments("for $x in (1, 3) return $x to $x + 1", "integer:1, integer:2, integer:3, integer:4")]
    [Arguments(
        "for $x in $numbers return ($x, $x)",
        "integer:1, integer:1, integer:2, integer:2, integer:3, integer:3"
    )]
    public async Task EvaluatesTheReturnExpression_OncePerItem(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("for $x in (1, 2), $y in (10, 20) return $x + $y", "integer:11, integer:21, integer:12, integer:22")]
    [Arguments(
        "for $x in (1, 2), $y in (10, 20) return ($x, $y)",
        "integer:1, integer:10, integer:1, integer:20, integer:2, integer:10, integer:2, integer:20"
    )]
    [Arguments("for $x in (1, 2), $y in ($x, $x + 10) return $y", "integer:1, integer:11, integer:2, integer:12")]
    [Arguments(
        "for $x in 1 to 2, $y in 1 to 2, $z in 1 to 2 return $x * 100 + $y * 10 + $z",
        "integer:111, integer:112, integer:121, integer:122, integer:211, integer:212, integer:221, integer:222"
    )]
    [Arguments("for $x in (1, 2), $y in () return $x", "()")]
    [Arguments("for $x in (), $y in (1, 2) return $y", "()")]
    public async Task WithSeveralVariables_ReturnsTheResultsInBindingOrder(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("for $x in (1, 2), $x in ($x * 10) return $x", "integer:10, integer:20")]
    [Arguments(
        "for $x in (1, 2) return (for $x in (10, 20) return $x + 1)",
        "integer:11, integer:21, integer:11, integer:21"
    )]
    [Arguments(
        "for $x in (1, 2) return ((for $x in (10) return $x), $x)",
        "integer:10, integer:1, integer:10, integer:2"
    )]
    [Arguments(
        "for $x in (1, 2) return (for $y in ($x, $x) return $x + $y)",
        "integer:2, integer:2, integer:4, integer:4"
    )]
    [Arguments("(for $host in (1) return $host), $host", "integer:1, integer:100")]
    [Arguments("for $host in ($host + 1) return $host", "integer:101")]
    [Arguments("for $x in (1, 2), $host in ($host + $x) return $host", "integer:101, integer:102")]
    public async Task AnInnerVariable_ShadowsAnOuterOne_AndTheOuterOneIsBackAfterwards(
        string expression,
        string expected
    ) => await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    public async Task AVariableThatWasNotBound_IsNotBoundAfterTheExpression()
    {
        using var context = NewContext();
        _ = Compile("for $unbound in (1, 2) return $unbound").Evaluate(context);

        var error = Assert.Throws<XPathException>(() => Compile("$unbound").Evaluate(context));

        await Assert.That(error.Diagnostic.Code).IsEqualTo("XPDY0002");
        await Assert.That(context.Variables.TryGet(Unbound, out _)).IsFalse();
    }

    [Test]
    public async Task WhenTheReturnExpressionFails_TheBindingIsUndoneAndTheErrorPropagates()
    {
        using var context = NewContext();

        var error = Assert.Throws<XPathException>(() =>
            Compile("for $unbound in (1, 2), $host in (5) return boom()").Evaluate(context)
        );

        await Assert.That(error.Diagnostic.Code).IsEqualTo("FORG0001");
        await Assert.That(context.Variables.TryGet(Unbound, out _)).IsFalse();
        await Assert.That(ArithmeticFixture.Describe(context.Variables.Get(Host))).IsEqualTo("integer:100");
    }

    [Test]
    public async Task EvaluatesTheSourceOncePerOuterBinding_AndTheReturnOncePerTuple()
    {
        var probe = new Probe();

        _ = Run("for $x in (1, 2, 3), $y in (tick(), tick()) return tock()", probe);

        await Assert.That(probe.First).IsEqualTo(6);
        await Assert.That(probe.Second).IsEqualTo(6);
    }

    [Test]
    public async Task TheFocus_IsNotChanged()
    {
        using var context = NewContext();
        context.SetContextItem(XdmItem.FromInteger(9), 4, 5);

        _ = Compile("for $x in (1, 2) return $x").Evaluate(context);

        await Assert.That(context.Position).IsEqualTo(4);
        await Assert.That(context.Size).IsEqualTo(5);
    }

    [Test]
    public async Task TheSequenceSizeLimit_BoundsTheResult()
    {
        var limits = XPathLimits.Default.WithMaxSequenceSize(3);
        var exceeded = XPathLimitErrors.SequenceSizeExceeded.LocalName;

        await Assert.That(Code("for $x in (1, 2, 3) return $x", limits: limits)).IsEqualTo(string.Empty);
        await Assert.That(Code("for $x in (1, 2, 3, 4) return $x", limits: limits)).IsEqualTo(exceeded);
        await Assert.That(Code("for $x in (1, 2), $y in (1, 2) return $x", limits: limits)).IsEqualTo(exceeded);
        await Assert.That(Code("for $x in (1, 2) return ($x, $x)", limits: limits)).IsEqualTo(exceeded);
    }

    [Test]
    public async Task ARangeSource_IsChargedAgainstTheRangeSizeLimit() =>
        await Assert
            .That(Code("for $x in 1 to 11 return 1", limits: XPathLimits.Default.WithMaxRangeSize(10)))
            .IsEqualTo(XPathLimitErrors.RangeSizeExceeded.LocalName);

    [Test]
    [Arguments("for $x in 1 to 'a' return $x", "XPTY0004")]
    [Arguments("for $x in (1, 2) return $x to 'a'", "XPTY0004")]
    [Arguments("for $x in $unbound return 1", "XPDY0002")]
    public async Task ErrorsOfTheSourceAndTheReturnExpression_Propagate(string expression, string code) =>
        await Assert.That(Code(expression)).IsEqualTo(code);

    [Test]
    [Arguments("for $x in $x return 1")]
    [Arguments("for $x in (1) return $y")]
    [Arguments("(for $x in (1) return $x), $x")]
    [Arguments("for $x in $y, $y in (1) return 1")]
    public async Task AVariable_IsInScopeOnlyAfterItsBinding_AndInsideTheReturnExpression(string expression)
    {
        var compiled = TryCompile(expression, out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.Code).IsEqualTo("XPST0008");
    }

    [Test]
    public async Task AVariableWithAnUndeclaredPrefix_IsAStaticError()
    {
        var compiled = TryCompile("for $p:x in (1) return 1", out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.Code).IsEqualTo("XPST0081");
    }

    [Test]
    public async Task ManyBindings_AreBoundedByTheRecursionDepthLimit_NotByTheStack()
    {
        await Assert.That(Code(ForWithBindings(20))).IsEqualTo(string.Empty);

        var compiled = TryCompile(ForWithBindings(300), out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathLimitErrors.RecursionDepthExceeded);
    }
}
