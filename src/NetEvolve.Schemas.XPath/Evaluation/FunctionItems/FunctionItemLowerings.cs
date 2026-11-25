namespace NetEvolve.Schemas.XPath.Evaluation.FunctionItems;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;

/// <summary>
/// The registration file of the lowerings of this folder: inline function expressions, named function references, and dynamic function
/// calls (XPath 3.0, 3.1.6, 3.1.7, and 3.2.2); the static call with a placeholder is lowered by the engine through <see cref="LowerPartial"/>.
/// </summary>
/// <remarks>
/// An inline function captures the variables that its body references and that it does not declare, by value, when the function item
/// is created. Limitation: the static context of the body is the one of the expression, so a function item that is passed to another
/// compilation keeps its own bindings.
/// </remarks>
internal static class FunctionItemLowerings
{
    /// <summary>Registers the lowerings of this folder.</summary>
    /// <param name="registry">The registry.</param>
    /// <param name="activeHost">The host slots and flags that the host provides; a named reference resolves against the functions that they leave visible.</param>
    /// <exception cref="System.InvalidOperationException">A node kind already has a lowering.</exception>
    public static void Register(LoweringRegistry registry, XPathHostRequirements activeHost)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register<InlineFunctionExpression>(static (node, context) => LowerInline(node, context));
        registry.Register<NamedFunctionReference>(
            (node, context) =>
                new NamedReferenceNode(Resolve(node.Name, node.Arity, node.Location, context, activeHost), node.Arity)
        );
        registry.Register<DynamicFunctionCall>(static (node, context) => LowerDynamic(node, context));
    }

    /// <summary>Resolves the name and arity of a call or a reference against the host functions of the static context and the built-in catalog.</summary>
    /// <param name="name">The function name; an unprefixed name is in the default function namespace.</param>
    /// <param name="arity">The number of arguments.</param>
    /// <param name="location">The location reported for XPST0017, usually the whole call.</param>
    /// <param name="context">The lowering context.</param>
    /// <param name="activeHost">The host slots and flags that the host provides.</param>
    /// <returns>The function; <c>fn:function-lookup</c> is bound to the static context of the compilation.</returns>
    /// <exception cref="XPathException">The prefix is undeclared (XPST0081) or no function matches (XPST0017).</exception>
    public static XPathFunction Resolve(
        QualifiedName name,
        int arity,
        SourceSpan location,
        LoweringContext context,
        XPathHostRequirements activeHost
    )
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(context);

        var statics = context.StaticContext;
        var namespaceName = statics.DefaultFunctionNamespace;
        if (name.Prefix is not null && !statics.TryResolveNamespace(name.Prefix, out namespaceName))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPST0081, name.Location.Offset, name.Location.Length);
        }

        return FunctionCatalog.Default.Registry.TryResolve(
            statics,
            new XPathQName(namespaceName!, name.LocalName),
            arity,
            activeHost,
            out var function
        )
            ? FunctionLookupFunction.Bind(function!, statics, activeHost)
            : throw XPathErrors.Exception(XPathErrorCodes.XPST0017, location.Offset, location.Length);
    }

    /// <summary>Determines whether a list of arguments holds a placeholder, which makes the call a partial function application.</summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns><see langword="true"/> when an argument is <c>?</c>.</returns>
    public static bool HasPlaceholder(IReadOnlyList<Expression> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        for (var i = 0; i < arguments.Count; i++)
        {
            if (arguments[i] is ArgumentPlaceholder)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Lowers a static call with a placeholder: the partial application of the named function.</summary>
    /// <param name="node">The call.</param>
    /// <param name="function">The function the call resolved to.</param>
    /// <param name="context">The lowering context.</param>
    /// <returns>The executable node.</returns>
    public static ExecutableNode LowerPartial(FunctionCall node, XPathFunction function, LoweringContext context)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(context);

        return new PartialApplicationNode(
            new NamedReferenceNode(function, node.Arguments.Count),
            LowerPartialArguments(node.Arguments, context),
            node.Location
        );
    }

    private static ExecutableNode LowerDynamic(DynamicFunctionCall node, LoweringContext context)
    {
        var function = context.Lower(node.Function);
        if (HasPlaceholder(node.Arguments))
        {
            return new PartialApplicationNode(function, LowerPartialArguments(node.Arguments, context), node.Location);
        }

        var arguments = new ExecutableNode[node.Arguments.Count];
        for (var i = 0; i < arguments.Length; i++)
        {
            arguments[i] = context.Lower(node.Arguments[i]);
        }

        return new DynamicCallNode(function, arguments, node.Location);
    }

    private static ExecutableNode?[] LowerPartialArguments(IReadOnlyList<Expression> arguments, LoweringContext context)
    {
        var lowered = new ExecutableNode?[arguments.Count];
        for (var i = 0; i < lowered.Length; i++)
        {
            lowered[i] = arguments[i] is ArgumentPlaceholder ? null : context.Lower(arguments[i]);
        }

        return lowered;
    }

    private static FunctionItemNode LowerInline(InlineFunctionExpression node, LoweringContext context)
    {
        var count = node.Parameters.Count;
        var types = new SequenceTypeInfo[count];
        for (var i = 0; i < count; i++)
        {
            var declared = node.Parameters[i].Type;
            types[i] = declared is null
                ? SequenceTypeInfo.AnyItems
                : SequenceTypeInfo.Bind(declared, context.StaticContext);
        }

        var result = node.ReturnType is null
            ? SequenceTypeInfo.AnyItems
            : SequenceTypeInfo.Bind(node.ReturnType, context.StaticContext);

        // The frame opens before the parameters are declared, so a parameter is local to the function and is never captured.
        using var frame = context.BeginCapture();
        var names = new XPathQName[count];
        var scopes = new List<LoweringContext.VariableScope>(count);
        try
        {
            for (var i = 0; i < count; i++)
            {
                var parameter = node.Parameters[i];
                scopes.Add(context.DeclareVariable(parameter.Name));
                names[i] = context.ResolveVariable(parameter.Name);
                if (System.Array.IndexOf(names, names[i], 0, i) >= 0)
                {
                    throw XPathErrors.Exception(
                        XPathErrorCodes.XQST0039,
                        parameter.Location.Offset,
                        parameter.Location.Length
                    );
                }
            }

            var body = context.Lower(node.Body);
            var free = new XPathQName[frame.FreeNames.Count];
            frame.FreeNames.CopyTo(free);
            return new FunctionItemNode(new FunctionSignature(types, result), names, body, free);
        }
        finally
        {
            for (var i = 0; i < scopes.Count; i++)
            {
                scopes[i].Dispose();
            }
        }
    }
}
