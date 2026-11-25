namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow30;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Evaluation.Flow30;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Compiles and runs XPath 3.0 expressions through the lowerings under test next to the lowerings of the flow and path folders, the
/// production arithmetic and comparisons, and fake <c>position()</c> and <c>last()</c> calls that read the focus.
/// </summary>
internal static class Flow30Fixture
{
    public static LoweringRegistry Registry()
    {
        var registry = new LoweringRegistry();
        FlowLowerings.Register(registry);
        Flow30Lowerings.Register(registry);
        PathLowerings.Register(registry);
        ArithmeticLowerings.Register(registry, ArithmeticFixture.Table());
        ComparisonLowerings.Register(registry, ComparisonFixture.Table());
        registry.Register<FunctionCall>((n, _) => new FocusCall(n.Name.LocalName));
        return registry;
    }

    public static CompiledExpression Compile(string expression)
    {
        if (!XPathParser.TryParse(expression.AsSpan(), XPathVersion.V30, out var syntax, out var diagnostic))
        {
            throw new InvalidOperationException("The test expression does not parse: " + diagnostic.Code);
        }

        return ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            XPathStaticContext.Default,
            Registry(),
            out var compiled,
            out diagnostic
        )
            ? compiled!
            : throw new InvalidOperationException(diagnostic.QName.LocalName);
    }

    public static string Run(string expression, XPathLimits? limits = null)
    {
        using var context = new XPathEvaluationContext();
        if (limits is not null)
        {
            context.Limits = limits;
        }

        return ArithmeticFixture.Describe(Compile(expression).Evaluate(context));
    }

    /// <summary>The local name of the error code that compiling or evaluating raises, empty when none.</summary>
    public static string Code(string expression, XPathLimits? limits = null)
    {
        try
        {
            _ = Run(expression, limits);
            return string.Empty;
        }
        catch (XPathException exception)
        {
            return exception.Diagnostic.QName.LocalName;
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message;
        }
    }

    private sealed class FocusCall(string name) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context) =>
            XdmSequence.Create(XdmItem.FromInteger(name == "position" ? context.Position : context.Size));
    }
}
