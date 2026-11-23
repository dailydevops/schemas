namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Nodes;

using System.Threading.Tasks;

/// <summary>
/// <c>fn:string</c> and <c>fn:data</c> (Functions and Operators 1.0, 2.1 and 2.4): the string value of a node or an atomic value,
/// and the atomized sequence.
/// </summary>
public class AccessorTests
{
    [Test]
    [Arguments("doc", "string:t1")]
    [Arguments("r", "string:t1")]
    [Arguments("a", "string:t1")]
    [Arguments("a/t", "string:t1")]
    [Arguments("a@x", "string:1")]
    [Arguments("r@lang", "string:en-US")]
    [Arguments("cm", "string:cm")]
    [Arguments("pi", "string:data")]
    [Arguments("h", "string:")]
    [Arguments("o", "string:")]
    public async Task String_OfANode_IsItsStringValue(string node, string expected)
    {
        await Assert.That(NodesHarness.Run("string", "node:" + node)).IsEqualTo(expected);
        await Assert.That(NodesHarness.RunWithContextItem("node:" + node, "string")).IsEqualTo(expected);
    }

    [Test]
    [Arguments("string:abc", "string:abc")]
    [Arguments("untypedAtomic:xyz", "string:xyz")]
    [Arguments("anyURI:urn:q", "string:urn:q")]
    [Arguments("boolean:true", "string:true")]
    [Arguments("boolean:false", "string:false")]
    [Arguments("integer:-42", "string:-42")]
    [Arguments("integer:0", "string:0")]
    [Arguments("decimal:1.10", "string:1.1")]
    [Arguments("decimal:3.0", "string:3")]
    [Arguments("decimal:-0.05", "string:-0.05")]
    [Arguments("double:1", "string:1")]
    [Arguments("double:100", "string:100")]
    [Arguments("double:0.5", "string:0.5")]
    [Arguments("double:-2.25", "string:-2.25")]
    [Arguments("double:123456.5", "string:123456.5")]
    [Arguments("double:0.000001", "string:0.000001")]
    [Arguments("double:1E6", "string:1.0E6")]
    [Arguments("double:1234567", "string:1.234567E6")]
    [Arguments("double:1E-7", "string:1.0E-7")]
    [Arguments("double:1.5E300", "string:1.5E300")]
    [Arguments("double:0", "string:0")]
    [Arguments("double:-0", "string:-0")]
    [Arguments("double:NaN", "string:NaN")]
    [Arguments("double:INF", "string:INF")]
    [Arguments("double:-INF", "string:-INF")]
    [Arguments("float:3.14", "string:3.14")]
    [Arguments("float:1E10", "string:1.0E10")]
    [Arguments("float:16777216", "string:1.6777216E7")]
    [Arguments("float:0.000001", "string:0.000001")]
    [Arguments("float:NaN", "string:NaN")]
    [Arguments("date:2024-05-17Z", "string:2024-05-17Z")]
    [Arguments("dateTime:2024-05-17T13:45:30+02:00", "string:2024-05-17T13:45:30+02:00")]
    [Arguments("duration:PT1H", "string:PT1H")]
    [Arguments("yearMonthDuration:P1Y2M", "string:P1Y2M")]
    [Arguments("hexBinary:0fb7", "string:0FB7")]
    [Arguments("base64Binary:AQID", "string:AQID")]
    public async Task String_OfAnAtomicValue_IsItsCanonicalForm(string argument, string expected)
    {
        await Assert.That(NodesHarness.Run("string", argument)).IsEqualTo(expected);
        await Assert.That(NodesHarness.RunWithContextItem(argument, "string")).IsEqualTo(expected);
    }

    [Test]
    public async Task String_OfTheEmptySequence_IsTheZeroLengthString() =>
        await Assert.That(NodesHarness.Run("string", "()")).IsEqualTo("string:");

    [Test]
    public async Task String_WithoutArgumentAndWithoutContextItem_RaisesXPDY0002() =>
        await Assert.That(NodesHarness.Run("string")).IsEqualTo("error:XPDY0002");

    [Test]
    [Arguments("node:a|node:b")]
    [Arguments("integer:1|integer:2")]
    public async Task String_OfSeveralItems_RaisesXPTY0004(string argument) =>
        await Assert.That(NodesHarness.Run("string", argument)).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task String_WithTwoArguments_IsNotRegistered() =>
        await Assert.That(NodesHarness.Run("string", "node:a", "node:b")).IsEqualTo("error:not-registered");

    [Test]
    [Arguments("node:a", "untypedAtomic:t1")]
    [Arguments("node:r", "untypedAtomic:t1")]
    [Arguments("node:a@x", "untypedAtomic:1")]
    [Arguments("node:a/t", "untypedAtomic:t1")]
    [Arguments("node:cm", "string:cm")]
    [Arguments("node:pi", "string:data")]
    [Arguments("integer:5", "integer:5")]
    [Arguments("string:s", "string:s")]
    [Arguments("boolean:true", "boolean:true")]
    [Arguments("node:a@x|integer:5|node:cm", "untypedAtomic:1,integer:5,string:cm")]
    [Arguments("()", "")]
    public async Task Data_AtomizesTheArgument(string argument, string expected) =>
        await Assert.That(NodesHarness.Run("data", argument)).IsEqualTo(expected);

    [Test]
    public async Task Data_WithoutArgument_IsNotRegisteredInXPath20() =>
        await Assert.That(NodesHarness.Run("data")).IsEqualTo("error:not-registered");
}
