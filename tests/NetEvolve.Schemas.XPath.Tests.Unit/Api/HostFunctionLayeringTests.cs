namespace NetEvolve.Schemas.XPath.Tests.Unit.Api;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;

public class HostFunctionLayeringTests
{
    private static readonly XPathQName Count = new(ApiFixture.FunctionNamespace, "count");

    private static XPathStaticContext Layers(params XPathFunctionLibrary[] libraries)
    {
        var builder = ApiFixture.Builder();
        foreach (var library in libraries)
        {
            _ = builder.WithLibrary(library);
        }

        return builder.Build();
    }

    private static XPathFunctionLibrary Library(string name, params XPathHostFunction[] functions) =>
        new(name, functions);

    [Test]
    public async Task BuiltInFunction_WithoutAHostFunction_IsFound() =>
        await Assert.That(ApiFixture.Run("count((1, 2, 3))")).IsEqualTo("3");

    [Test]
    public async Task HostFunction_OverridesTheBuiltInFunctionOfTheSameNameAndArity()
    {
        var statics = Layers(Library("host", new ConstantFunction(Count, 1, 1, 100)));

        await Assert.That(ApiFixture.Run("count((1, 2, 3))", null, statics)).IsEqualTo("100");
        await Assert.That(ApiFixture.Run("fn:count((1, 2, 3))", null, statics)).IsEqualTo("100");
    }

    [Test]
    public async Task HostFunction_OfAnotherArity_LeavesTheBuiltInFunctionVisible()
    {
        var statics = Layers(Library("host", new ConstantFunction(Count, 2, 2, 100)));

        await Assert.That(ApiFixture.Run("count((1, 2, 3))", null, statics)).IsEqualTo("3");
        await Assert.That(ApiFixture.Run("count(1, 2)", null, statics)).IsEqualTo("100");
    }

    [Test]
    public async Task SecondHostLayer_OverridesTheFirstInTheDocumentedOrder()
    {
        var first = Library("first", new ConstantFunction(Count, 1, 1, 100));
        var second = Library("second", new ConstantFunction(Count, 1, 1, 200));

        await Assert.That(ApiFixture.Run("count(1)", null, Layers(first, second))).IsEqualTo("200");
        await Assert.That(ApiFixture.Run("count(1)", null, Layers(second, first))).IsEqualTo("100");
    }

    [Test]
    public async Task LaterEntryInsideOneLibrary_Wins()
    {
        var library = Library("both", new ConstantFunction(Count, 1, 1, 100), new ConstantFunction(Count, 1, 1, 200));

        await Assert.That(ApiFixture.Run("count(1)", null, Layers(library))).IsEqualTo("200");
    }

    [Test]
    public async Task WithFunction_AddsALayerOfItsOwn()
    {
        var statics = ApiFixture
            .Builder()
            .WithFunction(new ConstantFunction(Count, 1, 1, 100))
            .WithFunction(new ConstantFunction(Count, 1, 1, 300))
            .Build();

        await Assert.That(ApiFixture.Run("count(1)", null, statics)).IsEqualTo("300");
    }

    [Test]
    public async Task HostFunction_InItsOwnNamespace_IsCalledThroughItsPrefix()
    {
        var statics = ApiFixture
            .Builder()
            .WithNamespace("my", "urn:my")
            .WithFunction(new ConstantFunction(new XPathQName("urn:my", "answer"), 0, 0, 42))
            .Build();

        await Assert.That(ApiFixture.Run("my:answer() + 1", null, statics)).IsEqualTo("43");
    }

    [Test]
    public async Task HostFunction_OutsideItsVersionMask_IsNotVisible()
    {
        var statics = Layers(Library("v3", new ConstantFunction(Count, 1, 1, 100, XPathVersionMask.V30)));

        await Assert.That(ApiFixture.Run("count((1, 2, 3))", null, statics)).IsEqualTo("3");
    }

    [Test]
    public async Task HostFunction_NeedingASlot_IsVisibleAndTheSlotIsTheBusinessOfTheFunction()
    {
        var statics = Layers(
            Library(
                "slot",
                new ConstantFunction(Count, 1, 1, 100, XPathVersionMask.All, XPathHostRequirements.CurrentItem)
            )
        );

        await Assert.That(ApiFixture.Run("count(1)", null, statics)).IsEqualTo("100");
    }

    [Test]
    public async Task HostFunction_NeedingTheXsltFlag_IsNotVisibleThroughThePublicApi()
    {
        var statics = Layers(
            Library(
                "flag",
                new ConstantFunction(Count, 1, 1, 100, XPathVersionMask.All, XPathHostRequirements.Xslt20Host)
            )
        );

        await Assert.That(ApiFixture.Run("count((1, 2, 3))", null, statics)).IsEqualTo("3");
    }

    [Test]
    public async Task InternalCompile_WithTheXsltFlag_MakesTheFlaggedFunctionsVisible()
    {
        var statics = ApiFixture.Statics();

        var without = XPathExpression.TryCompile("generate-id(/)".AsSpan(), statics, XPathHostRequirements.None);
        var with = XPathExpression.TryCompile("generate-id(/)".AsSpan(), statics, XPathHostRequirements.Xslt20Host);

        await Assert.That(without.Diagnostic.Code).IsEqualTo("XPST0017");
        await Assert.That(with.Success).IsTrue();
    }
}
