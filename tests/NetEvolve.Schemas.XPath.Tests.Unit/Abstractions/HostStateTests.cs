namespace NetEvolve.Schemas.XPath.Tests.Unit.Abstractions;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Tests.Unit.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class HostStateTests
{
    private static readonly XPathQName Name = new("urn:my", "state");

    private static XPathExpression Compile(XPathHostFunction function) =>
        XPathExpression.Compile(
            "my:state()",
            ApiFixture.Builder().WithNamespace("my", "urn:my").WithFunction(function).Build()
        );

    [Test]
    public async Task HostFunction_ReadsRunState_ThroughHostState()
    {
        var expression = Compile(new StateFunction(Name, 0));

        using var first = new XPathEvaluationContext();
        first.Host.HostState = 7L;
        using var second = new XPathEvaluationContext();
        second.Host.HostState = 9L;

        await Assert.That(ApiFixture.Render(expression.Evaluate(first))).IsEqualTo("7");
        await Assert.That(ApiFixture.Render(expression.Evaluate(second))).IsEqualTo("9");
    }

    [Test]
    public async Task HostFunction_Registered_HoldsOnlyImmutableData()
    {
        var expression = Compile(new StateFunction(Name, 100));

        using var first = new XPathEvaluationContext();
        first.Host.HostState = 1L;
        using var second = new XPathEvaluationContext();
        second.Host.HostState = 2L;

        // The registered function adds its immutable constant; the run state is the only thing that differs per context.
        await Assert.That(ApiFixture.Render(expression.Evaluate(first))).IsEqualTo("101");
        await Assert.That(ApiFixture.Render(expression.Evaluate(second))).IsEqualTo("102");
        await Assert.That(first.Host.HostState).IsEqualTo(1L);
    }

    [Test]
    public async Task HostFunction_SignatureFirst_BodyResolvedAtInvoke()
    {
        var expression = Compile(new StateFunction(Name, 0));

        using var context = new XPathEvaluationContext();
        context.Host.HostState = (Func<long>)(() => 5L);
        await Assert.That(ApiFixture.Render(expression.Evaluate(context))).IsEqualTo("5");

        context.Host.HostState = (Func<long>)(() => 6L);
        await Assert.That(ApiFixture.Render(expression.Evaluate(context))).IsEqualTo("6");
    }

    [Test]
    public async Task HostState_IsEmptyByDefault_AndClearedByClear()
    {
        using var context = new XPathEvaluationContext();
        await Assert.That(context.Host.HostState).IsNull();

        context.Host.HostState = "run";
        await Assert.That(context.Host.HostState).IsEqualTo("run");

        context.Host.Clear();
        await Assert.That(context.Host.HostState).IsNull();
    }
}
