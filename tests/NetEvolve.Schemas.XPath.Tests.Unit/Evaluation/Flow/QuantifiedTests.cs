namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using static FlowFixture;

/// <summary>
/// The quantified expressions some and every (XPath 2.0, 3.9): the empty binding sequence gives false for some and true for every, and the
/// evaluation stops at the first binding that decides.
/// </summary>
public class QuantifiedTests
{
    [Test]
    [Arguments("some $x in () satisfies true()", "boolean:False")]
    [Arguments("every $x in () satisfies false()", "boolean:True")]
    [Arguments("some $x in (1, 2), $y in () satisfies true()", "boolean:False")]
    [Arguments("every $x in (1, 2), $y in () satisfies false()", "boolean:True")]
    [Arguments("some $x in (), $y in (1, 2) satisfies true()", "boolean:False")]
    [Arguments("every $x in 3 to 1 satisfies false()", "boolean:True")]
    public async Task OverTheEmptySequence_SomeIsFalse_AndEveryIsTrue(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("some $x in (1, 2, 3) satisfies $x = 2", "boolean:True")]
    [Arguments("some $x in (1, 2, 3) satisfies $x = 5", "boolean:False")]
    [Arguments("every $x in (1, 2, 3) satisfies $x > 0", "boolean:True")]
    [Arguments("every $x in (1, 2, 3) satisfies $x < 3", "boolean:False")]
    [Arguments("some $x in (1, 2), $y in (3, 4) satisfies $x + $y = 6", "boolean:True")]
    [Arguments("some $x in (1, 2), $y in (3, 4) satisfies $x + $y = 9", "boolean:False")]
    [Arguments("every $x in (1, 2), $y in (3, 4) satisfies $x < $y", "boolean:True")]
    [Arguments("every $x in (1, 2), $y in (0, 4) satisfies $x < $y", "boolean:False")]
    [Arguments("some $x in 1 to 5 satisfies $x = 5", "boolean:True")]
    [Arguments("some $x in (1, 2), $y in ($x, $x + 1) satisfies $y = 3", "boolean:True")]
    [Arguments("every $x in (1, 2), $y in ($x, $x + 1) satisfies $y >= $x", "boolean:True")]
    public async Task ChecksTheBindings(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("some $x in (1, 2) satisfies $x", "boolean:True")]
    [Arguments("some $x in (0, 0) satisfies $x", "boolean:False")]
    [Arguments("every $x in (0, 1) satisfies $x", "boolean:False")]
    [Arguments("every $x in (1, 2) satisfies $x", "boolean:True")]
    [Arguments("some $x in (1) satisfies ()", "boolean:False")]
    [Arguments("every $x in (1) satisfies ()", "boolean:False")]
    [Arguments("some $x in (1) satisfies 'a'", "boolean:True")]
    public async Task TheSatisfiesExpression_UsesTheEffectiveBooleanValue(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("some $x in (1) satisfies ($x, $x)")]
    [Arguments("every $x in (1) satisfies ($x, $x)")]
    public async Task ASatisfiesExpression_WithoutAnEffectiveBooleanValue_RaisesForg0006(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("FORG0006");

    [Test]
    [Arguments("some $x in (1, 2, 3, 4) satisfies tick()", 1)]
    [Arguments("some $x in (1, 2, 3, 4) satisfies tickf()", 4)]
    [Arguments("every $x in (1, 2, 3, 4) satisfies tickf()", 1)]
    [Arguments("every $x in (1, 2, 3, 4) satisfies tick()", 4)]
    [Arguments("some $x in (1, 2), $y in (1, 2) satisfies tick()", 1)]
    [Arguments("some $x in (1, 2), $y in (1, 2) satisfies tickf()", 4)]
    [Arguments("every $x in (1, 2), $y in (1, 2) satisfies tickf()", 1)]
    [Arguments("every $x in (1, 2), $y in (1, 2) satisfies tick()", 4)]
    [Arguments("some $x in 1 to 1000 satisfies tick()", 1)]
    [Arguments("every $x in 1 to 1000 satisfies tickf()", 1)]
    public async Task StopsAtTheFirstDecidingBinding(string expression, int calls)
    {
        var probe = new Probe();

        _ = Run(expression, probe);

        await Assert.That(probe.First).IsEqualTo(calls);
    }

    [Test]
    [Arguments(
        "some $x in (1, 2, 3) satisfies (if ($x = 2) then true() else if ($x = 1) then false() else boom())",
        ""
    )]
    [Arguments(
        "every $x in (1, 2, 3) satisfies (if ($x = 1) then true() else if ($x = 2) then false() else boom())",
        ""
    )]
    [Arguments("some $x in (1, 2, 3) satisfies (if ($x = 3) then boom() else false())", "FORG0001")]
    public async Task ABindingAfterTheDecidingOne_IsNeverEvaluated(string expression, string code) =>
        await Assert.That(Code(expression)).IsEqualTo(code);

    [Test]
    public async Task AnEarlyExit_OverAHugeRange_NeverReadsTheRest()
    {
        var limits = XPathLimits.Default.WithMaxRangeSize(1_000_000_000);

        await Assert.That(Run("some $x in 1 to 1000000000 satisfies $x = 3", limits: limits)).IsEqualTo("boolean:True");
        await Assert
            .That(Run("every $x in 1 to 1000000000 satisfies $x < 3", limits: limits))
            .IsEqualTo("boolean:False");
    }

    [Test]
    public async Task ARangeSource_IsChargedAgainstTheRangeSizeLimit() =>
        await Assert
            .That(Code("some $x in 1 to 11 satisfies true()", limits: XPathLimits.Default.WithMaxRangeSize(10)))
            .IsEqualTo(XPathLimitErrors.RangeSizeExceeded.LocalName);

    [Test]
    [Arguments("some $x in (1, 2), $x in ($x * 10) satisfies $x = 20", "boolean:True")]
    [Arguments("some $x in (1, 2), $x in ($x * 10) satisfies $x = 2", "boolean:False")]
    [Arguments("every $x in (1, 2) satisfies (some $x in (10) satisfies $x = 10)", "boolean:True")]
    [Arguments("every $x in (1, 2) satisfies (some $x in (10) satisfies $x = 10) and $x < 3", "boolean:True")]
    [Arguments("(some $host in (1) satisfies $host = 1) and $host = 100", "boolean:True")]
    public async Task AnInnerVariable_ShadowsAnOuterOne(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    public async Task TheBindings_AreUndone_AfterTheDecision_AndAfterAnError()
    {
        using var context = NewContext();

        _ = Compile("some $unbound in (1, 2) satisfies $unbound = 1").Evaluate(context);
        var afterDecision = context.Variables.TryGet(Unbound, out _);
        var error = Assert.Throws<XPathException>(() =>
            Compile("every $unbound in (1, 2) satisfies boom()").Evaluate(context)
        );

        await Assert.That(afterDecision).IsFalse();
        await Assert.That(error.Diagnostic.Code).IsEqualTo("FORG0001");
        await Assert.That(context.Variables.TryGet(Unbound, out _)).IsFalse();
    }

    [Test]
    [Arguments("some $x in (1) satisfies $y", "XPST0008")]
    [Arguments("some $x in $x satisfies true()", "XPST0008")]
    [Arguments("every $p:x in (1) satisfies true()", "XPST0081")]
    public async Task AVariable_IsInScopeOnlyInTheSatisfiesExpressionAndLaterBindings(string expression, string code)
    {
        var compiled = TryCompile(expression, out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.Code).IsEqualTo(code);
    }

    [Test]
    public async Task TheResult_IsABoolean()
    {
        var some = Evaluate("some $x in (1) satisfies true()");

        await Assert.That(some.Count).IsEqualTo(1);
        await Assert.That(ArithmeticFixture.Describe(some)).IsEqualTo("boolean:True");
    }
}
