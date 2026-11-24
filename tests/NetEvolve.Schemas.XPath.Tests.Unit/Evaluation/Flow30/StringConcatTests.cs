namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow30;

using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static Flow30Fixture;

/// <summary>The string concatenation operator (XPath 3.0, 3.8): both operands are atomized, an empty operand is the zero-length string.</summary>
public class StringConcatTests
{
    [Test]
    [Arguments("'a' || 'b'", "string:ab")]
    [Arguments("'a' || 'b' || 'c'", "string:abc")]
    [Arguments("1 || 2", "string:12")]
    [Arguments("'x' || 1.5", "string:x1.5")]
    [Arguments("'a' || ()", "string:a")]
    [Arguments("() || 'b'", "string:b")]
    [Arguments("1 + 2 || 3", "string:33")]
    public async Task JoinsTheStringValues(string expression, string expected) =>
        await Assert.That(Run(expression)).IsEqualTo(expected);

    [Test]
    public async Task AResultLongerThanTheScratchBuffer_IsJoinedInFull()
    {
        var left = new string('x', 300);

        await Assert.That(Run($"'{left}' || 'yz' || 3")).IsEqualTo($"string:{left}yz3");
    }

    [Test]
    [Arguments("() || ()")]
    [Arguments("() || () || ()")]
    public async Task EmptyOperands_YieldTheZeroLengthString(string expression) =>
        await Assert.That(Run(expression)).IsEqualTo("string:");

    [Test]
    [Arguments("(1, 2) || 'a'")]
    [Arguments("'a' || (1, 2)")]
    [Arguments("'a' || 'b' || (1, 2)")]
    public async Task AnOperandWithMoreThanOneItem_RaisesXPTY0004(string expression) =>
        await Assert.That(Code(expression)).IsEqualTo("XPTY0004");

    [Test]
    public async Task AResultLongerThanTheStringLimit_RaisesTheLimitError()
    {
        var limits = XPathLimits.Default.WithMaxStringLength(5);

        await Assert.That(Code("'abc' || 'def'", limits)).IsEqualTo("XP0006");
        await Assert.That(Code("'abc' || 'de'", limits)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task AChainOfFiveOperators_AllocatesOneResultString(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var compiled = Compile("'ab' || 'cd' || 'ef' || 'gh' || 'ij' || 'kl'");
        using var context = new XPathEvaluationContext();
        var result = XdmSequence.Empty;

        // One string of twelve characters: the object header, the length, the characters, and the terminator, aligned.
        AllocationAssert.AtMost(64, () => result = compiled.Evaluate(context), warmups: 50);

        await Assert.That(ArithmeticFixture.Describe(result)).IsEqualTo("string:abcdefghijkl");
    }
}
