namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

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

    [Test]
    public async Task Trace_SendsTheLabelAndTheValueToTheHostSinkWhenSet()
    {
        string? label = null;
        XdmSequence? traced = null;
        using var context = new XPathEvaluationContext();
        context.Host.Trace = (text, value) =>
        {
            label = text;
            traced = value;
        };
        var argument = XdmSequence.Create(XdmItem.FromString("v"));

        var result = SequencesHarness
            .Resolve("trace", 2)
            .Call(context, [argument, XdmSequence.Create(XdmItem.FromString("lbl"))]);
        context.Host.Clear();

        await Assert.That(label).IsEqualTo("lbl");
        await Assert.That(SequencesHarness.Show(traced!.Value)).IsEqualTo("string:v");
        await Assert.That(SequencesHarness.Show(result)).IsEqualTo("string:v");
        await Assert.That(context.Host.Trace).IsNull();
    }
}
