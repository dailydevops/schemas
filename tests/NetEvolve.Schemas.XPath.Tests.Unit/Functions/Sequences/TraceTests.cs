namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System.Threading.Tasks;

/// <summary><c>fn:trace</c> (Functions and Operators 1.0, 3.2.1): the value of the first argument, unchanged.</summary>
public class TraceTests
{
    [Test]
    [Arguments("()", "")]
    [Arguments("integer:7", "integer:7")]
    [Arguments("integer:1|string:b", "integer:1,string:b")]
    [Arguments("node:d1|node:d1/0", "node:d1,node:d1/0")]
    public async Task Trace_ReturnsItsFirstArgumentUnchanged(string value, string expected) =>
        await Assert.That(SequencesHarness.Run("trace", value, "string:label")).IsEqualTo(expected);

    [Test]
    [Arguments(1)]
    [Arguments(3)]
    public async Task Trace_IsRegisteredForTwoArgumentsOnly(int arity) =>
        await Assert.That(SequencesHarness.TryResolve("trace", arity, out _)).IsFalse();

    [Test]
    public async Task Trace_IsRegisteredForTwoArguments() =>
        await Assert.That(SequencesHarness.TryResolve("trace", 2, out _)).IsTrue();
}
