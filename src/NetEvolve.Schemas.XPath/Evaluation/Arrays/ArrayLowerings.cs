namespace NetEvolve.Schemas.XPath.Evaluation.Arrays;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>The registration file of the lowerings of the array constructors (XPath 3.1, 3.11.2).</summary>
internal static class ArrayLowerings
{
    /// <summary>Registers the lowerings of this folder.</summary>
    /// <param name="registry">The registry.</param>
    /// <exception cref="System.InvalidOperationException">A node kind already has a lowering.</exception>
    public static void Register(LoweringRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register<SquareArrayConstructor>(static (node, context) => LowerSquare(node, context));
        registry.Register<CurlyArrayConstructor>(
            static (node, context) => new CurlyArrayNode(node.Content is null ? null : context.Lower(node.Content))
        );
    }

    private static SquareArrayNode LowerSquare(SquareArrayConstructor node, LoweringContext context)
    {
        var members = new ExecutableNode[node.Members.Count];
        for (var i = 0; i < members.Length; i++)
        {
            members[i] = context.Lower(node.Members[i]);
        }

        return new SquareArrayNode(members);
    }
}
