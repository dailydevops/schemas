namespace NetEvolve.Schemas.XPath.Tests.Unit.Context;

using System;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class ContextImplementationTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task StaticContext_Prefix_ResolvesDeclaredAndPredefinedPrefixes()
    {
        var context = XPathStaticContext.Default.ToBuilder().WithNamespace("p", "urn:p").Build();

        await Assert.That(context.TryResolveNamespace("p", out var uri)).IsTrue();
        await Assert.That(uri).IsEqualTo("urn:p");
        await Assert.That(context.TryResolveNamespace("xml", out var xml)).IsTrue();
        await Assert.That(xml).IsEqualTo("http://www.w3.org/XML/1998/namespace");
    }

    [Test]
    public async Task StaticContext_UndeclaredPrefix_IsNotResolved()
    {
        var found = XPathStaticContext.Default.TryResolveNamespace("nope", out var uri);

        await Assert.That(found).IsFalse();
        await Assert.That(uri).IsNull();
    }

    [Test]
    public async Task StaticContext_DefaultNamespaces_AreReturnedAsConfigured()
    {
        var context = XPathStaticContext.Default.ToBuilder().WithDefaultElementNamespace("urn:e").Build();

        await Assert.That(context.DefaultElementNamespace).IsEqualTo("urn:e");
        await Assert.That(context.DefaultFunctionNamespace).IsEqualTo("http://www.w3.org/2005/xpath-functions");
        await Assert.That(XPathStaticContext.Default.DefaultElementNamespace).IsNull();
    }

    [Test]
    public async Task Variable_DeclaredStatically_IsBoundDynamically()
    {
        var name = new XPathQName(string.Empty, "v");
        var context = XPathStaticContext.Default.ToBuilder().WithVariable(name, "xs:integer").Build();
        using var evaluation = new XPathEvaluationContext();
        evaluation.Variables.Set(name, XdmItem.FromInteger(7));

        await Assert.That(context.TryGetVariableType(name, out var type)).IsTrue();
        await Assert.That(type).IsEqualTo("xs:integer");
        var value = evaluation.Variables.Get(name);
        await Assert.That(value.Count).IsEqualTo(1);
        await Assert.That(value[0].TryGetInteger(out var number)).IsTrue();
        await Assert.That(number).IsEqualTo(7L);
    }

    [Test]
    public async Task Variable_UndeclaredStatically_IsNotKnown()
    {
        var known = XPathStaticContext.Default.TryGetVariableType(new XPathQName(string.Empty, "v"), out _);

        await Assert.That(known).IsFalse();
    }

    [Test]
    public async Task Variable_Unbound_RaisesXpdy0002()
    {
        using var evaluation = new XPathEvaluationContext();

        var failure = Assert.Throws<XPathException>(() => evaluation.Variables.Get(new XPathQName(string.Empty, "v")));

        await Assert.That(failure.Diagnostic.Code).IsEqualTo("XPDY0002");
    }

    [Test]
    public async Task CurrentDateTime_TwoReadsInOneEvaluation_AreEqual()
    {
        using var evaluation = new XPathEvaluationContext();
        var calls = 0;
        evaluation.Clock = () => Epoch.AddSeconds(calls++);

        var first = evaluation.CurrentDateTime;
        var second = evaluation.CurrentDateTime;

        await Assert.That(second).IsEqualTo(first);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task CurrentDateTime_NewEvaluation_MayDiffer()
    {
        using var evaluation = new XPathEvaluationContext();
        var calls = 0;
        evaluation.Clock = () => Epoch.AddSeconds(calls++);

        var first = evaluation.CurrentDateTime;
        evaluation.Reset();
        var second = evaluation.CurrentDateTime;

        await Assert.That(second).IsNotEqualTo(first);
    }

    [Test]
    public async Task CurrentDateTime_CarriesTheImplicitTimezone()
    {
        using var evaluation = new XPathEvaluationContext { ImplicitTimezoneMinutes = 90, Clock = static () => Epoch };

        var now = evaluation.CurrentDateTime;

        await Assert.That(now.Offset).IsEqualTo(TimeSpan.FromMinutes(90));
        await Assert.That(now.Hour).IsEqualTo(1);
        await Assert.That(now.Minute).IsEqualTo(30);
    }

    [Test]
    public async Task ImplicitTimezone_DefaultsToUtcAndIsReturnedAsConfigured()
    {
        using var evaluation = new XPathEvaluationContext();
        await Assert.That(evaluation.ImplicitTimezoneMinutes).IsEqualTo(0);

        evaluation.ImplicitTimezoneMinutes = -300;

        await Assert.That(evaluation.ImplicitTimezoneMinutes).IsEqualTo(-300);
        Assert.Throws<ArgumentOutOfRangeException>(() => evaluation.ImplicitTimezoneMinutes = 841);
        Assert.Throws<ArgumentOutOfRangeException>(() => evaluation.ImplicitTimezoneMinutes = -841);
    }

    [Test]
    public async Task BaseUri_IsReturnedAsConfigured()
    {
        var uri = new Uri("http://example.test/base/");

        var context = XPathStaticContext.Default.ToBuilder().WithBaseUri(uri).Build();

        await Assert.That(context.BaseUri).IsEqualTo(uri);
        await Assert.That(XPathStaticContext.Default.BaseUri).IsNull();
    }

    [Test]
    public async Task HostSlots_AreIsolatedPerEvaluationAndRoundTrip()
    {
        using var first = new XPathEvaluationContext();
        using var second = new XPathEvaluationContext();
        var keys = new FakeKeys();

        first.Host.CurrentItem = XdmItem.FromString("a");
        first.Host.CurrentGroup = XdmSequence.Create(XdmItem.FromInteger(1));
        first.Host.CurrentGroupingKey = XdmSequence.Create(XdmItem.FromInteger(2));
        first.Host.Keys = keys;

        await Assert.That(first.Host.Keys).IsSameReferenceAs(keys);
        await Assert.That(first.Host.CurrentGroup!.Value.Count).IsEqualTo(1);
        await Assert.That(second.Host.CurrentItem).IsNull();
        await Assert.That(second.Host.CurrentGroup).IsNull();
        await Assert.That(second.Host.CurrentGroupingKey).IsNull();
        await Assert.That(second.Host.Keys).IsNull();

        first.Reset();

        await Assert.That(first.Host.CurrentItem).IsNull();
        await Assert.That(first.Host.CurrentGroupingKey).IsNull();
        await Assert.That(first.Host.Keys).IsNull();
    }

    [Test]
    public async Task Focus_Enter_ReturnsPreviousAndRestoreBringsItBack()
    {
        using var evaluation = new XPathEvaluationContext();
        evaluation.SetContextItem(XdmItem.FromInteger(1), 1, 3);

        var previous = evaluation.EnterFocus(XdmItem.FromInteger(2), 2, 3);

        await Assert.That(evaluation.Focus.Position).IsEqualTo(2);
        await Assert.That(evaluation.Focus.Size).IsEqualTo(3);
        await Assert.That(evaluation.Focus.Item!.Value.TryGetInteger(out var inner)).IsTrue();
        await Assert.That(inner).IsEqualTo(2L);

        evaluation.RestoreFocus(previous);

        await Assert.That(evaluation.Focus.Position).IsEqualTo(1);
        await Assert.That(evaluation.Focus.Item!.Value.TryGetInteger(out var outer)).IsTrue();
        await Assert.That(outer).IsEqualTo(1L);
    }

    [Test]
    public async Task Focus_NoContextItem_IsEmpty()
    {
        using var evaluation = new XPathEvaluationContext();

        await Assert.That(evaluation.Focus.Item).IsNull();
        await Assert.That(evaluation.Focus.Position).IsEqualTo(0);
    }

    [Test]
    public async Task Focus_EnterAndRestore_AllocateZeroBytes()
    {
        using var evaluation = new XPathEvaluationContext();
        var item = XdmItem.FromInteger(5);
        evaluation.SetContextItem(item);

        AllocationAssert.NoAllocation(() =>
        {
            var previous = evaluation.EnterFocus(item, 2, 4);
            evaluation.RestoreFocus(previous);
        });

        await Assert.That(evaluation.Focus.Position).IsEqualTo(1);
    }

    private sealed class FakeKeys : IXPathKeyProvider
    {
        public bool TryLookup(
            in XPathQName name,
            in XdmSequence values,
            in XPathNode node,
            bool subtree,
            out XdmSequence nodes
        )
        {
            nodes = default;
            return false;
        }
    }
}
