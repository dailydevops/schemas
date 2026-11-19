namespace NetEvolve.Schemas.XPath.Tests.Unit.Collations;

using System;
using System.Globalization;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;
using NetEvolve.Schemas.XPath.Tests.Unit.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Runs a QT3 case through the compiled expression, like <see cref="Qt3SequenceFixture"/> does, and adds the production lowering of
/// <see langword="and"/> and <see langword="or"/> (<see cref="LogicalNode"/>), which six cases of <c>fn-codepoint-equal</c> need and the shared fixture does
/// not register. The check of an expectation is the one of <see cref="Qt3SequenceFixture.Satisfies"/>.
/// </summary>
internal static class Qt3CollationFixture
{
    private const string FunctionNamespace = "http://www.w3.org/2005/xpath-functions";

    private static readonly XPathStaticContext Statics = TypeFixture
        .Statics.ToBuilder()
        .WithNamespace("fn", FunctionNamespace)
        .WithNamespace("err", "http://www.w3.org/2005/xqt-errors")
        .Build();

    /// <summary>Compiles and evaluates an expression; the error code when compiling or evaluating raises one.</summary>
    public static (XdmSequence Result, string Code) Run(string expression)
    {
        try
        {
            if (!XPathParser.TryParse(expression.AsSpan(), out var syntax, out var diagnostic))
            {
                return (default, diagnostic.Code);
            }

            _ = ExpressionCompiler.TryCompile(
                syntax!,
                expression,
                Statics,
                Registry(),
                out var compiled,
                out diagnostic
            );
            if (compiled is null)
            {
                return (default, diagnostic.Code);
            }

            using var evaluation = new XPathEvaluationContext();
            return (compiled.Evaluate(evaluation), string.Empty);
        }
        catch (XPathException failure)
        {
            return (default, failure.Diagnostic.Code);
        }
    }

    private static LoweringRegistry Registry()
    {
        var registry = TypeFixture.Registry();
        registry.Register<DecimalLiteral>(
            static (n, _) =>
                new CastFixture.ConstantNode(
                    XdmSequence.Create(XdmItem.FromDecimal(XsdDecimal.Parse(n.Lexeme.AsSpan()).Value))
                )
        );
        registry.Register<DoubleLiteral>(
            static (n, _) =>
                new CastFixture.ConstantNode(
                    XdmSequence.Create(XdmItem.FromDouble(double.Parse(n.Lexeme, CultureInfo.InvariantCulture)))
                )
        );
        registry.Register<FunctionCall>(LowerCall);
        registry.Register<LogicalExpression>(
            static (node, context) =>
                new LogicalNode(node.Operator, context.Lower(node.Left), context.Lower(node.Right))
        );
        ArithmeticLowerings.Register(registry, ArithmeticFixture.Table());
        CastingLowerings.Register(registry, CastingCatalog.Default.Table);
        ComparisonLowerings.Register(registry, ComparisonFixture.Table());
        return registry;
    }

    // A call lowering that resolves an unprefixed name in the function namespace, through the default catalog.
    private static CallNode LowerCall(FunctionCall node, LoweringContext context)
    {
        var name = node.Name;
        var namespaceUri = FunctionNamespace;
        if (name.Prefix is not null && !context.StaticContext.TryResolveNamespace(name.Prefix, out namespaceUri))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPST0081, name.Location.Offset, name.Location.Length);
        }

        var arity = node.Arguments.Count;
        var found = FunctionCatalog.Default.Registry.TryResolve(
            context.StaticContext,
            new XPathQName(namespaceUri!, name.LocalName),
            arity,
            XPathHostRequirements.None,
            out var function
        );
        if (!found)
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPST0017, node.Location.Offset, node.Location.Length);
        }

        var arguments = new ExecutableNode[arity];
        for (var i = 0; i < arity; i++)
        {
            arguments[i] = context.Lower(node.Arguments[i]);
        }

        return new CallNode(function!, arguments);
    }

    private sealed class CallNode(XPathFunction function, ExecutableNode[] arguments) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context)
        {
            var values = new XdmSequence[arguments.Length];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = arguments[i].Evaluate(context);
            }

            return function.Call(context, values);
        }
    }
}
