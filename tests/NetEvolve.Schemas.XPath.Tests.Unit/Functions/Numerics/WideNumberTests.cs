namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Numerics;

using System.Threading.Tasks;

/// <summary>
/// An <c>xs:integer</c> or <c>xs:decimal</c> beyond 64 bits is exact in every function that takes a number: the result is the
/// <c>xs:integer</c> or <c>xs:decimal</c> of the exact value, not a rounded double.
/// </summary>
public class WideNumberTests
{
    [Test]
    [Arguments("abs", "integer:-9223372036854775808", "integer:9223372036854775808")]
    [Arguments("abs", "integer:-9223372036854775809", "integer:9223372036854775809")]
    [Arguments("abs", "integer:9223372036854775809", "integer:9223372036854775809")]
    [Arguments("abs", "integer:-123456789012345678901234567890", "integer:123456789012345678901234567890")]
    [Arguments("ceiling", "integer:123456789012345678901234567890", "integer:123456789012345678901234567890")]
    [Arguments("ceiling", "integer:-123456789012345678901234567890", "integer:-123456789012345678901234567890")]
    [Arguments("floor", "integer:123456789012345678901234567890", "integer:123456789012345678901234567890")]
    [Arguments("round", "integer:123456789012345678901234567890", "integer:123456789012345678901234567890")]
    [Arguments(
        "round-half-to-even",
        "integer:-123456789012345678901234567890",
        "integer:-123456789012345678901234567890"
    )]
    [Arguments("abs", "decimal:-123456789012345678901234567890.5", "decimal:123456789012345678901234567890.5")]
    [Arguments("ceiling", "decimal:123456789012345678901234567890.5", "decimal:123456789012345678901234567891")]
    [Arguments("ceiling", "decimal:-123456789012345678901234567890.5", "decimal:-123456789012345678901234567890")]
    [Arguments("floor", "decimal:123456789012345678901234567890.5", "decimal:123456789012345678901234567890")]
    [Arguments("floor", "decimal:-123456789012345678901234567890.5", "decimal:-123456789012345678901234567891")]
    [Arguments("round", "decimal:123456789012345678901234567890.5", "decimal:123456789012345678901234567891")]
    [Arguments("round", "decimal:-123456789012345678901234567890.5", "decimal:-123456789012345678901234567890")]
    [Arguments(
        "round-half-to-even",
        "decimal:123456789012345678901234567890.5",
        "decimal:123456789012345678901234567890"
    )]
    [Arguments(
        "round-half-to-even",
        "decimal:123456789012345678901234567891.5",
        "decimal:123456789012345678901234567892"
    )]
    public async Task Functions_AreExactBeyond64Bits(string function, string argument, string expected) =>
        await Assert.That(NumericsHarness.Run(function, argument)).IsEqualTo(expected);

    [Test]
    [Arguments("integer:123456789012345678901234567890", "integer:-3", "integer:123456789012345678901234568000")]
    [Arguments("integer:123456789012345678901234567500", "integer:-3", "integer:123456789012345678901234568000")]
    [Arguments("integer:123456789012345678901234566500", "integer:-3", "integer:123456789012345678901234566000")]
    [Arguments("integer:-123456789012345678901234567890", "integer:-30", "integer:0")]
    [Arguments("integer:123456789012345678901234567890", "integer:2", "integer:123456789012345678901234567890")]
    [Arguments("decimal:123456789012345678901234567890.125", "integer:2", "decimal:123456789012345678901234567890.12")]
    [Arguments("decimal:123456789012345678901234567890.135", "integer:2", "decimal:123456789012345678901234567890.14")]
    public async Task RoundHalfToEven_WithAPrecision_IsExactBeyond64Bits(
        string argument,
        string precision,
        string expected
    ) => await Assert.That(NumericsHarness.Run("round-half-to-even", argument, precision)).IsEqualTo(expected);

    [Test]
    public async Task Number_OfAnIntegerBeyond64Bits_IsTheNearestDouble() =>
        await Assert
            .That(NumericsHarness.Run("number", "integer:9223372036854775808"))
            .IsEqualTo("double:9.223372036854776E+18");
}
