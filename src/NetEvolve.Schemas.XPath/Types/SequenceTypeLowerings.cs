namespace NetEvolve.Schemas.XPath.Types;

using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>The registration file of this folder: the lowerings of <c>instance of</c> and <c>treat as</c>.</summary>
/// <remarks>
/// The sequence type is bound while lowering, so an unknown type name or an unbound prefix is a static error of the whole expression.
/// </remarks>
internal static class SequenceTypeLowerings
{
    /// <summary>Registers the lowerings of this folder.</summary>
    /// <param name="registry">The registry.</param>
    /// <exception cref="System.InvalidOperationException">A node kind already has a lowering.</exception>
    public static void Register(LoweringRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register<InstanceOfExpression>(
            static (node, context) =>
                new InstanceOfNode(
                    context.Lower(node.Operand),
                    SequenceTypeMatcher.Create(node.Type, context.StaticContext),
                    UntypedAnnotationSource.Instance
                )
        );
        registry.Register<TreatExpression>(
            static (node, context) =>
                new TreatNode(
                    context.Lower(node.Operand),
                    SequenceTypeMatcher.Create(node.Type, context.StaticContext),
                    UntypedAnnotationSource.Instance,
                    node.Location
                )
        );
    }
}
