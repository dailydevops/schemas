namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow;

using System;
using System.Text;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Compiles and runs expressions through the lowerings under test (<see cref="FlowLowerings"/>) next to the production arithmetic and
/// comparisons. The function calls are counting fakes: <c>true()</c> and <c>false()</c>; <c>tick()</c> and <c>tickf()</c>, which count
/// on <see cref="Probe.First"/> and return true or false; <c>tock()</c>, which counts on <see cref="Probe.Second"/>; and <c>boom()</c>,
/// which raises FORG0001. The variables are declared in the static context; <see cref="NewContext"/> binds them, except
/// <see cref="Unbound"/>.
/// </summary>
internal static class FlowFixture
{
    public static readonly XPathQName Host = new(string.Empty, "host");
    public static readonly XPathQName Unbound = new(string.Empty, "unbound");
    public static readonly XPathQName Numbers = new(string.Empty, "numbers");
    public static readonly XPathQName Empty = new(string.Empty, "empty");
    public static readonly XPathQName Zero = new(string.Empty, "zero");
    public static readonly XPathQName One = new(string.Empty, "one");
    public static readonly XPathQName Blank = new(string.Empty, "blank");
    public static readonly XPathQName Word = new(string.Empty, "word");
    public static readonly XPathQName NotANumber = new(string.Empty, "nan");
    public static readonly XPathQName Pair = new(string.Empty, "pair");
    public static readonly XPathQName Node = new(string.Empty, "node");
    public static readonly XPathQName Untyped = new(string.Empty, "untyped");
    public static readonly XPathQName BadUntyped = new(string.Empty, "badUntyped");

    private static readonly XPathStaticContext Statics = XPathStaticContext
        .Default.ToBuilder()
        .WithVariable(Host)
        .WithVariable(Unbound)
        .WithVariable(Numbers)
        .WithVariable(Empty)
        .WithVariable(Zero)
        .WithVariable(One)
        .WithVariable(Blank)
        .WithVariable(Word)
        .WithVariable(NotANumber)
        .WithVariable(Pair)
        .WithVariable(Node)
        .WithVariable(Untyped)
        .WithVariable(BadUntyped)
        .Build();

    /// <summary>The counters the fake calls advance.</summary>
    public sealed class Probe
    {
        public int First { get; set; }

        public int Second { get; set; }
    }

    /// <summary>Gets a registry with the lowerings under test, the production arithmetic and comparisons, and the fake calls.</summary>
    public static LoweringRegistry Registry(Probe probe)
    {
        var registry = new LoweringRegistry();
        FlowLowerings.Register(registry);
        ArithmeticLowerings.Register(registry, ArithmeticFixture.Table());
        ComparisonLowerings.Register(registry, ComparisonFixture.Table());
        registry.Register<FunctionCall>((n, _) => new CallNode(probe, n.Name.LocalName));
        return registry;
    }

    public static CompiledExpression? TryCompile(string expression, out XPathDiagnostic diagnostic, Probe? probe = null)
    {
        if (!XPathParser.TryParse(expression.AsSpan(), out var syntax, out diagnostic))
        {
            throw new InvalidOperationException("The test expression does not parse: " + diagnostic.Code);
        }

        _ = ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            Statics,
            Registry(probe ?? new Probe()),
            out var compiled,
            out diagnostic
        );
        return compiled;
    }

    public static CompiledExpression Compile(string expression, Probe? probe = null) =>
        TryCompile(expression, out var diagnostic, probe)
        ?? throw new InvalidOperationException($"'{expression}' does not compile: {diagnostic.Code}");

    public static XPathEvaluationContext NewContext(XPathLimits? limits = null)
    {
        var context = new XPathEvaluationContext();
        if (limits is not null)
        {
            context.Limits = limits;
        }

        context.Variables.Set(Host, XdmItem.FromInteger(100));
        context.Variables.Set(
            Numbers,
            XdmSequence.Create([XdmItem.FromInteger(1), XdmItem.FromInteger(2), XdmItem.FromInteger(3)])
        );
        context.Variables.Set(Empty, XdmSequence.Empty);
        context.Variables.Set(Zero, XdmItem.FromInteger(0));
        context.Variables.Set(One, XdmItem.FromInteger(1));
        context.Variables.Set(Blank, XdmItem.FromString(string.Empty));
        context.Variables.Set(Word, XdmItem.FromString("a"));
        context.Variables.Set(NotANumber, XdmItem.FromDouble(double.NaN));
        context.Variables.Set(Pair, XdmSequence.Create([XdmItem.FromInteger(1), XdmItem.FromInteger(2)]));
        context.Variables.Set(Node, XdmItem.FromNode(AxisFixture.Node("a")));
        context.Variables.Set(Untyped, XdmItem.FromUntypedAtomic("2"));
        context.Variables.Set(BadUntyped, XdmItem.FromUntypedAtomic("x"));
        return context;
    }

    public static XdmSequence Evaluate(string expression, Probe? probe = null, XPathLimits? limits = null)
    {
        using var context = NewContext(limits);
        return Compile(expression, probe).Evaluate(context);
    }

    /// <summary>Evaluates an expression and describes the value: <c>()</c> for the empty sequence, otherwise the items.</summary>
    public static string Run(string expression, Probe? probe = null, XPathLimits? limits = null) =>
        ArithmeticFixture.Describe(Evaluate(expression, probe, limits));

    /// <summary>Compiles and evaluates an expression; the local name of the error code that either step raises, empty when none.</summary>
    public static string Code(string expression, Probe? probe = null, XPathLimits? limits = null)
    {
        var compiled = TryCompile(expression, out var diagnostic, probe);
        if (compiled is null)
        {
            return diagnostic.QName.LocalName;
        }

        using var context = NewContext(limits);
        try
        {
            _ = compiled.Evaluate(context);
            return string.Empty;
        }
        catch (XPathException exception)
        {
            return exception.Diagnostic.QName.LocalName;
        }
    }

    /// <summary>A list of <c>count</c> binding clauses, <c>for $a0 in 1, $a1 in 1, ... return 1</c>.</summary>
    public static string ForWithBindings(int count)
    {
        var text = new StringBuilder("for ");
        for (var i = 0; i < count; i++)
        {
            _ = text.Append(i == 0 ? string.Empty : ", ").Append("$a").Append(i).Append(" in 1");
        }

        return text.Append(" return 1").ToString();
    }

    private sealed class CallNode(Probe probe, string name) : ExecutableNode
    {
        public override XdmSequence Evaluate(XPathEvaluationContext context)
        {
            switch (name)
            {
                case "true":
                    return XdmSequence.Create(XdmItem.FromBoolean(true));
                case "false":
                    return XdmSequence.Create(XdmItem.FromBoolean(false));
                case "tick":
                    probe.First++;
                    return XdmSequence.Create(XdmItem.FromBoolean(true));
                case "tickf":
                    probe.First++;
                    return XdmSequence.Create(XdmItem.FromBoolean(false));
                case "tock":
                    probe.Second++;
                    return XdmSequence.Create(XdmItem.FromBoolean(true));
                case "boom":
                    throw XPathErrors.Exception(XPathErrorCodes.FORG0001);
                default:
                    throw new InvalidOperationException("Unknown test function: " + name);
            }
        }
    }
}
