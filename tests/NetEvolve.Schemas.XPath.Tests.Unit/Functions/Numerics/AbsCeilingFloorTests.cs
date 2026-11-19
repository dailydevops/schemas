namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Numerics;

using System.Threading.Tasks;

/// <summary>
/// <c>fn:abs</c>, <c>fn:ceiling</c>, and <c>fn:floor</c> (Functions and Operators 1.0, 6.4.1 to 6.4.3): the result value and the result
/// type for each numeric type (<c>xs:integer</c> stays <c>xs:integer</c>, <c>xs:decimal</c> stays <c>xs:decimal</c>, and so on), the
/// special floating-point values with the sign of a zero, <c>xs:untypedAtomic</c> as <c>xs:double</c>, and the empty sequence.
/// </summary>
public class AbsCeilingFloorTests
{
    [Test]
    [Arguments("integer:-5", "integer:5")]
    [Arguments("integer:5", "integer:5")]
    [Arguments("integer:0", "integer:0")]
    [Arguments("decimal:-2.50", "decimal:2.5")]
    [Arguments("decimal:2.5", "decimal:2.5")]
    [Arguments("decimal:-0.000001", "decimal:0.000001")]
    [Arguments("decimal:0", "decimal:0")]
    [Arguments("float:-1.5", "float:1.5")]
    [Arguments("float:1.5", "float:1.5")]
    [Arguments("float:-0", "float:0")]
    [Arguments("float:-INF", "float:INF")]
    [Arguments("float:NaN", "float:NaN")]
    [Arguments("double:-2.5", "double:2.5")]
    [Arguments("double:2.5", "double:2.5")]
    [Arguments("double:-0", "double:0")]
    [Arguments("double:0", "double:0")]
    [Arguments("double:-INF", "double:INF")]
    [Arguments("double:INF", "double:INF")]
    [Arguments("double:NaN", "double:NaN")]
    [Arguments("untypedAtomic:-2.5", "double:2.5")]
    [Arguments("untypedAtomic: -7 ", "double:7")]
    [Arguments("()", "")]
    public async Task Abs_ReturnsTheMagnitudeInTheTypeOfTheArgument(string argument, string expected) =>
        await Assert.That(NumericsHarness.Run("abs", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("string:-2")]
    [Arguments("boolean:true")]
    [Arguments("date:2024-05-17Z")]
    [Arguments("duration:PT1H")]
    [Arguments("integer:1|integer:2")]
    public async Task NumericFunctions_ForAnArgumentThatIsNotANumber_RaiseXPTY0004(string argument)
    {
        foreach (var function in (string[])["abs", "ceiling", "floor", "round", "round-half-to-even"])
        {
            await Assert.That(NumericsHarness.Run(function, argument)).IsEqualTo("error:XPTY0004");
        }
    }

    [Test]
    [Arguments("untypedAtomic:abc")]
    [Arguments("untypedAtomic:")]
    public async Task NumericFunctions_ForAnUntypedValueThatIsNotADouble_RaiseFORG0001(string argument)
    {
        foreach (var function in (string[])["abs", "ceiling", "floor", "round", "round-half-to-even"])
        {
            await Assert.That(NumericsHarness.Run(function, argument)).IsEqualTo("error:FORG0001");
        }
    }

    [Test]
    [Arguments("integer:7", "integer:7")]
    [Arguments("integer:-7", "integer:-7")]
    [Arguments("decimal:2.1", "decimal:3")]
    [Arguments("decimal:2", "decimal:2")]
    [Arguments("decimal:-2.9", "decimal:-2")]
    [Arguments("decimal:-0.5", "decimal:0")]
    [Arguments("decimal:0.000000000000000000000000000001", "decimal:1")]
    [Arguments("decimal:-0.000000000000000000000000000001", "decimal:0")]
    [Arguments("float:2.1", "float:3")]
    [Arguments("float:-2.1", "float:-2")]
    [Arguments("float:-0.3", "float:-0")]
    [Arguments("float:-0", "float:-0")]
    [Arguments("float:0", "float:0")]
    [Arguments("float:16777216", "float:16777216")]
    [Arguments("float:INF", "float:INF")]
    [Arguments("float:NaN", "float:NaN")]
    [Arguments("double:2.1", "double:3")]
    [Arguments("double:2", "double:2")]
    [Arguments("double:-2.9", "double:-2")]
    [Arguments("double:-0.5", "double:-0")]
    [Arguments("double:-0", "double:-0")]
    [Arguments("double:0", "double:0")]
    [Arguments("double:0.0000001", "double:1")]
    [Arguments("double:4503599627370497", "double:4503599627370497")]
    [Arguments("double:1E300", "double:1E+300")]
    [Arguments("double:-INF", "double:-INF")]
    [Arguments("double:NaN", "double:NaN")]
    [Arguments("untypedAtomic:2.1", "double:3")]
    [Arguments("()", "")]
    public async Task Ceiling_ReturnsTheSmallestIntegerNotLessThanTheArgument(string argument, string expected) =>
        await Assert.That(NumericsHarness.Run("ceiling", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("integer:7", "integer:7")]
    [Arguments("integer:-7", "integer:-7")]
    [Arguments("decimal:2.9", "decimal:2")]
    [Arguments("decimal:2", "decimal:2")]
    [Arguments("decimal:-2.1", "decimal:-3")]
    [Arguments("decimal:0.5", "decimal:0")]
    [Arguments("decimal:-0.000000000000000000000000000001", "decimal:-1")]
    [Arguments("float:2.9", "float:2")]
    [Arguments("float:-2.1", "float:-3")]
    [Arguments("float:0.3", "float:0")]
    [Arguments("float:-0", "float:-0")]
    [Arguments("float:INF", "float:INF")]
    [Arguments("float:NaN", "float:NaN")]
    [Arguments("double:2.9", "double:2")]
    [Arguments("double:-2.1", "double:-3")]
    [Arguments("double:0.5", "double:0")]
    [Arguments("double:-0", "double:-0")]
    [Arguments("double:0", "double:0")]
    [Arguments("double:-0.0000001", "double:-1")]
    [Arguments("double:-4503599627370497", "double:-4503599627370497")]
    [Arguments("double:-INF", "double:-INF")]
    [Arguments("double:NaN", "double:NaN")]
    [Arguments("untypedAtomic:-2.1", "double:-3")]
    [Arguments("()", "")]
    public async Task Floor_ReturnsTheLargestIntegerNotGreaterThanTheArgument(string argument, string expected) =>
        await Assert.That(NumericsHarness.Run("floor", argument)).IsEqualTo(expected);
}
