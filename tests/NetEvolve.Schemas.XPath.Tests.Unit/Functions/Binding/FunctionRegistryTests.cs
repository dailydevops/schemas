namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Binding;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class FunctionRegistryTests
{
    private static readonly XPathParameterType AnyItems = new(XPathItemType.Item, Occurrence.ZeroOrMore);

    private static XPathStaticContext Context(XPathVersion version) =>
        XPathStaticContext.Default.ToBuilder().WithVersion(version).Build();

    private static FunctionRegistry BuiltIn(params XPathFunction[] functions)
    {
        var catalog = new FunctionCatalog();
        catalog.Stage(builder =>
        {
            foreach (var function in functions)
            {
                builder.Add(function);
            }
        });
        return catalog.Registry;
    }

    private static XPathQName Name(string local) => new(FakeBindingFunction.Namespace, local);

    // Lookup by name and arity

    [Test]
    public async Task Lookup_SameNameWithTwoArities_ResolvesBoth()
    {
        var one = new FakeBindingFunction("f", 1, 1, [AnyItems]);
        var two = new FakeBindingFunction("f", 2, 2, [AnyItems, AnyItems]);
        var registry = BuiltIn(one, two);

        var foundOne = registry.TryResolve(
            XPathStaticContext.Default,
            Name("f"),
            1,
            XPathHostRequirements.None,
            out var a
        );
        var foundTwo = registry.TryResolve(
            XPathStaticContext.Default,
            Name("f"),
            2,
            XPathHostRequirements.None,
            out var b
        );

        await Assert.That(foundOne).IsTrue();
        await Assert.That(a).IsSameReferenceAs(one);
        await Assert.That(foundTwo).IsTrue();
        await Assert.That(b).IsSameReferenceAs(two);
    }

    [Test]
    public async Task Lookup_UnknownArity_IsNotFound()
    {
        var registry = BuiltIn(new FakeBindingFunction("f", 1, 1, [AnyItems]));

        var found = registry.TryResolve(
            XPathStaticContext.Default,
            Name("f"),
            3,
            XPathHostRequirements.None,
            out var function
        );

        await Assert.That(found).IsFalse();
        await Assert.That(function).IsNull();
    }

    [Test]
    public async Task Lookup_UnknownName_IsNotFound()
    {
        var registry = BuiltIn(new FakeBindingFunction("f", 1, 1, [AnyItems]));

        var found = registry.TryResolve(XPathStaticContext.Default, Name("g"), 1, XPathHostRequirements.None, out _);

        await Assert.That(found).IsFalse();
    }

    [Test]
    public async Task Lookup_OptionalArguments_ResolvesEveryArityInTheRange()
    {
        var registry = BuiltIn(new FakeBindingFunction("sub", 2, 3, [AnyItems, AnyItems, AnyItems]));

        await Assert.That(Found(registry, "sub", 1)).IsFalse();
        await Assert.That(Found(registry, "sub", 2)).IsTrue();
        await Assert.That(Found(registry, "sub", 3)).IsTrue();
        await Assert.That(Found(registry, "sub", 4)).IsFalse();
    }

    [Test]
    public async Task Lookup_Variadic_ResolvesFromTheMinimumArityUp()
    {
        var registry = BuiltIn(new FakeConcatFunction());

        await Assert.That(Found(registry, "concat", 1)).IsFalse();
        await Assert.That(Found(registry, "concat", 2)).IsTrue();
        await Assert.That(Found(registry, "concat", 3)).IsTrue();
        await Assert.That(Found(registry, "concat", 100)).IsTrue();
    }

    [Test]
    public async Task Call_Variadic_ConvertsEveryArgumentWithTheLastParameterType()
    {
        var registry = BuiltIn(new FakeConcatFunction());
        _ = registry.TryResolve(
            XPathStaticContext.Default,
            Name("concat"),
            5,
            XPathHostRequirements.None,
            out var concat
        );
        using var context = new XPathEvaluationContext();
        XdmSequence[] arguments =
        [
            XdmSequence.Create(XdmItem.FromString("a")),
            XdmSequence.Create(XdmItem.FromInteger(1)),
            XdmSequence.Empty,
            XdmSequence.Create(XdmItem.FromUntypedAtomic("u")),
            XdmSequence.Create(XdmItem.FromString("z")),
        ];

        var result = concat!.Call(context, arguments);

        await Assert.That(result[0]).IsEqualTo(XdmItem.FromString("a1uz"));
    }

    [Test]
    public async Task Call_Variadic_RepeatedParameterTypeStillChecksTheOccurrence()
    {
        var registry = BuiltIn(new FakeConcatFunction());
        _ = registry.TryResolve(
            XPathStaticContext.Default,
            Name("concat"),
            3,
            XPathHostRequirements.None,
            out var concat
        );
        using var context = new XPathEvaluationContext();
        XdmSequence[] arguments =
        [
            XdmSequence.Empty,
            XdmSequence.Empty,
            XdmSequence.Create([XdmItem.FromString("a"), XdmItem.FromString("b")]),
        ];

        var failure = Assert.Throws<XPathException>(() => concat!.Call(context, arguments));

        await Assert.That(failure.Diagnostic.Code).IsEqualTo("XPTY0004");
    }

    // Availability flags

    [Test]
    [Arguments(BindingRoute.BuiltIn)]
    [Arguments(BindingRoute.Host)]
    [Arguments(BindingRoute.User)]
    public async Task Version_FunctionFlaggedForXPath30_IsNotFoundInAnXPath20Context(BindingRoute route)
    {
        var function = new FakeBindingFunction("v30", 1, 1, [AnyItems]) { VersionMask = XPathVersionMask.V30 };

        var visible = BindingHarness.Resolve(route, function, Context(XPathVersion.V30));
        var hidden = Throws(() => BindingHarness.Resolve(route, function, Context(XPathVersion.V20)));

        await Assert.That(visible).IsSameReferenceAs(function);
        await Assert.That(hidden).IsTrue();
    }

    [Test]
    [Arguments(BindingRoute.BuiltIn)]
    [Arguments(BindingRoute.Host)]
    [Arguments(BindingRoute.User)]
    public async Task Version_FunctionFlaggedForXPath20Only_IsNotFoundInAnXPath31Context(BindingRoute route)
    {
        var function = new FakeBindingFunction("v20", 1, 1, [AnyItems]) { VersionMask = XPathVersionMask.V20 };

        await Assert
            .That(BindingHarness.Resolve(route, function, Context(XPathVersion.V20)))
            .IsSameReferenceAs(function);
        await Assert.That(Throws(() => BindingHarness.Resolve(route, function, Context(XPathVersion.V31)))).IsTrue();
    }

    [Test]
    [Arguments(BindingRoute.BuiltIn)]
    [Arguments(BindingRoute.Host)]
    [Arguments(BindingRoute.User)]
    public async Task Host_FunctionFlaggedForAHost_IsFoundOnlyWhenThatHostIsActive(BindingRoute route)
    {
        var function = new FakeBindingFunction("current", 1, 1, [AnyItems])
        {
            Requires = XPathHostRequirements.CurrentItem,
        };

        var active = BindingHarness.Resolve(route, function, null, XPathHostRequirements.CurrentItem);
        var other = Throws(() => BindingHarness.Resolve(route, function, null, XPathHostRequirements.Keys));
        var none = Throws(() => BindingHarness.Resolve(route, function));

        await Assert.That(active).IsSameReferenceAs(function);
        await Assert.That(other).IsTrue();
        await Assert.That(none).IsTrue();
    }

    [Test]
    public async Task Host_FunctionNeedingTwoSlots_NeedsBothActive()
    {
        var function = new FakeBindingFunction("both", 1, 1, [AnyItems])
        {
            Requires = XPathHostRequirements.CurrentItem | XPathHostRequirements.Keys,
        };
        var registry = BuiltIn(function);

        await Assert.That(Found(registry, "both", 1, XPathHostRequirements.CurrentItem)).IsFalse();
        await Assert
            .That(Found(registry, "both", 1, XPathHostRequirements.CurrentItem | XPathHostRequirements.Keys))
            .IsTrue();
        await Assert
            .That(
                Found(
                    registry,
                    "both",
                    1,
                    XPathHostRequirements.CurrentItem
                        | XPathHostRequirements.Keys
                        | XPathHostRequirements.DecimalFormats
                )
            )
            .IsTrue();
    }

    // function-available

    [Test]
    [Arguments(BindingRoute.BuiltIn)]
    [Arguments(BindingRoute.Host)]
    [Arguments(BindingRoute.User)]
    public async Task IsAvailable_VersionFlag_IsFalseInAnXPath20ContextAndTrueInAnXPath30Context(BindingRoute route)
    {
        var function = new FakeBindingFunction("v30", 1, 1, [AnyItems]) { VersionMask = XPathVersionMask.V30 };
        var (registry, v20) = BindingHarness.Register(route, function, Context(XPathVersion.V20));
        var (_, v30) = BindingHarness.Register(route, function, Context(XPathVersion.V30));

        await Assert.That(registry.IsAvailable(v20, function.Name, 1, XPathHostRequirements.None)).IsFalse();
        await Assert.That(registry.TryResolve(v20, function.Name, 1, XPathHostRequirements.None, out _)).IsFalse();
        await Assert.That(registry.IsAvailable(v30, function.Name, 1, XPathHostRequirements.None)).IsTrue();
        await Assert.That(registry.TryResolve(v30, function.Name, 1, XPathHostRequirements.None, out _)).IsTrue();
    }

    [Test]
    [Arguments(BindingRoute.BuiltIn)]
    [Arguments(BindingRoute.Host)]
    [Arguments(BindingRoute.User)]
    public async Task IsAvailable_HostFlag_IsFalseWithoutTheSlotAndTrueWithIt(BindingRoute route)
    {
        var function = new FakeBindingFunction("keyed", 1, 1, [AnyItems]) { Requires = XPathHostRequirements.Keys };
        var (registry, context) = BindingHarness.Register(route, function);

        await Assert.That(registry.IsAvailable(context, function.Name, 1, XPathHostRequirements.None)).IsFalse();
        await Assert.That(registry.IsAvailable(context, function.Name, 1, XPathHostRequirements.Keys)).IsTrue();
        await Assert.That(registry.TryResolve(context, function.Name, 1, XPathHostRequirements.None, out _)).IsFalse();
        await Assert.That(registry.TryResolve(context, function.Name, 1, XPathHostRequirements.Keys, out _)).IsTrue();
    }

    [Test]
    [Arguments(BindingRoute.BuiltIn)]
    [Arguments(BindingRoute.Host)]
    [Arguments(BindingRoute.User)]
    public async Task IsAvailable_UnknownArityAndUnknownName_AreFalse(BindingRoute route)
    {
        var function = new FakeBindingFunction("known", 1, 1, [AnyItems]);
        var (registry, context) = BindingHarness.Register(route, function);

        await Assert.That(registry.IsAvailable(context, function.Name, 1, XPathHostRequirements.None)).IsTrue();
        await Assert.That(registry.IsAvailable(context, function.Name, 2, XPathHostRequirements.None)).IsFalse();
        await Assert.That(registry.IsAvailable(context, Name("unknown"), 1, XPathHostRequirements.None)).IsFalse();
    }

    // The query behind the host function policy of #150 (P-X2): what does the catalog itself define?

    [Test]
    public async Task IsDefinedByCatalog_DependsOnNameArityAndVersionOnly()
    {
        var registry = BuiltIn(
            new FakeBindingFunction("v30", 1, 1, [AnyItems]) { VersionMask = XPathVersionMask.V30 },
            new FakeBindingFunction("keyed", 1, 1, [AnyItems]) { Requires = XPathHostRequirements.Keys }
        );

        await Assert.That(registry.IsDefinedByCatalog(XPathVersion.V20, Name("v30"), 1)).IsFalse();
        await Assert.That(registry.IsDefinedByCatalog(XPathVersion.V30, Name("v30"), 1)).IsTrue();
        await Assert.That(registry.IsDefinedByCatalog(XPathVersion.V30, Name("v30"), 2)).IsFalse();
        await Assert.That(registry.IsDefinedByCatalog(XPathVersion.V20, Name("keyed"), 1)).IsTrue();
        await Assert.That(registry.IsDefinedByCatalog(XPathVersion.V20, Name("other"), 1)).IsFalse();
    }

    [Test]
    public async Task IsDefinedByCatalog_IgnoresHostAndUserFunctions()
    {
        var user = new FakeBindingFunction("user", 1, 1, [AnyItems]);

        await Assert
            .That(BuiltIn().WithUserFunctions([user]).IsDefinedByCatalog(XPathVersion.V20, user.Name, 1))
            .IsFalse();
    }

    [Test]
    public async Task IsDefinedByCatalog_InvalidArguments_Throw()
    {
        await Assert
            .That(() => BuiltIn().IsDefinedByCatalog(XPathVersion.V20, Name("f"), -1))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => BuiltIn().IsDefinedByCatalog((XPathVersion)7, Name("f"), 1))
            .Throws<ArgumentOutOfRangeException>();
    }

    // Layering: one lookup over host libraries, the user layer, and the built-in catalog

    [Test]
    public async Task Layering_HostLibrary_OverridesTheBuiltInFunctionOfTheSameNameAndArity()
    {
        var builtIn = new FakeBindingFunction("f", 1, 1, [AnyItems]);
        var host = new FakeBindingFunction("f", 1, 1, [AnyItems]);
        var context = XPathStaticContext.Default.ToBuilder().WithFunction(host).Build();

        _ = BuiltIn(builtIn).TryResolve(context, Name("f"), 1, XPathHostRequirements.None, out var found);

        await Assert.That(found).IsSameReferenceAs(host);
    }

    [Test]
    public async Task Layering_UserFunction_OverridesTheBuiltInFunctionOfTheSameNameAndArity()
    {
        var builtIn = new FakeBindingFunction("f", 1, 1, [AnyItems]);
        var user = new FakeBindingFunction("f", 1, 1, [AnyItems]);

        _ = BuiltIn(builtIn)
            .WithUserFunctions([user])
            .TryResolve(XPathStaticContext.Default, Name("f"), 1, XPathHostRequirements.None, out var found);

        await Assert.That(found).IsSameReferenceAs(user);
    }

    [Test]
    public async Task Layering_HostLibrary_OverridesTheUserFunctionOfTheSameNameAndArity()
    {
        var user = new FakeBindingFunction("f", 1, 1, [AnyItems]);
        var host = new FakeBindingFunction("f", 1, 1, [AnyItems]);
        var context = XPathStaticContext.Default.ToBuilder().WithFunction(host).Build();

        _ = BuiltIn()
            .WithUserFunctions([user])
            .TryResolve(context, Name("f"), 1, XPathHostRequirements.None, out var found);

        await Assert.That(found).IsSameReferenceAs(host);
    }

    [Test]
    public async Task Layering_LaterUserFunction_WinsInsideTheUserLayerAndOverEarlierLayers()
    {
        var first = new FakeBindingFunction("f", 1, 1, [AnyItems]);
        var second = new FakeBindingFunction("f", 1, 1, [AnyItems]);
        var third = new FakeBindingFunction("f", 1, 1, [AnyItems]);

        var registry = BuiltIn().WithUserFunctions([first, second]).WithUserFunctions([third]);
        _ = registry.TryResolve(XPathStaticContext.Default, Name("f"), 1, XPathHostRequirements.None, out var found);
        _ = BuiltIn()
            .WithUserFunctions([first, second])
            .TryResolve(XPathStaticContext.Default, Name("f"), 1, XPathHostRequirements.None, out var inside);

        await Assert.That(found).IsSameReferenceAs(third);
        await Assert.That(inside).IsSameReferenceAs(second);
    }

    [Test]
    public async Task Layering_WithUserFunctions_DoesNotChangeTheRegistryItWasCalledOn()
    {
        var registry = BuiltIn();

        _ = registry.WithUserFunctions([new FakeBindingFunction("f", 1, 1, [AnyItems])]);

        await Assert.That(Found(registry, "f", 1)).IsFalse();
    }

    [Test]
    public async Task Layering_HostLibraryFunction_HiddenByTheHostFlag_FallsBackToTheCatalog()
    {
        var builtIn = new FakeBindingFunction("f", 1, 1, [AnyItems]);
        var host = new FakeBindingFunction("f", 1, 1, [AnyItems]) { Requires = XPathHostRequirements.Keys };
        var context = XPathStaticContext.Default.ToBuilder().WithFunction(host).Build();

        _ = BuiltIn(builtIn).TryResolve(context, Name("f"), 1, XPathHostRequirements.None, out var without);
        _ = BuiltIn(builtIn).TryResolve(context, Name("f"), 1, XPathHostRequirements.Keys, out var with);

        await Assert.That(without).IsSameReferenceAs(builtIn);
        await Assert.That(with).IsSameReferenceAs(host);
    }

    // The contract host function has no declared parameter types: arguments reach it unchanged (limitation)

    [Test]
    public async Task PlainHostFunction_WithoutSignature_ReceivesTheArgumentsUnchanged()
    {
        var plain = new FakePlainHostFunction("plain", 1);
        var context = XPathStaticContext.Default.ToBuilder().WithFunction(plain).Build();
        _ = BuiltIn().TryResolve(context, Name("plain"), 1, XPathHostRequirements.None, out var function);
        using var evaluation = new XPathEvaluationContext();
        var nodeAndNumber = XdmSequence.Create([XdmItem.FromUntypedAtomic("1"), XdmItem.FromInteger(2)]);

        var result = function!.Call(evaluation, [nodeAndNumber]);

        await Assert.That(function).IsTypeOf<HostFunctionAdapter>();
        await Assert.That(result).IsEqualTo(nodeAndNumber);
    }

    // Guards

    [Test]
    public async Task TryResolve_NullContext_Throws() =>
        await Assert
            .That(() => BuiltIn().TryResolve(null!, Name("f"), 1, XPathHostRequirements.None, out _))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task TryResolve_NegativeArity_Throws() =>
        await Assert
            .That(() =>
                BuiltIn().TryResolve(XPathStaticContext.Default, Name("f"), -1, XPathHostRequirements.None, out _)
            )
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task IsAvailable_NullContext_Throws() =>
        await Assert
            .That(() => BuiltIn().IsAvailable(null!, Name("f"), 1, XPathHostRequirements.None))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task WithUserFunctions_NullList_Throws() =>
        await Assert.That(() => BuiltIn().WithUserFunctions(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task WithUserFunctions_NullElement_Throws() =>
        await Assert.That(() => BuiltIn().WithUserFunctions([null!])).Throws<ArgumentNullException>();

    [Test]
    public async Task Create_NullElement_Throws() =>
        await Assert.That(() => FunctionRegistry.Create([null!])).Throws<ArgumentNullException>();

    private static bool Found(
        FunctionRegistry registry,
        string local,
        int arity,
        XPathHostRequirements host = XPathHostRequirements.None
    ) => registry.TryResolve(XPathStaticContext.Default, Name(local), arity, host, out _);

    private static bool Throws(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }
}
