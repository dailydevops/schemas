namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Binding;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The catalog collects function groups through their registration files; the registry freezes on first use.</summary>
public class FunctionCatalogTests
{
    private static readonly XPathParameterType AnyItems = new(XPathItemType.Item, Occurrence.ZeroOrMore);

    private static FakeBindingFunction Fake(
        string local,
        int arity,
        XPathVersionMask versions = XPathVersionMask.All
    ) => new(local, arity, arity, Parameters(arity)) { VersionMask = versions };

    private static XPathParameterType[] Parameters(int arity)
    {
        var parameters = new XPathParameterType[arity];
        Array.Fill(parameters, AnyItems);
        return parameters;
    }

    // A registration file exposes one static method of this shape for the module initializer to stage.
    private static void RegisterStringGroup(FunctionCatalogBuilder builder)
    {
        builder.Add(Fake("upper", 1));
        builder.Add(Fake("lower", 1));
    }

    private static void RegisterNumericGroup(FunctionCatalogBuilder builder) => builder.Add(Fake("abs", 1));

    private static bool Found(FunctionRegistry registry, string local, int arity) =>
        registry.TryResolve(
            XPathStaticContext.Default,
            new XPathQName(FakeBindingFunction.Namespace, local),
            arity,
            XPathHostRequirements.None,
            out _
        );

    [Test]
    public async Task Stage_TwoGroups_BothAreCollected()
    {
        var catalog = new FunctionCatalog();
        catalog.Stage(RegisterStringGroup);
        catalog.Stage(RegisterNumericGroup);

        var registry = catalog.Registry;

        await Assert.That(Found(registry, "upper", 1)).IsTrue();
        await Assert.That(Found(registry, "lower", 1)).IsTrue();
        await Assert.That(Found(registry, "abs", 1)).IsTrue();
        await Assert.That(Found(registry, "other", 1)).IsFalse();
    }

    [Test]
    public async Task Stage_RegistrationOrder_DoesNotAffectLookup()
    {
        var forward = new FunctionCatalog();
        forward.Stage(RegisterStringGroup);
        forward.Stage(RegisterNumericGroup);
        var backward = new FunctionCatalog();
        backward.Stage(RegisterNumericGroup);
        backward.Stage(RegisterStringGroup);

        foreach (var name in new[] { "upper", "lower", "abs", "other" })
        {
            for (var arity = 0; arity <= 2; arity++)
            {
                await Assert
                    .That(Found(backward.Registry, name, arity))
                    .IsEqualTo(Found(forward.Registry, name, arity));
            }
        }
    }

    [Test]
    public async Task Registry_DuplicateNameAndArity_FailsNamingTheFunction()
    {
        var catalog = new FunctionCatalog();
        catalog.Stage(builder => builder.Add(Fake("dup", 1)));
        catalog.Stage(builder => builder.Add(Fake("dup", 1)));

        var failure = Assert.Throws<InvalidOperationException>(() => _ = catalog.Registry);

        await Assert.That(failure.Message).Contains("dup");
    }

    [Test]
    public async Task Registry_OverlappingArityRanges_Fail()
    {
        var catalog = new FunctionCatalog();
        catalog.Stage(builder =>
        {
            builder.Add(new FakeBindingFunction("range", 1, 2, Parameters(2)));
            builder.Add(new FakeBindingFunction("range", 2, 3, Parameters(3)));
        });

        await Assert.That(() => catalog.Registry).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Registry_VariadicOverlappingAFixedArity_Fails()
    {
        var catalog = new FunctionCatalog();
        catalog.Stage(builder =>
        {
            builder.Add(new FakeConcatFunction());
            builder.Add(new FakeBindingFunction("concat", 3, 3, Parameters(3)));
        });

        await Assert.That(() => catalog.Registry).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Registry_SameNameWithDisjointVersions_IsAllowed()
    {
        var catalog = new FunctionCatalog();
        catalog.Stage(builder =>
        {
            builder.Add(Fake("join", 1, XPathVersionMask.V20));
            builder.Add(Fake("join", 1, XPathVersionMask.V30 | XPathVersionMask.V31));
        });
        var v30Context = XPathStaticContext.Default.ToBuilder().WithVersion(XPathVersion.V30).Build();

        var found = catalog.Registry.TryResolve(
            v30Context,
            new XPathQName(FakeBindingFunction.Namespace, "join"),
            1,
            XPathHostRequirements.None,
            out var function
        );

        await Assert.That(found).IsTrue();
        await Assert.That(function!.Versions).IsEqualTo(XPathVersionMask.V30 | XPathVersionMask.V31);
    }

    [Test]
    public async Task Registry_SameNameAndArityInDifferentNamespaces_IsAllowed()
    {
        var catalog = new FunctionCatalog();
        catalog.Stage(builder =>
        {
            builder.Add(Fake("same", 1));
            builder.Add(new FakeBindingFunction("same", 1, 1, Parameters(1), ns: "urn:other"));
        });

        await Assert.That(Found(catalog.Registry, "same", 1)).IsTrue();
    }

    [Test]
    public async Task Registry_IsFrozenAfterFirstUse()
    {
        var catalog = new FunctionCatalog();
        catalog.Stage(RegisterStringGroup);
        var first = catalog.Registry;

        await Assert.That(() => catalog.Stage(RegisterNumericGroup)).Throws<InvalidOperationException>();
        await Assert.That(catalog.Registry).IsSameReferenceAs(first);
        await Assert.That(Found(catalog.Registry, "abs", 1)).IsFalse();
    }

    [Test]
    public async Task Default_IsOneSharedInstance() =>
        await Assert.That(FunctionCatalog.Default).IsSameReferenceAs(FunctionCatalog.Default);

    [Test]
    public async Task Stage_NullRegistration_Throws() =>
        await Assert.That(() => new FunctionCatalog().Stage(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Add_NullFunction_Throws()
    {
        var catalog = new FunctionCatalog();
        catalog.Stage(builder => builder.Add(null!));

        await Assert.That(() => catalog.Registry).Throws<ArgumentNullException>();
    }
}
