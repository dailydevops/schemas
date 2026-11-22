namespace NetEvolve.Schemas.XPath.Evaluation.Flow;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;
using NetEvolve.Schemas.Xml.Datatypes.Floating;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The registration file of this folder: the lowerings of the literals, the variable reference, the parenthesized expression, the comma
/// operator, the range expression, <see langword="and"/> and <see langword="or"/>, the conditional expression, and the <see langword="for"/>, <c>some</c>, and
/// <c>every</c> expressions.
/// </summary>
/// <remarks>
/// <para>
/// A literal is parsed while the expression is lowered, by the datatype layer of the library, so every target framework reads the same
/// value; an integer is an <c>xs:integer</c> of any width, a form with a point is an <c>xs:decimal</c>, and a form with an exponent is
/// an <c>xs:double</c>. Limitation: a literal that the datatype layer cannot take (more digits than its cap) is a static error, FOAR0002.
/// </para>
/// <para>
/// An expression with several range variables is lowered as a chain: <c>for $a in A, $b in B return R</c> becomes
/// <c>for $a in A return (for $b in B return R)</c>, which keeps the scope of each variable exact (a source sees the variables before
/// it, not its own) and makes every variable one level of the tree, so the recursion depth limit bounds the number of variables.
/// </para>
/// </remarks>
internal static class FlowLowerings
{
    /// <summary>Registers the lowerings of this folder.</summary>
    /// <param name="registry">The registry.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="registry"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.InvalidOperationException">A node kind already has a lowering.</exception>
    public static void Register(LoweringRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        registry.Register<IntegerLiteral>(static (node, _) => Constant(ParseInteger(node)));
        registry.Register<DecimalLiteral>(static (node, _) => Constant(ParseDecimal(node)));
        registry.Register<DoubleLiteral>(static (node, _) => Constant(ParseDouble(node)));
        registry.Register<StringLiteral>(static (node, _) => Constant(XdmItem.FromString(node.Value)));
        registry.Register<ParenthesizedExpression>(
            static (node, context) =>
                node.Inner is null ? new ConstantNode(XdmSequence.Empty) : context.Lower(node.Inner)
        );
        registry.Register<VariableReference>(
            static (node, context) => new VariableNode(context.ResolveVariable(node.Name))
        );
        registry.Register<SequenceExpression>(static (node, context) => LowerSequence(node, context));
        registry.Register<RangeExpression>(
            static (node, context) => new RangeNode(context.Lower(node.From), context.Lower(node.To), node.Location)
        );
        registry.Register<LogicalExpression>(
            static (node, context) =>
                new LogicalNode(node.Operator, context.Lower(node.Left), context.Lower(node.Right))
        );
        registry.Register<IfExpression>(
            static (node, context) =>
                new IfNode(context.Lower(node.Condition), context.Lower(node.Then), context.Lower(node.Else))
        );
        registry.Register<ForExpression>(static (node, context) => LowerFor(node, context));
        registry.Register<QuantifiedExpression>(static (node, context) => LowerQuantified(node, context));
    }

    private static ConstantNode Constant(in XdmItem item) => new(XdmSequence.Create(item));

    private static XdmItem ParseInteger(IntegerLiteral node)
    {
        var parsed = XsdDecimal.Parse(node.Lexeme.AsSpan());
        if (parsed.Success)
        {
            var integer = DecimalArithmetic.ToBigInteger(parsed.Value);
            if (integer.Success)
            {
                return XdmItem.FromBigInteger(integer.Value);
            }
        }

        throw Overflow(node);
    }

    private static XdmItem ParseDecimal(DecimalLiteral node)
    {
        var parsed = XsdDecimal.Parse(node.Lexeme.AsSpan());
        return parsed.Success ? XdmItem.FromDecimal(parsed.Value) : throw Overflow(node);
    }

    private static XdmItem ParseDouble(DoubleLiteral node)
    {
        var parsed = XsdFloatingPoint.ParseDouble(node.Lexeme.AsSpan(), XsdVersion.V10);
        return parsed.Success ? XdmItem.FromDouble(parsed.Value) : throw Overflow(node);
    }

    private static XPathException Overflow(Expression node) =>
        XPathErrors.Exception(XPathErrorCodes.FOAR0002, node.Location.Offset, node.Location.Length);

    private static SequenceNode LowerSequence(SequenceExpression node, LoweringContext context)
    {
        var items = new ExecutableNode[node.Items.Count];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = context.Lower(node.Items[i]);
        }

        return new SequenceNode(items, node.Location);
    }

    private static ForNode LowerFor(ForExpression node, LoweringContext context)
    {
        var binding = node.Bindings[0];
        var source = context.Lower(binding.Source);
        using var scope = context.DeclareVariable(binding.Name);
        var name = context.ResolveVariable(binding.Name);
        var body =
            node.Bindings.Count == 1
                ? context.Lower(node.Return)
                : context.Lower(new ForExpression(node.Location, Tail(node.Bindings), node.Return));
        return new ForNode(name, source, body, node.Location);
    }

    private static QuantifiedNode LowerQuantified(QuantifiedExpression node, LoweringContext context)
    {
        var binding = node.Bindings[0];
        var source = context.Lower(binding.Source);
        using var scope = context.DeclareVariable(binding.Name);
        var name = context.ResolveVariable(binding.Name);
        var body =
            node.Bindings.Count == 1
                ? context.Lower(node.Satisfies)
                : context.Lower(
                    new QuantifiedExpression(node.Location, node.Quantifier, Tail(node.Bindings), node.Satisfies)
                );
        return new QuantifiedNode(node.Quantifier, name, source, body);
    }

    // The bindings after the first one; the chain of nodes is built from them one level at a time.
    private static List<VariableBinding> Tail(IReadOnlyList<VariableBinding> bindings)
    {
        var tail = new List<VariableBinding>(bindings.Count - 1);
        for (var i = 1; i < bindings.Count; i++)
        {
            tail.Add(bindings[i]);
        }

        return tail;
    }
}
