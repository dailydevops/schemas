namespace NetEvolve.Schemas.XPath.Tests.Unit.Limits;

using System;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Compiles and runs expressions under chosen limits. The range expression and the string producer are test-local stand-ins for the
/// packages that own them (range and sequence construction, #226; string functions, #263 and #355): each charges the limit helper
/// that the real node calls, so the tests pin the contract those packages build on.
/// </summary>
internal static class LimitsFixture
{
    public static readonly XPathStaticContext Statics = AxisFixture.Statics;

    /// <summary>Gets a registry with the literals, parentheses, arithmetic, paths, and the two stand-ins.</summary>
    public static LoweringRegistry Registry()
    {
        var registry = ArithmeticFixture.Registry(ArithmeticFixture.Table());
        PathLowerings.Register(registry);
        registry.Register<RangeExpression>(
            static (n, c) => new RangeStandIn(n.Location, c.Lower(n.From), c.Lower(n.To))
        );
        registry.Register<FunctionCall>(
            static (n, c) =>
                string.Equals(n.Name.LocalName, "repeat", StringComparison.Ordinal)
                    ? new RepeatStandIn(n.Location, c.Lower(n.Arguments[0]), c.Lower(n.Arguments[1]))
                    : throw new InvalidOperationException("Unknown test function: " + n.Name.LocalName)
        );
        return registry;
    }

    /// <summary>Parses and compiles; returns <see langword="null"/> and the diagnostic when the expression does not compile.</summary>
    public static CompiledExpression? Compile(string expression, XPathLimits? limits, out XPathDiagnostic diagnostic)
    {
        if (!XPathParser.TryParse(expression.AsSpan(), out var syntax, out diagnostic))
        {
            throw new InvalidOperationException("The test expression does not parse: " + diagnostic.Code);
        }

        _ = ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            Statics,
            Registry(),
            out var compiled,
            out diagnostic,
            limits
        );
        return compiled;
    }

    public static CompiledExpression CompileOrThrow(string expression, XPathLimits? limits = null) =>
        Compile(expression, limits, out var diagnostic)
        ?? throw new InvalidOperationException($"'{expression}' does not compile: {diagnostic.Code}");

    /// <summary>Compiles and evaluates with the context item <c>a</c> of the axis tree; returns the diagnostic of the failure, <see langword="default"/> when there is none.</summary>
    public static XPathDiagnostic Evaluate(string expression, XPathLimits limits, out XdmSequence result)
    {
        result = XdmSequence.Empty;
        var compiled = Compile(expression, limits, out var diagnostic);
        if (compiled is null)
        {
            return diagnostic;
        }

        using var context = NewContext(limits);
        try
        {
            result = compiled.Evaluate(context);
            return default;
        }
        catch (XPathException exception)
        {
            return exception.Diagnostic;
        }
    }

    public static XPathEvaluationContext NewContext(XPathLimits limits)
    {
        var context = new XPathEvaluationContext { Limits = limits };
        context.SetContextItem(XdmItem.FromNode(AxisFixture.Node("a")));
        return context;
    }

    /// <summary>Builds <c>1+1+...+1</c> with the given number of terms; the syntax tree is left-deep, so its depth equals the number of terms.</summary>
    public static string Chain(int terms)
    {
        var text = new System.Text.StringBuilder(terms * 2);
        _ = text.Append('1');
        for (var i = 1; i < terms; i++)
        {
            _ = text.Append("+1");
        }

        return text.ToString();
    }

    private static long ToLong(in XdmSequence value)
    {
        _ = value[0].TryGetBigInteger(out var number);
        return number > long.MaxValue ? long.MaxValue
            : number < long.MinValue ? long.MinValue
            : (long)number;
    }

    // Stand-in for the range expression: it charges the range before it builds anything.
    private sealed class RangeStandIn(SourceSpan location, ExecutableNode from, ExecutableNode to) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context)
        {
            var first = ToLong(from.Evaluate(context));
            var last = ToLong(to.Evaluate(context));
            context.Limits.ThrowIfRangeTooLarge(first, last, location);

            if (first > last)
            {
                return XdmSequence.Empty;
            }

            var items = new XdmItem[checked((int)(last - first + 1))];
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = XdmItem.FromInteger(first + i);
            }

            return XdmSequence.Create(items);
        }
    }

    // Stand-in for a string producer: repeat(text, count) charges the length before it builds the string.
    private sealed class RepeatStandIn(SourceSpan location, ExecutableNode text, ExecutableNode count) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context)
        {
            _ = text.Evaluate(context)[0].TryGetString(out var value);
            var times = ToLong(count.Evaluate(context));
            context.Limits.ThrowIfStringTooLong(value!.Length * times, location);

            var builder = new System.Text.StringBuilder(value.Length * (int)times);
            for (var i = 0; i < times; i++)
            {
                _ = builder.Append(value);
            }

            return XdmSequence.Create(XdmItem.FromString(builder.ToString()));
        }
    }
}
