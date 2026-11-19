namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Numerics;

using System.Threading.Tasks;

/// <summary>
/// <c>fn:number</c> (Functions and Operators 1.0, 2.3): the value of the argument as an <c>xs:double</c>, NaN for the empty sequence and
/// for a value that cannot be converted, and the context item when the call has no argument.
/// </summary>
public class NumberTests
{
    [Test]
    [Arguments("string:12.5", "double:12.5")]
    [Arguments("string: 12.5 ", "double:12.5")]
    [Arguments("string:\t\r\n 7\n", "double:7")]
    [Arguments("string:+5", "double:5")]
    [Arguments("string:-5", "double:-5")]
    [Arguments("string:1e3", "double:1000")]
    [Arguments("string:1.5E-3", "double:0.0015")]
    [Arguments("string:INF", "double:INF")]
    [Arguments("string:-INF", "double:-INF")]
    [Arguments("string:NaN", "double:NaN")]
    [Arguments("string:-0", "double:-0")]
    [Arguments("string:0", "double:0")]
    [Arguments("untypedAtomic: 3 ", "double:3")]
    [Arguments("untypedAtomic:4.25", "double:4.25")]
    [Arguments("anyURI:5", "double:NaN")]
    public async Task Number_OfAString_IsTheDoubleOfItsLexicalForm(string argument, string expected) =>
        await Assert.That(NumericsHarness.Run("number", argument)).IsEqualTo(expected);

    [Test]
    [Arguments("string:abc")]
    [Arguments("string:")]
    [Arguments("string: ")]
    [Arguments("string:1 2")]
    [Arguments("string:0x10")]
    [Arguments("string:1,5")]
    [Arguments("string:inf")]
    [Arguments("string:nan")]
    [Arguments("string:+INF")]
    [Arguments("string:1e")]
    [Arguments("string:.")]
    [Arguments("untypedAtomic:abc")]
    [Arguments("untypedAtomic:")]
    [Arguments("anyURI:http://example.org/")]
    public async Task Number_OfAValueThatIsNotADouble_IsNaN(string argument) =>
        await Assert.That(NumericsHarness.Run("number", argument)).IsEqualTo("double:NaN");

    [Test]
    [Arguments("boolean:true", "double:1")]
    [Arguments("boolean:false", "double:0")]
    [Arguments("integer:5", "double:5")]
    [Arguments("integer:-5", "double:-5")]
    [Arguments("integer:9223372036854775807", "double:9.223372036854776E+18")]
    [Arguments("decimal:0.1", "double:0.1")]
    [Arguments("decimal:-2.5", "double:-2.5")]
    [Arguments("float:1.5", "double:1.5")]
    [Arguments("float:-0", "double:-0")]
    [Arguments("float:INF", "double:INF")]
    [Arguments("float:NaN", "double:NaN")]
    [Arguments("double:2.5", "double:2.5")]
    [Arguments("double:-0", "double:-0")]
    [Arguments("double:-INF", "double:-INF")]
    [Arguments("double:NaN", "double:NaN")]
    [Arguments("()", "double:NaN")]
    public async Task Number_OfANumberOrABoolean_IsTheDoubleOfTheValue(string argument, string expected) =>
        await Assert.That(NumericsHarness.Run("number", argument)).IsEqualTo(expected);

    // The cast to xs:double fails for these types, and a failed cast is NaN, not an error.
    [Test]
    [Arguments("date:2024-05-17Z")]
    [Arguments("duration:PT1H")]
    public async Task Number_OfAValueWithoutACastToDouble_IsNaN(string argument) =>
        await Assert.That(NumericsHarness.Run("number", argument)).IsEqualTo("double:NaN");

    [Test]
    [Arguments("integer:1|integer:2")]
    [Arguments("string:1|string:2")]
    public async Task Number_OfASequenceOfSeveralItems_RaisesXPTY0004(string argument) =>
        await Assert.That(NumericsHarness.Run("number", argument)).IsEqualTo("error:XPTY0004");

    // Decision (#271): a lexical form beyond the digit cap of the datatype layer (4,096 digits) is an implementation limit and not an
    // invalid form, so it raises FOCA0001 as in function conversion (#621, #660), where the specification would give a number or NaN.
    [Test]
    public async Task Number_OfALexicalFormBeyondTheDigitCap_RaisesFOCA0001() =>
        await Assert.That(NumericsHarness.Run("number", "string:" + new string('9', 5000))).IsEqualTo("error:FOCA0001");

    [Test]
    [Arguments("integer:7", "double:7")]
    [Arguments("double:-2.5", "double:-2.5")]
    [Arguments("decimal:0.5", "double:0.5")]
    [Arguments("boolean:true", "double:1")]
    [Arguments("string: 42 ", "double:42")]
    [Arguments("string:x", "double:NaN")]
    [Arguments("untypedAtomic:1e2", "double:100")]
    [Arguments("node:w1/0/0", "double:1")]
    [Arguments("node:d3/0@a", "double:9")]
    [Arguments("node:w1/0", "double:12")]
    [Arguments("node:d4/0", "double:NaN")]
    public async Task Number_WithoutAnArgument_IsTheNumberOfTheContextItem(string contextItem, string expected) =>
        await Assert.That(NumericsHarness.RunWithContextItem(contextItem, "number")).IsEqualTo(expected);

    [Test]
    public async Task Number_WithoutAnArgumentAndWithoutAContextItem_RaisesXPDY0002() =>
        await Assert.That(NumericsHarness.Run("number")).IsEqualTo("error:XPDY0002");

    [Test]
    public async Task Number_WithAnArgument_IgnoresTheContextItem() =>
        await Assert.That(NumericsHarness.RunWithContextItem("integer:7", "number", "string:3")).IsEqualTo("double:3");

    [Test]
    public async Task Number_WithAnEmptyArgument_IsNaNAlthoughTheContextItemExists() =>
        await Assert.That(NumericsHarness.RunWithContextItem("integer:7", "number", "()")).IsEqualTo("double:NaN");

    [Test]
    [Arguments("node:w1/0/0", "double:1")]
    [Arguments("node:d3/0@a", "double:9")]
    [Arguments("node:d4/0", "double:NaN")]
    public async Task Number_OfANode_IsTheNumberOfItsTypedValue(string argument, string expected) =>
        await Assert.That(NumericsHarness.Run("number", argument)).IsEqualTo(expected);
}
