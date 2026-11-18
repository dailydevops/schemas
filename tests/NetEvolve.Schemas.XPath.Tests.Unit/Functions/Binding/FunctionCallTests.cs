namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Binding;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The one call path: arity check, argument conversion, invocation, and the status of the function.</summary>
public class FunctionCallTests
{
    private static readonly XPathParameterType One = new(XPathItemType.Double, Occurrence.ExactlyOne);

    [Test]
    public async Task Call_ErrorStatus_ThrowsAnXPathExceptionCarryingTheDiagnostic()
    {
        var function = new FakeBindingFunction("fail", 1, 1, [One], failWith: XPathErrorCodes.FOER0000);
        using var context = new XPathEvaluationContext();

        var failure = Assert.Throws<XPathException>(() =>
            function.Call(context, [XdmSequence.Create(XdmItem.FromDouble(1))])
        );

        await Assert.That(failure.Diagnostic.Code).IsEqualTo("FOER0000");
    }

    [Test]
    public async Task Call_ConvertedArguments_ReachTheFunction()
    {
        var function = new FakeBindingFunction("echo", 1, 1, [One]);
        using var context = new XPathEvaluationContext();

        var result = function.Call(context, [XdmSequence.Create(XdmItem.FromInteger(4))]);

        await Assert.That(result).IsEqualTo(XdmSequence.Create(XdmItem.FromDouble(4)));
    }

    [Test]
    public async Task Call_NoArguments_InvokesTheFunction()
    {
        var expected = XdmSequence.Create(XdmItem.FromBoolean(true));
        var function = new FakeBindingFunction("now", 0, 0, [], fixedResult: expected);
        using var context = new XPathEvaluationContext();

        await Assert.That(function.Call(context, [])).IsEqualTo(expected);
    }

    [Test]
    public async Task Call_TooFewArguments_Throws()
    {
        var function = new FakeBindingFunction("echo", 1, 1, [One]);
        using var context = new XPathEvaluationContext();

        await Assert.That(() => function.Call(context, [])).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Call_TooManyArguments_Throws()
    {
        var function = new FakeBindingFunction("echo", 1, 1, [One]);
        using var context = new XPathEvaluationContext();

        await Assert
            .That(() => function.Call(context, [XdmSequence.Empty, XdmSequence.Empty]))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Call_NullContext_Throws()
    {
        var function = new FakeBindingFunction("echo", 1, 1, [One]);

        await Assert.That(() => function.Call(null!, [XdmSequence.Empty])).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_ParameterCountOutsideTheArityRange_Throws()
    {
        await Assert.That(() => new FakeBindingFunction("bad", 2, 2, [One])).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new FakeBindingFunction("bad", 1, 1, [One, One])).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new FakeBindingFunction("bad", 1, 1, null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ParameterType_UndefinedItemTypeOrOccurrence_Throws()
    {
        await Assert
            .That(() => new XPathParameterType((XPathItemType)999, Occurrence.ExactlyOne))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => new XPathParameterType(XPathItemType.Item, (Occurrence)999))
            .Throws<ArgumentOutOfRangeException>();
    }

    // Allocation: the binding layer of a one-argument call with a singleton argument

    [Test]
    public async Task Call_OneArgumentSingleton_PromotedValue_AllocatesNothingInTheBindingLayer()
    {
        var function = BindingHarness.Resolve(
            BindingRoute.BuiltIn,
            new FakeBindingFunction("echo", 1, 1, [One], fixedResult: XdmSequence.Create(XdmItem.FromBoolean(true)))
        );
        using var context = new XPathEvaluationContext();
        XdmSequence[] arguments = [XdmSequence.Create(XdmItem.FromInteger(3))];
        XdmSequence result = default;

        AllocationAssert.NoAllocation(() => result = function.Call(context, arguments));

        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Call_OneArgumentSingleton_UnconvertedValue_AllocatesNothingInTheBindingLayer()
    {
        var function = BindingHarness.Resolve(
            BindingRoute.Host,
            new FakeBindingFunction(
                "echo",
                1,
                1,
                [new XPathParameterType(XPathItemType.String, Occurrence.ExactlyOne)],
                fixedResult: XdmSequence.Create(XdmItem.FromBoolean(true))
            )
        );
        using var context = new XPathEvaluationContext();
        XdmSequence[] arguments = [XdmSequence.Create(XdmItem.FromString("x"))];
        XdmSequence result = default;

        AllocationAssert.NoAllocation(() => result = function.Call(context, arguments));

        await Assert.That(result.Count).IsEqualTo(1);
    }
}
