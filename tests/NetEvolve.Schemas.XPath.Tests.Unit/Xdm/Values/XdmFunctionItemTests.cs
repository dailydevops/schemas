namespace NetEvolve.Schemas.XPath.Tests.Unit.Xdm.Values;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The function item of the data model: its kind, its accessors, and its identity.</summary>
public class XdmFunctionItemTests
{
    [Test]
    public async Task FromFunction_RoundTripsWithItsOwnKind(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var function = new ProbeFunction(new XPathQName("urn:f", "f"), 2);
        var item = XdmItem.FromFunction(function);

        await Assert.That(item.Kind).IsEqualTo(XdmItemKind.Function);
        await Assert.That(item.IsNode).IsFalse();
        await Assert.That(item.TryGetFunction(out var read)).IsTrue();
        await Assert.That(ReferenceEquals(read, function)).IsTrue();
        await Assert.That(read!.Arity).IsEqualTo(2);
        await Assert.That(read.Name).IsEqualTo(new XPathQName("urn:f", "f"));
        await Assert.That(read.Signature.Arity).IsEqualTo(2);
        await Assert.That(XdmItem.FromInteger(1).TryGetFunction(out var none)).IsFalse();
        await Assert.That(none).IsNull();
    }

    [Test]
    public async Task AFunctionItem_IsEqualOnlyToTheSameFunction(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var function = new ProbeFunction(null, 0);
        var same = XdmItem.FromFunction(function);
        var again = XdmItem.FromFunction(function);
        var other = XdmItem.FromFunction(new ProbeFunction(null, 0));

        await Assert.That(same == again).IsTrue();
        await Assert.That(same.GetHashCode()).IsEqualTo(again.GetHashCode());
        await Assert.That(same == other).IsFalse();
        await Assert.That(same.Equals((object)again)).IsTrue();
        await Assert.That(function.Name).IsNull();
    }

    [Test]
    public async Task FromFunction_RejectsNull(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _ = await Assert.That(() => XdmItem.FromFunction(null!)).Throws<ArgumentNullException>();
    }

    private sealed class ProbeFunction(XPathQName? name, int arity)
        : XdmFunction(name, new FunctionSignature(Parameters(arity), SequenceTypeInfo.AnyItems))
    {
        public override XdmSequence Invoke(XPathEvaluationContext context, ReadOnlySpan<XdmSequence> arguments) =>
            XdmSequence.Empty;

        private static SequenceTypeInfo[] Parameters(int arity)
        {
            var parameters = new SequenceTypeInfo[arity];
            Array.Fill(parameters, SequenceTypeInfo.AnyItems);
            return parameters;
        }
    }
}
