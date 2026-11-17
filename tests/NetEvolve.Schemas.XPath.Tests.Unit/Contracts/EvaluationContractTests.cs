namespace NetEvolve.Schemas.XPath.Tests.Unit.Contracts;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class EvaluationContractTests
{
    private static XPathStaticContext StaticContext() =>
        XPathStaticContext
            .Default.ToBuilder()
            .WithNamespace("fake", FakeFunction.Namespace)
            .WithFunction(new FakeFunction("string-length", 1, 1))
            .Build();

    [Test]
    [Arguments("fake:string-length(.)", 5L)]
    [Arguments("fake:string-length(@a)", 1L)]
    public async Task Expression_OverFakeNodeSource_CallsFakeFunction(string expression, long expected)
    {
        Argument.ThrowIfNull(expression);
        var source = new FakeNodeSource();
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(new XPathNode(source.Element)));

        var result = FakeEngine.Compile(expression, StaticContext()).Evaluate(context);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(expected);
    }

    [Test]
    public async Task Layering_LaterLibraryWins_ForSameNameAndArity()
    {
        var first = new XPathFunctionLibrary("first", [new FakeFunction("f", 1, 1, "first")]);
        var second = new XPathFunctionLibrary("second", [new FakeFunction("f", 1, 1, "second")]);
        var name = new XPathQName(FakeFunction.Namespace, "f");

        var context = XPathStaticContext.Default.ToBuilder().WithLibrary(first).WithLibrary(second).Build();
        var reversed = XPathStaticContext.Default.ToBuilder().WithLibrary(second).WithLibrary(first).Build();

        await Assert.That(Invoke(context, name)).IsEqualTo("second");
        await Assert.That(Invoke(reversed, name)).IsEqualTo("first");
        await Assert.That(first.Name).IsEqualTo("first");
        await Assert.That(first.Functions.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Layering_FunctionOfLaterLayerWithOtherArity_DoesNotHideEarlierOne()
    {
        var context = XPathStaticContext
            .Default.ToBuilder()
            .WithFunction(new FakeFunction("f", 1, 1, "one"))
            .WithFunction(new FakeFunction("f", 2, 2, "two"))
            .Build();
        var name = new XPathQName(FakeFunction.Namespace, "f");

        await Assert.That(Invoke(context, name, 1)).IsEqualTo("one");
        await Assert.That(Invoke(context, name, 2)).IsEqualTo("two");
        await Assert.That(context.TryResolveFunction(name, 3, out _)).IsFalse();
        await Assert.That(context.TryResolveFunction(new XPathQName("urn:other", "f"), 1, out _)).IsFalse();
    }

    [Test]
    public async Task Layering_FunctionOutsideTheStaticVersion_IsNotResolved()
    {
        var name = new XPathQName(FakeFunction.Namespace, "v3");
        var context = XPathStaticContext
            .Default.ToBuilder()
            .WithFunction(new FakeFunction("v3", 0, 0, "x") { VersionMask = XPathVersionMask.V30 })
            .Build();
        var v3 = context.ToBuilder().WithVersion(XPathVersion.V30).Build();

        await Assert.That(context.TryResolveFunction(name, 0, out _)).IsFalse();
        await Assert.That(v3.TryResolveFunction(name, 0, out _)).IsTrue();
    }

    [Test]
    public async Task HostFunction_DefaultsToAllVersionsAndNoHostRequirements()
    {
        var function = new FakeFunction("d", 0, 0, "x");
        var plain = new PlainFunction();

        await Assert.That(plain.Versions).IsEqualTo(XPathVersionMask.All);
        await Assert.That(plain.HostRequirements).IsEqualTo(XPathHostRequirements.None);
        await Assert.That(function.MinArity).IsEqualTo(0);
        await Assert.That(function.MaxArity).IsEqualTo(0);
        await Assert.That(function.Name.LocalName).IsEqualTo("d");
    }

    [Test]
    public async Task HostFunction_InvertedArityRange_IsRejected() =>
        await Assert.That(() => new FakeFunction("bad", 2, 1)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task StaticContext_Builder_ProducesImmutableCopies()
    {
        var builder = XPathStaticContext
            .Default.ToBuilder()
            .WithVersion(XPathVersion.V31)
            .WithXPath10Compatibility(true)
            .WithBaseUri(new Uri("urn:base"))
            .WithDefaultElementNamespace("urn:default")
            .WithVariable(new XPathQName("", "v"), "xs:integer");
        var first = builder.Build();
        _ = builder.WithBaseUri(new Uri("urn:other"));
        var second = first.ToBuilder().Build();

        await Assert.That(first.Version).IsEqualTo(XPathVersion.V31);
        await Assert.That(first.XPath10Compatibility).IsTrue();
        await Assert.That(first.BaseUri?.OriginalString).IsEqualTo("urn:base");
        await Assert.That(first.DefaultElementNamespace).IsEqualTo("urn:default");
        await Assert.That(first.TryGetVariableType(new XPathQName("", "v"), out var type)).IsTrue();
        await Assert.That(type).IsEqualTo("xs:integer");
        await Assert.That(first.TryGetVariableType(new XPathQName("", "w"), out _)).IsFalse();
        await Assert.That(second.BaseUri?.OriginalString).IsEqualTo("urn:base");
        await Assert.That(builder.Build().BaseUri?.OriginalString).IsEqualTo("urn:other");
    }

    [Test]
    public async Task StaticContext_Default_UsesXPath20AndTheStandardFunctionNamespace()
    {
        var context = XPathStaticContext.Default;

        await Assert.That(context.Version).IsEqualTo(XPathVersion.V20);
        await Assert.That(context.XPath10Compatibility).IsFalse();
        await Assert.That(context.BaseUri).IsNull();
        await Assert.That(context.DefaultElementNamespace).IsNull();
        await Assert.That(context.DefaultFunctionNamespace).IsEqualTo("http://www.w3.org/2005/xpath-functions");
        await Assert.That(context.TryResolveNamespace("xml", out var xml)).IsTrue();
        await Assert.That(xml).IsEqualTo("http://www.w3.org/XML/1998/namespace");
        await Assert.That(context.TryResolveNamespace("fake", out _)).IsFalse();
    }

    [Test]
    public async Task StaticContext_Namespace_ResolvesByPrefix()
    {
        var context = StaticContext();

        await Assert.That(context.TryResolveNamespace("fake", out var uri)).IsTrue();
        await Assert.That(uri).IsEqualTo(FakeFunction.Namespace);
    }

    [Test]
    public async Task EvaluationContext_FocusVariablesAndReset_RoundTrip()
    {
        var name = new XPathQName("", "v");
        using var context = new XPathEvaluationContext();
        await Assert.That(context.ContextItem).IsNull();

        context.SetContextItem(XdmItem.FromInteger(7), 2, 3);
        context.Variables.Set(name, XdmItem.FromString("s"));
        context.Host.CurrentItem = XdmItem.FromBoolean(true);

        await Assert.That(context.ContextItem!.Value.TryGetInteger(out var seven)).IsTrue();
        await Assert.That(seven).IsEqualTo(7L);
        await Assert.That(context.Position).IsEqualTo(2);
        await Assert.That(context.Size).IsEqualTo(3);
        await Assert.That(context.Variables.TryGet(name, out var value)).IsTrue();
        await Assert.That(value.Count).IsEqualTo(1);

        context.Variables.Set(name, XdmSequence.Empty);
        await Assert.That(context.Variables.TryGet(name, out var empty)).IsTrue();
        await Assert.That(empty.IsEmpty).IsTrue();
        await Assert.That(context.Variables.TryGet(new XPathQName("", "none"), out _)).IsFalse();

        context.ResourceResolver = new XPathResourceResolverAdapter(DenyAllResourceResolver.Instance);
        context.Reset();

        await Assert.That(context.ContextItem).IsNull();
        await Assert.That(context.Host.CurrentItem).IsNull();
        await Assert.That(context.Variables.TryGet(name, out _)).IsFalse();
        await Assert.That(context.ResourceResolver).IsNotNull();
    }

    [Test]
    public async Task EvaluationContext_InvalidFocus_IsRejected()
    {
        using var context = new XPathEvaluationContext();

        await Assert
            .That(() => context.SetContextItem(XdmItem.FromInteger(1), 0, 1))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => context.SetContextItem(XdmItem.FromInteger(1), 2, 1))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task EvaluationContext_AfterDispose_RejectsUse()
    {
        var context = new XPathEvaluationContext();
        context.Dispose();
        context.Dispose();

        await Assert.That(() => context.Reset()).Throws<ObjectDisposedException>();
        await Assert.That(() => context.SetContextItem(XdmItem.FromInteger(1))).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task FunctionCall_CarriesContextArgumentsAndDiagnostic()
    {
        using var context = new XPathEvaluationContext();

        var (sameContext, count, resultEmpty, code) = Probe(context);

        await Assert.That(sameContext).IsTrue();
        await Assert.That(count).IsEqualTo(1);
        await Assert.That(resultEmpty).IsTrue();
        await Assert.That(code).IsEqualTo("XPDY0002");
    }

    private static (bool SameContext, int Count, bool ResultEmpty, string Code) Probe(XPathEvaluationContext context)
    {
        XdmSequence[] arguments = [XdmSequence.Empty];
        var call = new XPathFunctionCall(context, arguments)
        {
            Diagnostic = XPathDiagnostic.Create(XPathErrorCodes.XPDY0002),
        };

        return (
            ReferenceEquals(call.Context, context),
            call.Arguments.Length,
            call.Result.IsEmpty,
            call.Diagnostic.Code
        );
    }

    [Test]
    public async Task Results_CarryStatusValueAndDiagnostic()
    {
        var failure = new XPathResult(
            XPathEvaluationStatus.Error,
            XdmSequence.Empty,
            XPathDiagnostic.Create(XPathErrorCodes.XPST0003)
        );
        var ok = new XPathResult(XPathEvaluationStatus.Ok, XdmSequence.Create(XdmItem.FromInteger(1)), default);
        var failed = new XPathCompileResult(null, XPathDiagnostic.Create(XPathErrorCodes.XPST0003));

        await Assert.That(failure.Status).IsEqualTo(XPathEvaluationStatus.Error);
        await Assert.That(failure.Diagnostic.Code).IsEqualTo("XPST0003");
        await Assert.That(ok.Value.Count).IsEqualTo(1);
        await Assert.That(failed.Success).IsFalse();
        await Assert.That(failed.Expression).IsNull();
    }

    private static string? Invoke(XPathStaticContext context, XPathQName name, int arity = 1)
    {
        if (!context.TryResolveFunction(name, arity, out var function))
        {
            return null;
        }

        using var evaluation = new XPathEvaluationContext();
        var arguments = new XdmSequence[arity];
        var call = new XPathFunctionCall(evaluation, arguments);
        _ = function!.Invoke(ref call);
        return call.Result[0].TryGetString(out var text) ? text : null;
    }

    private sealed class PlainFunction() : XPathHostFunction(new XPathQName("urn:p", "p"), 0, 0)
    {
        public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation) => XPathEvaluationStatus.Ok;
    }
}
