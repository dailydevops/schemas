namespace NetEvolve.Schemas.XPath.Casting.Core;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The lowerings of this folder: <c>cast as</c> and <c>castable as</c>.</summary>
/// <remarks>
/// The target type is bound while lowering, so an unknown type name, an unbound prefix, or a type that cannot be a target is a static error of the whole expression.
/// The namespaces of the static context are handed to a cast whose target resolves the prefix of a lexical form (<c>xs:QName</c>); the language version decides whether
/// the operand may be cast from its lexical form (see <see cref="NamespacesFor"/>).
/// </remarks>
internal static class CastingLowerings
{
    /// <summary>Registers the lowerings of this folder over the default casting table.</summary>
    /// <param name="registry">The registry.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="registry"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">A node kind already has a lowering.</exception>
    public static void Register(LoweringRegistry registry)
    {
        Argument.ThrowIfNull(registry);

        Register(registry, CastingCatalog.Default.Table);
    }

    /// <summary>Registers the lowerings of this folder over a casting table.</summary>
    /// <param name="registry">The registry.</param>
    /// <param name="table">The casting table.</param>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">A node kind already has a lowering.</exception>
    public static void Register(LoweringRegistry registry, CastingTable table)
    {
        Argument.ThrowIfNull(registry);
        Argument.ThrowIfNull(table);

        registry.Register<CastExpression>(
            (node, context) =>
            {
                var target = CastTargetResolver.Resolve(node.Type, context.StaticContext, table);
                return new CastNode(
                    context.Lower(node.Operand),
                    target,
                    node.Type.AllowsEmpty,
                    table,
                    node.Location,
                    NamespacesFor(node.Operand, context.StaticContext, table, target)
                );
            }
        );
        registry.Register<CastableExpression>(
            (node, context) =>
            {
                var target = CastTargetResolver.Resolve(node.Type, context.StaticContext, table);
                return new CastableNode(
                    context.Lower(node.Operand),
                    target,
                    node.Type.AllowsEmpty,
                    table,
                    NamespacesFor(node.Operand, context.StaticContext, table, target)
                );
            }
        );
    }

    /// <summary>Gets the namespaces for the cast of an operand to a target type.</summary>
    /// <param name="operand">The operand of the cast.</param>
    /// <param name="statics">The static context.</param>
    /// <param name="table">The casting table.</param>
    /// <param name="target">The target type.</param>
    /// <returns>
    /// <see langword="null"/> when the cast does not depend on the namespaces, and when XPath 2.0 does not let the operand be cast from its lexical form:
    /// there only a string literal is cast to <c>xs:QName</c> (Functions and Operators, 17.1.1), so any other string or untyped operand is XPTY0004.
    /// XPath 3.0 and 3.1 cast any string or untyped value against the namespaces of the static context. Otherwise the namespaces of the static context.
    /// </returns>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    internal static INamespaceContext? NamespacesFor(
        Expression operand,
        XPathStaticContext statics,
        CastingTable table,
        XdmTypeId target
    )
    {
        Argument.ThrowIfNull(operand);
        Argument.ThrowIfNull(statics);
        Argument.ThrowIfNull(table);

        if (!table.IsNamespaceSensitive(target))
        {
            return null;
        }

        return statics.Version == XPathVersion.V20 && !IsStringLiteral(operand)
            ? null
            : new StaticNamespaceContext(statics);
    }

    // A string literal, also when parenthesized: the parentheses do not change the value.
    private static bool IsStringLiteral(Expression operand)
    {
        var current = operand;
        while (current is ParenthesizedExpression { Inner: { } inner })
        {
            current = inner;
        }

        return current is StringLiteral;
    }
}
