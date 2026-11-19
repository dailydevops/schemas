namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Numerics;

using System.Threading.Tasks;

/// <summary>
/// <c>fn:true</c>, <c>fn:false</c>, <c>fn:boolean</c>, and <c>fn:not</c> (Functions and Operators 1.0, section 9): the first two are the
/// constants, the other two wrap the effective boolean value routine (XPath 2.0, 2.4.3) and so raise FORG0006 where it does.
/// </summary>
public class BooleanTests
{
    [Test]
    public async Task True_IsTheBooleanTrue() =>
        await Assert.That(NumericsHarness.Run("true")).IsEqualTo("boolean:true");

    [Test]
    public async Task False_IsTheBooleanFalse() =>
        await Assert.That(NumericsHarness.Run("false")).IsEqualTo("boolean:false");

    [Test]
    public async Task TrueAndFalse_IgnoreTheContextItem()
    {
        await Assert.That(NumericsHarness.RunWithContextItem("integer:0", "true")).IsEqualTo("boolean:true");
        await Assert.That(NumericsHarness.RunWithContextItem("integer:1", "false")).IsEqualTo("boolean:false");
    }

    [Test]
    [Arguments("()", "boolean:false")]
    [Arguments("boolean:true", "boolean:true")]
    [Arguments("boolean:false", "boolean:false")]
    [Arguments("string:", "boolean:false")]
    [Arguments("string:a", "boolean:true")]
    [Arguments("string:false", "boolean:true")]
    [Arguments("string:0", "boolean:true")]
    [Arguments("untypedAtomic:", "boolean:false")]
    [Arguments("untypedAtomic:0", "boolean:true")]
    [Arguments("anyURI:", "boolean:false")]
    [Arguments("anyURI:u", "boolean:true")]
    [Arguments("integer:0", "boolean:false")]
    [Arguments("integer:-3", "boolean:true")]
    [Arguments("integer:123456789012345678901234567890", "boolean:true")]
    [Arguments("decimal:0.0", "boolean:false")]
    [Arguments("decimal:0.5", "boolean:true")]
    [Arguments("float:0", "boolean:false")]
    [Arguments("float:-0", "boolean:false")]
    [Arguments("float:NaN", "boolean:false")]
    [Arguments("float:INF", "boolean:true")]
    [Arguments("double:0", "boolean:false")]
    [Arguments("double:-0", "boolean:false")]
    [Arguments("double:NaN", "boolean:false")]
    [Arguments("double:-INF", "boolean:true")]
    [Arguments("double:0.1", "boolean:true")]
    [Arguments("node:w1/0", "boolean:true")]
    [Arguments("node:d3/0@a", "boolean:true")]
    [Arguments("node:w1/0|node:w1/0/0", "boolean:true")]
    [Arguments("node:w1/0|integer:0", "boolean:true")]
    public async Task Boolean_IsTheEffectiveBooleanValue(string argument, string expected) =>
        await Assert.That(NumericsHarness.Run("boolean", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("()", "boolean:true")]
    [Arguments("boolean:true", "boolean:false")]
    [Arguments("boolean:false", "boolean:true")]
    [Arguments("string:", "boolean:true")]
    [Arguments("string:a", "boolean:false")]
    [Arguments("integer:0", "boolean:true")]
    [Arguments("integer:2", "boolean:false")]
    [Arguments("decimal:0", "boolean:true")]
    [Arguments("float:NaN", "boolean:true")]
    [Arguments("double:NaN", "boolean:true")]
    [Arguments("double:1", "boolean:false")]
    [Arguments("node:w1/0", "boolean:false")]
    [Arguments("node:w1/0|node:w1/0/0", "boolean:false")]
    public async Task Not_IsTheNegationOfTheEffectiveBooleanValue(string argument, string expected) =>
        await Assert.That(NumericsHarness.Run("not", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("integer:1|integer:2")]
    [Arguments("boolean:true|boolean:true")]
    [Arguments("string:a|string:b")]
    [Arguments("integer:1|node:w1/0")]
    [Arguments("date:2024-05-17Z")]
    [Arguments("duration:PT1H")]
    public async Task BooleanAndNot_ForAValueWithoutAnEffectiveBooleanValue_RaiseFORG0006(string argument)
    {
        await Assert.That(NumericsHarness.Run("boolean", argument)).IsEqualTo("error:FORG0006");
        await Assert.That(NumericsHarness.Run("not", argument)).IsEqualTo("error:FORG0006");
    }
}
