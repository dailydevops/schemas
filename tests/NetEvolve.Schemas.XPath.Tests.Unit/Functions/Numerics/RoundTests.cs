namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Numerics;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// <c>fn:round</c> and <c>fn:round-half-to-even</c> (Functions and Operators 1.0, 6.4.4 and 6.4.5): the ties at .5 for positive and
/// negative values, negative precision, the sign of a zero, NaN, the infinities, and the result type for each numeric type.
/// </summary>
public class RoundTests
{
    // A tie rounds toward positive infinity: 2.5 gives 3 and -2.5 gives -2.
    [Test]
    [Arguments("integer:5", "integer:5")]
    [Arguments("integer:-5", "integer:-5")]
    [Arguments("decimal:2.5", "decimal:3")]
    [Arguments("decimal:-2.5", "decimal:-2")]
    [Arguments("decimal:3.5", "decimal:4")]
    [Arguments("decimal:-3.5", "decimal:-3")]
    [Arguments("decimal:0.5", "decimal:1")]
    [Arguments("decimal:-0.5", "decimal:0")]
    [Arguments("decimal:2.4999999999999999999999999", "decimal:2")]
    [Arguments("decimal:-2.5000000000000000000000001", "decimal:-3")]
    [Arguments("decimal:2", "decimal:2")]
    [Arguments("float:2.5", "float:3")]
    [Arguments("float:-2.5", "float:-2")]
    [Arguments("float:1.5", "float:2")]
    [Arguments("float:-1.5", "float:-1")]
    [Arguments("float:0.5", "float:1")]
    [Arguments("float:-0.5", "float:-0")]
    [Arguments("float:-0.3", "float:-0")]
    [Arguments("float:0.3", "float:0")]
    [Arguments("float:-0", "float:-0")]
    [Arguments("float:0", "float:0")]
    [Arguments("float:8388609", "float:8388609")]
    [Arguments("float:INF", "float:INF")]
    [Arguments("float:-INF", "float:-INF")]
    [Arguments("float:NaN", "float:NaN")]
    [Arguments("double:2.5", "double:3")]
    [Arguments("double:-2.5", "double:-2")]
    [Arguments("double:1.5", "double:2")]
    [Arguments("double:-1.5", "double:-1")]
    [Arguments("double:0.5", "double:1")]
    [Arguments("double:-0.5", "double:-0")]
    [Arguments("double:0.49999999999999994", "double:0")]
    [Arguments("double:-0.49999999999999994", "double:-0")]
    [Arguments("double:2.4999999999999996", "double:2")]
    [Arguments("double:-0.0000001", "double:-0")]
    [Arguments("double:-0", "double:-0")]
    [Arguments("double:0", "double:0")]
    [Arguments("double:4503599627370497", "double:4503599627370497")]
    [Arguments("double:-2251799813685248.5", "double:-2251799813685248")]
    [Arguments("double:9007199254740993", "double:9007199254740992")]
    [Arguments("double:1E300", "double:1E+300")]
    [Arguments("double:INF", "double:INF")]
    [Arguments("double:-INF", "double:-INF")]
    [Arguments("double:NaN", "double:NaN")]
    [Arguments("untypedAtomic:2.5", "double:3")]
    [Arguments("()", "")]
    public async Task Round_RoundsTiesTowardPositiveInfinity(string argument, string expected) =>
        await Assert.That(NumericsHarness.Run("round", argument)).IsEqualTo(expected);

    // A tie rounds to the even neighbor: 2.5 gives 2 and 3.5 gives 4.
    [Test]
    [Arguments("integer:5", "integer:5")]
    [Arguments("decimal:2.5", "decimal:2")]
    [Arguments("decimal:3.5", "decimal:4")]
    [Arguments("decimal:-2.5", "decimal:-2")]
    [Arguments("decimal:-3.5", "decimal:-4")]
    [Arguments("decimal:0.5", "decimal:0")]
    [Arguments("decimal:1.5", "decimal:2")]
    [Arguments("decimal:2.5000000000000000000000001", "decimal:3")]
    [Arguments("decimal:2", "decimal:2")]
    [Arguments("float:2.5", "float:2")]
    [Arguments("float:3.5", "float:4")]
    [Arguments("float:-2.5", "float:-2")]
    [Arguments("float:-3.5", "float:-4")]
    [Arguments("float:-0", "float:-0")]
    [Arguments("float:0", "float:0")]
    [Arguments("float:INF", "float:INF")]
    [Arguments("float:NaN", "float:NaN")]
    [Arguments("double:2.5", "double:2")]
    [Arguments("double:3.5", "double:4")]
    [Arguments("double:-2.5", "double:-2")]
    [Arguments("double:-3.5", "double:-4")]
    [Arguments("double:0.5", "double:0")]
    [Arguments("double:1.5", "double:2")]
    [Arguments("double:-0", "double:-0")]
    [Arguments("double:0", "double:0")]
    [Arguments("double:-INF", "double:-INF")]
    [Arguments("double:NaN", "double:NaN")]
    [Arguments("untypedAtomic:3.5", "double:4")]
    [Arguments("()", "")]
    public async Task RoundHalfToEven_RoundsTiesToTheEvenNeighbor(string argument, string expected) =>
        await Assert.That(NumericsHarness.Run("round-half-to-even", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("decimal:1.125", "integer:2", "decimal:1.12")]
    [Arguments("decimal:1.135", "integer:2", "decimal:1.14")]
    [Arguments("decimal:-1.125", "integer:2", "decimal:-1.12")]
    [Arguments("decimal:-1.135", "integer:2", "decimal:-1.14")]
    [Arguments("decimal:1.2", "integer:5", "decimal:1.2")]
    [Arguments("decimal:35", "integer:-1", "decimal:40")]
    [Arguments("decimal:25", "integer:-1", "decimal:20")]
    [Arguments("decimal:-25", "integer:-1", "decimal:-20")]
    [Arguments("decimal:1234.5678", "integer:-2", "decimal:1200")]
    [Arguments("decimal:1250.5", "integer:-2", "decimal:1300")]
    [Arguments("decimal:0.5", "integer:-9223372036854775808", "decimal:0")]
    [Arguments("decimal:1.25", "integer:9223372036854775807", "decimal:1.25")]
    [Arguments("integer:12345", "integer:-2", "integer:12300")]
    [Arguments("integer:12350", "integer:-2", "integer:12400")]
    [Arguments("integer:12250", "integer:-2", "integer:12200")]
    [Arguments("integer:-12350", "integer:-2", "integer:-12400")]
    [Arguments("integer:1234567", "integer:-3", "integer:1235000")]
    [Arguments("integer:12345", "integer:0", "integer:12345")]
    [Arguments("integer:12345", "integer:2", "integer:12345")]
    [Arguments("integer:12345", "integer:9223372036854775807", "integer:12345")]
    [Arguments("integer:12345", "integer:-5", "integer:0")]
    [Arguments("integer:12345", "integer:-9223372036854775808", "integer:0")]
    [Arguments("integer:-9223372036854775808", "integer:-1", "integer:-9223372036854775810")]
    [Arguments("double:2.5", "integer:0", "double:2")]
    [Arguments("double:0.125", "integer:2", "double:0.12")]
    [Arguments("double:0.375", "integer:2", "double:0.38")]
    [Arguments("double:-0.125", "integer:2", "double:-0.12")]
    [Arguments("double:1234.5", "integer:-2", "double:1200")]
    [Arguments("double:1250", "integer:-2", "double:1200")]
    [Arguments("double:1350", "integer:-2", "double:1400")]
    [Arguments("double:-0.4", "integer:0", "double:-0")]
    [Arguments("double:0.4", "integer:0", "double:0")]
    [Arguments("double:-0", "integer:2", "double:-0")]
    [Arguments("double:INF", "integer:2", "double:INF")]
    [Arguments("double:-INF", "integer:-2", "double:-INF")]
    [Arguments("double:NaN", "integer:2", "double:NaN")]
    [Arguments("double:1E300", "integer:-2", "double:1E+300")]
    [Arguments("double:1E-300", "integer:2", "double:0")]
    [Arguments("float:0.125", "integer:2", "float:0.12")]
    [Arguments("float:-0.05", "integer:1", "float:-0.1")]
    [Arguments("float:1250", "integer:-2", "float:1200")]
    [Arguments("float:-0", "integer:1", "float:-0")]
    [Arguments("float:INF", "integer:1", "float:INF")]
    [Arguments("float:NaN", "integer:1", "float:NaN")]
    [Arguments("untypedAtomic:0.125", "integer:2", "double:0.12")]
    // The examples of the specification, and the zero of a result that is zero: the sign of the original argument (6.4.5).
    // A float is cast through its exact binary value: the float nearest to 150.015 is 150.0149993896..., which rounds down.
    [Arguments("double:3567.812", "integer:2", "double:3567.81")]
    [Arguments("double:0.0047564", "integer:2", "double:0")]
    [Arguments("double:-0.0047564", "integer:2", "double:-0")]
    [Arguments("decimal:35612.25", "integer:-2", "decimal:35600")]
    [Arguments("float:150.015", "integer:2", "float:150.01")]
    [Arguments("decimal:1.125", "untypedAtomic:2", "decimal:1.12")]
    [Arguments("()", "integer:2", "")]
    public async Task RoundHalfToEven_WithAPrecision_RoundsToThatManyFractionDigits(
        string argument,
        string precision,
        string expected
    ) => await Assert.That(NumericsHarness.Run("round-half-to-even", argument, precision)).IsEqualTo(expected);

    [Test]
    [Arguments("decimal:2.5", "integer:10000000000000000000000000", "decimal:2.5")]
    [Arguments("decimal:2.5", "integer:-10000000000000000000000000", "decimal:0")]
    [Arguments("double:2.5", "integer:10000000000000000000000000", "double:2.5")]
    [Arguments("double:2.5", "integer:-10000000000000000000000000", "double:0")]
    [Arguments("integer:25", "integer:10000000000000000000000000", "integer:25")]
    [Arguments("integer:25", "integer:-10000000000000000000000000", "integer:0")]
    public async Task RoundHalfToEven_WithAPrecisionBeyondSixtyFourBits_KeepsOrClearsEveryDigit(
        string argument,
        string precision,
        string expected
    ) => await Assert.That(NumericsHarness.Run("round-half-to-even", argument, precision)).IsEqualTo(expected);

    [Test]
    [Arguments("decimal:1.5", "()")]
    [Arguments("decimal:1.5", "integer:1|integer:2")]
    [Arguments("decimal:1.5", "string:1")]
    [Arguments("decimal:1.5", "decimal:1.5")]
    [Arguments("decimal:1.5", "double:1")]
    public async Task RoundHalfToEven_ForAPrecisionThatIsNotOneInteger_RaisesXPTY0004(
        string argument,
        string precision
    ) => await Assert.That(NumericsHarness.Run("round-half-to-even", argument, precision)).IsEqualTo("error:XPTY0004");

    // fn:round with a precision (Functions and Operators 3.1, 4.4.4, #727): a tie toward positive infinity at the chosen precision.
    [Test]
    [Arguments("integer:12345", "integer:0", "integer:12345")]
    [Arguments("integer:12345", "integer:2", "integer:12345")]
    [Arguments("integer:12345", "integer:-2", "integer:12300")]
    [Arguments("integer:12350", "integer:-2", "integer:12400")]
    [Arguments("integer:12250", "integer:-2", "integer:12300")]
    [Arguments("integer:-12350", "integer:-2", "integer:-12300")]
    [Arguments("integer:-12250", "integer:-2", "integer:-12200")]
    [Arguments("integer:12345", "integer:-5", "integer:0")]
    [Arguments("integer:-9223372036854775808", "integer:-1", "integer:-9223372036854775810")]
    [Arguments("integer:123456789012345678901234567895", "integer:-1", "integer:123456789012345678901234567900")]
    [Arguments("integer:-123456789012345678901234567895", "integer:-1", "integer:-123456789012345678901234567890")]
    [Arguments("integer:123456789012345678901234567890", "integer:3", "integer:123456789012345678901234567890")]
    [Arguments("decimal:1.125", "integer:2", "decimal:1.13")]
    [Arguments("decimal:-1.125", "integer:2", "decimal:-1.12")]
    [Arguments("decimal:1.135", "integer:2", "decimal:1.14")]
    [Arguments("decimal:-1.135", "integer:2", "decimal:-1.13")]
    [Arguments("decimal:2.5", "integer:0", "decimal:3")]
    [Arguments("decimal:-2.5", "integer:0", "decimal:-2")]
    [Arguments("decimal:1.2", "integer:5", "decimal:1.2")]
    [Arguments("decimal:1250", "integer:-2", "decimal:1300")]
    [Arguments("decimal:-1250", "integer:-2", "decimal:-1200")]
    [Arguments("decimal:1234.5678", "integer:-2", "decimal:1200")]
    [Arguments("decimal:0.5", "integer:-9223372036854775808", "decimal:0")]
    [Arguments("decimal:1.25", "integer:9223372036854775807", "decimal:1.25")]
    [Arguments("decimal:123456789012345678901234567890.25", "integer:1", "decimal:123456789012345678901234567890.3")]
    [Arguments("double:2.5", "integer:0", "double:3")]
    [Arguments("double:-2.5", "integer:0", "double:-2")]
    [Arguments("double:0.125", "integer:2", "double:0.13")]
    [Arguments("double:-0.125", "integer:2", "double:-0.12")]
    [Arguments("double:1250", "integer:-2", "double:1300")]
    [Arguments("double:-1250", "integer:-2", "double:-1200")]
    [Arguments("double:-0.4", "integer:0", "double:-0")]
    [Arguments("double:0.4", "integer:0", "double:0")]
    [Arguments("double:-0", "integer:2", "double:-0")]
    [Arguments("double:0", "integer:2", "double:0")]
    [Arguments("double:INF", "integer:2", "double:INF")]
    [Arguments("double:-INF", "integer:-2", "double:-INF")]
    [Arguments("double:NaN", "integer:2", "double:NaN")]
    [Arguments("double:1E300", "integer:-2", "double:1E+300")]
    [Arguments("double:1E-300", "integer:2", "double:0")]
    [Arguments("double:-1E-300", "integer:2", "double:-0")]
    [Arguments("float:0.125", "integer:2", "float:0.13")]
    [Arguments("float:-0.125", "integer:2", "float:-0.12")]
    [Arguments("float:1250", "integer:-2", "float:1300")]
    [Arguments("float:-0.05", "integer:1", "float:-0.1")]
    [Arguments("float:-0", "integer:1", "float:-0")]
    [Arguments("float:-0.3", "integer:0", "float:-0")]
    [Arguments("float:INF", "integer:1", "float:INF")]
    [Arguments("float:-INF", "integer:1", "float:-INF")]
    [Arguments("float:NaN", "integer:1", "float:NaN")]
    // The examples of the specification.
    [Arguments("double:2.5", "integer:0", "double:3")]
    [Arguments("double:2.4999", "integer:0", "double:2")]
    [Arguments("double:-2.5", "integer:0", "double:-2")]
    [Arguments("double:1.125", "integer:2", "double:1.13")]
    [Arguments("double:8452", "integer:-1", "double:8450")]
    [Arguments("double:8455", "integer:-1", "double:8460")]
    [Arguments("double:-8455", "integer:-1", "double:-8450")]
    [Arguments("untypedAtomic:1.125", "integer:2", "double:1.13")]
    [Arguments("decimal:1.125", "untypedAtomic:2", "decimal:1.13")]
    [Arguments("()", "integer:2", "")]
    public async Task RoundWithAPrecision_RoundsTiesTowardPositiveInfinityInTheTypeOfTheArgument(
        string argument,
        string precision,
        string expected
    ) => await Assert.That(NumericsHarness.RunAt(XPathVersion.V31, "round", argument, precision)).IsEqualTo(expected);

    [Test]
    [Arguments("decimal:2.5", "integer:10000000000000000000000000", "decimal:2.5")]
    [Arguments("decimal:2.5", "integer:-10000000000000000000000000", "decimal:0")]
    [Arguments("double:2.5", "integer:10000000000000000000000000", "double:2.5")]
    [Arguments("double:2.5", "integer:-10000000000000000000000000", "double:0")]
    [Arguments("float:2.5", "integer:-10000000000000000000000000", "float:0")]
    [Arguments("integer:25", "integer:10000000000000000000000000", "integer:25")]
    [Arguments("integer:25", "integer:-10000000000000000000000000", "integer:0")]
    public async Task RoundWithAPrecision_BeyondSixtyFourBits_KeepsOrClearsEveryDigit(
        string argument,
        string precision,
        string expected
    ) => await Assert.That(NumericsHarness.RunAt(XPathVersion.V30, "round", argument, precision)).IsEqualTo(expected);

    [Test]
    [Arguments("decimal:1.5", "()")]
    [Arguments("decimal:1.5", "integer:1|integer:2")]
    [Arguments("decimal:1.5", "string:1")]
    [Arguments("decimal:1.5", "decimal:1.5")]
    [Arguments("decimal:1.5", "double:1")]
    [Arguments("string:1.5", "integer:1")]
    public async Task RoundWithAPrecision_ForAnArgumentOrPrecisionOfTheWrongType_RaisesXPTY0004(
        string argument,
        string precision
    ) =>
        await Assert
            .That(NumericsHarness.RunAt(XPathVersion.V31, "round", argument, precision))
            .IsEqualTo("error:XPTY0004");

    [Test]
    public async Task RoundWithAPrecision_ForAnUntypedArgumentThatIsNotADouble_RaisesFORG0001() =>
        await Assert
            .That(NumericsHarness.RunAt(XPathVersion.V31, "round", "untypedAtomic:abc", "integer:1"))
            .IsEqualTo("error:FORG0001");

    [Test]
    public async Task RoundWithAPrecision_InXPath20_IsNotRegistered() =>
        await Assert
            .That(NumericsHarness.RunAt(XPathVersion.V20, "round", "decimal:1.25", "integer:1"))
            .IsEqualTo("error:not-registered");
}
