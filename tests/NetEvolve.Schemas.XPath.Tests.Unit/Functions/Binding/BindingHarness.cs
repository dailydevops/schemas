namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Binding;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Runs one fake function through each of the three registrations: built-in catalog, host library, and user layer.</summary>
internal static class BindingHarness
{
    internal readonly record struct Outcome(XdmSequence Result, string Code);

    /// <summary>Registers <paramref name="function"/> through a route.</summary>
    internal static (FunctionRegistry Registry, XPathStaticContext Context) Register(
        BindingRoute route,
        XPathFunction function,
        XPathStaticContext? context = null
    )
    {
        var staticContext = context ?? XPathStaticContext.Default;
        var registry = new FunctionCatalog().Registry;
        switch (route)
        {
            case BindingRoute.BuiltIn:
                var catalog = new FunctionCatalog();
                catalog.Stage(builder => builder.Add(function));
                registry = catalog.Registry;
                break;
            case BindingRoute.Host:
                staticContext = staticContext.ToBuilder().WithFunction(function).Build();
                break;
            default:
                registry = registry.WithUserFunctions([function]);
                break;
        }

        return (registry, staticContext);
    }

    /// <summary>Registers <paramref name="function"/> through a route and resolves it again at its minimum arity.</summary>
    internal static XPathFunction Resolve(
        BindingRoute route,
        XPathFunction function,
        XPathStaticContext? context = null,
        XPathHostRequirements activeHost = XPathHostRequirements.None
    )
    {
        var (registry, staticContext) = Register(route, function, context);
        var found = registry.TryResolve(staticContext, function.Name, function.MinArity, activeHost, out var resolved);
        return found ? resolved! : throw new InvalidOperationException($"{route} registration was not found.");
    }

    /// <summary>Calls a one-argument echo function with the given parameter type through one route.</summary>
    internal static Outcome Run(BindingRoute route, XPathParameterType type, XdmSequence argument)
    {
        var function = Resolve(route, new FakeBindingFunction("echo", 1, 1, [type]));
        using var context = new XPathEvaluationContext();
        try
        {
            return new Outcome(function.Call(context, [argument]), string.Empty);
        }
        catch (XPathException failure)
        {
            return new Outcome(default, failure.Diagnostic.Code);
        }
    }

    /// <summary>Asserts that all three routes give the expected result.</summary>
    internal static async Task AssertConverts(XPathParameterType type, XdmSequence argument, XdmSequence expected)
    {
        var first = Run(BindingRoute.BuiltIn, type, argument);
        await Assert.That(first.Code).IsEqualTo(string.Empty);
        await Assert.That(first.Result).IsEqualTo(expected);
        await Assert.That(Run(BindingRoute.Host, type, argument)).IsEqualTo(first);
        await Assert.That(Run(BindingRoute.User, type, argument)).IsEqualTo(first);
    }

    /// <summary>Asserts that all three routes raise the same error code.</summary>
    internal static async Task AssertFails(XPathParameterType type, XdmSequence argument, string code)
    {
        var first = Run(BindingRoute.BuiltIn, type, argument);
        await Assert.That(first.Code).IsEqualTo(code);
        await Assert.That(Run(BindingRoute.Host, type, argument)).IsEqualTo(first);
        await Assert.That(Run(BindingRoute.User, type, argument)).IsEqualTo(first);
    }
}
