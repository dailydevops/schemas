namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Fake lowerings standing in for the concrete ones of later packages.</summary>
internal static class FakeLowerings
{
    public static LoweringRegistry Registry()
    {
        var registry = new LoweringRegistry();
        registry.Register<IntegerLiteral>(static (n, _) => new LiteralNode(n.Lexeme));
        registry.Register<VariableReference>(static (n, c) => new VariableNode(c.ResolveVariable(n.Name)));
        registry.Register<SequenceExpression>(
            static (n, c) =>
            {
                var items = new ExecutableNode[n.Items.Count];
                for (var i = 0; i < items.Length; i++)
                {
                    items[i] = c.Lower(n.Items[i]);
                }

                return new SequenceNode(items);
            }
        );
        registry.Register<FunctionCall>(
            static (n, c) =>
            {
                var function = c.ResolveFunction(n.Name, n.Arguments.Count, n.Location);
                var args = new ExecutableNode[n.Arguments.Count];
                for (var i = 0; i < args.Length; i++)
                {
                    args[i] = c.Lower(n.Arguments[i]);
                }

                return new CallNode(function, args);
            }
        );
        return registry;
    }

    internal sealed class LiteralNode(string lexeme) : ExecutableNode
    {
        private readonly string _lexeme = lexeme;

        public override XdmSequence Evaluate(XPathEvaluationContext context) =>
            XdmSequence.Create(XdmItem.FromString(_lexeme));
    }

    internal sealed class VariableNode(XPathQName name) : ExecutableNode
    {
        private readonly XPathQName _name = name;

        public override XdmSequence Evaluate(XPathEvaluationContext context) => context.Variables.Get(_name);
    }

    internal sealed class SequenceNode(IReadOnlyList<ExecutableNode> items) : ExecutableNode
    {
        public IReadOnlyList<ExecutableNode> Items { get; } = items;

        public override XdmSequence Evaluate(XPathEvaluationContext context) => XdmSequence.Empty;
    }

    internal sealed class CallNode(XPathHostFunction function, IReadOnlyList<ExecutableNode> arguments) : ExecutableNode
    {
        public XPathHostFunction Function { get; } = function;

        public IReadOnlyList<ExecutableNode> Arguments { get; } = arguments;

        public override XdmSequence Evaluate(XPathEvaluationContext context) => XdmSequence.Empty;
    }
}
