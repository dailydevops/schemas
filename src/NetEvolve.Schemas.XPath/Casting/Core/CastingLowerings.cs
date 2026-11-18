namespace NetEvolve.Schemas.XPath.Casting.Core;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The lowerings of this folder: <c>cast as</c> and <c>castable as</c>.</summary>
/// <remarks>The target type is bound while lowering, so an unknown type name, an unbound prefix, or a type that cannot be a target is a static error of the whole expression.</remarks>
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
                new CastNode(
                    context.Lower(node.Operand),
                    CastTargetResolver.Resolve(node.Type, context.StaticContext, table),
                    node.Type.AllowsEmpty,
                    table,
                    node.Location
                )
        );
        registry.Register<CastableExpression>(
            (node, context) =>
                new CastableNode(
                    context.Lower(node.Operand),
                    CastTargetResolver.Resolve(node.Type, context.StaticContext, table),
                    node.Type.AllowsEmpty,
                    table
                )
        );
    }
}
