namespace NetEvolve.Schemas.XPath.Api;

using System;
using System.Collections.Concurrent;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Compatibility;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Compilation.Optimization;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Arrays;
using NetEvolve.Schemas.XPath.Evaluation.Arrow;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Evaluation.Flow30;
using NetEvolve.Schemas.XPath.Evaluation.FunctionItems;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Evaluation.Temporal;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;

/// <summary>
/// The composition root of the package: it fills the operator tables, registers the lowerings of every folder, binds a function call
/// over the host functions of the static context and the built-in catalog, and compiles an expression. The registry and the tables
/// are filled once and read only afterwards, so concurrent compilations share them.
/// </summary>
/// <remarks>
/// Limitations: only XPath 2.0 is enabled, so a static context of another version is rejected with <see cref="VersionNotEnabled"/>
/// (design #123, AP10). The prefixes <c>fn</c> and <c>xs</c> are not predeclared by <see cref="XPathStaticContext.Default"/> (#876); a caller
/// declares them. The public API compiles under every host slot and no host flag; the internal overload takes both.
/// </remarks>
internal static class XPathEngine
{
    /// <summary>The engine code of a static context whose language version is not enabled.</summary>
    internal static readonly XPathErrorCode VersionNotEnabled = new(XPathLimitErrors.EngineNamespace, "XP0001");

    /// <summary>
    /// The host slots that the public API treats as provided: a function that needs a slot is visible, and finds the slot unset at run
    /// time (XPDY0002) when the caller did not set it. A host flag such as <see cref="XPathHostRequirements.Xslt20Host"/> is not a slot.
    /// </summary>
    internal const XPathHostRequirements AllSlots =
        XPathHostRequirements.CurrentItem
        | XPathHostRequirements.CurrentGroup
        | XPathHostRequirements.RegexGroups
        | XPathHostRequirements.DecimalFormats
        | XPathHostRequirements.Keys;

    private static readonly ArithmeticOperatorTable Arithmetic = CreateArithmetic();
    internal static readonly ComparisonOperatorTable Comparison = CreateComparison();
    private static readonly LoweringRegistry DefaultRegistry = CreateRegistry(AllSlots);

    // One registry per host flag combination instead of one per compilation: building it allocates a dictionary and a delegate per lowering.
    private static readonly ConcurrentDictionary<XPathHostRequirements, LoweringRegistry> HostRegistries = new();

    /// <summary>Compiles an expression without throwing for static errors.</summary>
    /// <param name="text">The expression text.</param>
    /// <param name="source">The expression text as a string, kept by the compiled expression.</param>
    /// <param name="context">The static context, <see langword="null"/> for <see cref="XPathStaticContext.Default"/>.</param>
    /// <param name="activeHost">The host slots and flags that the host provides.</param>
    /// <param name="optimizations">The rewrites of the optimization stage; every rewrite by default, a test switches some off.</param>
    /// <returns>The compiled expression, or the diagnostic.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    internal static XPathCompileResult TryCompile(
        ReadOnlySpan<char> text,
        string source,
        XPathStaticContext? context,
        XPathHostRequirements activeHost,
        XPathOptimizations optimizations = XPathOptimizations.All
    )
    {
        ArgumentNullException.ThrowIfNull(source);

        context ??= XPathStaticContext.Default;
        if (context.Version != XPathVersion.V20)
        {
            return new XPathCompileResult(
                null,
                new XPathDiagnostic(
                    VersionNotEnabled,
                    XPathErrorKind.Engine,
                    "The language version " + context.Version + " is not enabled; only XPath 2.0 is.",
                    -1,
                    0
                )
            );
        }

        if (!XPathParser.TryParse(text, out var syntax, out var diagnostic))
        {
            return new XPathCompileResult(null, diagnostic);
        }

        var registry =
            activeHost == AllSlots
                ? DefaultRegistry
                : HostRegistries.GetOrAdd(activeHost, static host => CreateRegistry(host));
        return ExpressionCompiler.TryCompile(
            syntax!,
            source,
            context,
            registry,
            out var compiled,
            out diagnostic,
            null,
            optimizations
        )
            ? new XPathCompileResult(new XPathExpression(compiled!), default)
            : new XPathCompileResult(null, diagnostic);
    }

    /// <summary>Creates the registry of the lowerings for the host slots that the host provides.</summary>
    /// <param name="activeHost">The host slots and flags that the host provides.</param>
    /// <returns>The registry.</returns>
    internal static LoweringRegistry CreateRegistry(XPathHostRequirements activeHost)
    {
        var registry = new LoweringRegistry();
        FlowLowerings.Register(registry);
        Flow30Lowerings.Register(registry);
        ArrowLowerings.Register(registry);
        PathLowerings.Register(registry);
        SequenceTypeLowerings.Register(registry);
        CastingLowerings.Register(registry);
        ArithmeticLowerings.Register(registry, Arithmetic);
        ComparisonLowerings.Register(registry, Comparison);
        FunctionItemLowerings.Register(registry, activeHost);
        ArrayLowerings.Register(registry);
        registry.Register<MapConstructor>(
            static (node, context) => Functions.Maps.MapConstructorNode.Lower(node, context)
        );
        registry.Register<FunctionCall>((node, context) => LowerCall(node, context, activeHost));
        return registry;
    }

    private static ArithmeticOperatorTable CreateArithmetic()
    {
        var table = new ArithmeticOperatorTable();
        NumericOperators.Register(table);
        TemporalOperators.Register(table);
        return table;
    }

    private static ComparisonOperatorTable CreateComparison()
    {
        var table = new ComparisonOperatorTable();
        ComparisonOperators.Register(table);
        TemporalOperators.Register(table);
        return table;
    }

    // The host functions of the static context come first, the later layer winning, then the built-in catalog (design #123, #168).
    private static ExecutableNode LowerCall(
        FunctionCall node,
        LoweringContext context,
        XPathHostRequirements activeHost
    )
    {
        var statics = context.StaticContext;
        var arity = node.Arguments.Count;
        var function = FunctionItemLowerings.Resolve(node.Name, arity, node.Location, context, activeHost);

        // A placeholder makes the call a partial function application: the result is a function item (XPath 3.0, 3.1.8).
        if (FunctionItemLowerings.HasPlaceholder(node.Arguments))
        {
            return FunctionItemLowerings.LowerPartial(node, function, context);
        }

        var arguments = new ExecutableNode[arity];
        for (var i = 0; i < arity; i++)
        {
            arguments[i] = context.Lower(node.Arguments[i]);
        }

        // A constructor function resolves the prefix of a lexical QName against the static context, as cast as does (#644).
        if (function is ConstructorFunction constructor && arity == 1)
        {
            function = constructor.BindTo(node.Arguments[0], statics);
        }

        // In XPath 1.0 compatibility mode the node of the mode converts the arguments first (XPath 2.0, 3.1.5).
        return statics.XPath10Compatibility
            ? new CompatibilityCallNode(function, arguments)
            : new CallNode(function, arguments);
    }
}
