namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ComparisonFixture;

/// <summary>The lowering of the comparison expressions: parsed text, through the registry, to an evaluated result.</summary>
public class ComparisonLoweringTests
{
    private static readonly XPathQName First = new(string.Empty, "first");
    private static readonly XPathQName Second = new(string.Empty, "second");
    private static readonly XPathQName Both = new(string.Empty, "both");
    private static readonly XPathQName Untyped = new(string.Empty, "untyped");
    private static readonly XPathQName Absent = new(string.Empty, "absent");
    private static readonly XPathQName Text = new(string.Empty, "text");
    private static readonly XPathQName S12 = new(string.Empty, "s12");
    private static readonly XPathQName S23 = new(string.Empty, "s23");
    private static readonly XPathQName S34 = new(string.Empty, "s34");
    private static readonly XPathQName S03 = new(string.Empty, "s03");
    private static readonly XPathQName Mixed = new(string.Empty, "mixed");

    private static readonly XPathStaticContext Statics = XPathStaticContext
        .Default.ToBuilder()
        .WithVariable(First)
        .WithVariable(Second)
        .WithVariable(Both)
        .WithVariable(Untyped)
        .WithVariable(Absent)
        .WithVariable(Text)
        .WithVariable(S12)
        .WithVariable(S23)
        .WithVariable(S34)
        .WithVariable(S03)
        .WithVariable(Mixed)
        .Build();

    private static LoweringRegistry Registry()
    {
        var registry = ArithmeticFixture.Registry(ArithmeticFixture.Table());
        ComparisonLowerings.Register(registry, Table());
        return registry;
    }

    private static XdmSequence Evaluate(string expression)
    {
        if (!XPathParser.TryParse(expression.AsSpan(), out var syntax, out var diagnostic))
        {
            throw new InvalidOperationException("The test expression does not parse: " + diagnostic.Code);
        }

        if (!ExpressionCompiler.TryCompile(syntax!, expression, Statics, Registry(), out var compiled, out diagnostic))
        {
            throw new InvalidOperationException("The test expression does not compile: " + diagnostic.Code);
        }

        var tree = new CountingTree(5);
        using var context = new XPathEvaluationContext();
        context.Variables.Set(First, XdmItem.FromNode(tree.At(1)));
        context.Variables.Set(Second, XdmItem.FromNode(tree.At(3)));
        context.Variables.Set(Both, XdmSequence.Create([XdmItem.FromNode(tree.At(1)), XdmItem.FromNode(tree.At(3))]));
        context.Variables.Set(Untyped, XdmItem.FromUntypedAtomic("10"));
        context.Variables.Set(Absent, XdmSequence.Empty);
        context.Variables.Set(Text, XdmItem.FromString("a"));
        context.Variables.Set(S12, Many("i:1", "i:2"));
        context.Variables.Set(S23, Many("i:2", "i:3"));
        context.Variables.Set(S34, Many("i:3", "i:4"));
        context.Variables.Set(S03, Many("i:0", "i:3"));
        context.Variables.Set(Mixed, Many("i:1", "s:a"));
        return compiled!.Evaluate(context);
    }

    private static string Result(string expression) => Outcome(() => Evaluate(expression));

    [Test]
    [Arguments("1 eq 1", "true")]
    [Arguments("1 ne 1", "false")]
    [Arguments("1 lt 2", "true")]
    [Arguments("2 le 1", "false")]
    [Arguments("2 gt 1", "true")]
    [Arguments("1 ge 2", "false")]
    [Arguments("1.5 eq 1.50", "true")]
    [Arguments("1 eq 1.0e0", "true")]
    [Arguments("'a' lt 'b'", "true")]
    [Arguments("'a' eq 'a'", "true")]
    [Arguments("$absent eq 1", "()")]
    [Arguments("1 eq $absent", "()")]
    [Arguments("$untyped eq '10'", "true")]
    [Arguments("$untyped eq 10", "XPTY0004")]
    [Arguments("1 eq 'a'", "XPTY0004")]
    public async Task ValueComparison_IsLoweredAndEvaluated(string expression, string expected) =>
        await Assert.That(Result(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("1 = 1", "true")]
    [Arguments("1 != 1", "false")]
    [Arguments("1 < 2", "true")]
    [Arguments("2 <= 1", "false")]
    [Arguments("2 > 1", "true")]
    [Arguments("1 >= 2", "false")]
    [Arguments("$s12 = $s23", "true")]
    [Arguments("$s12 = $s34", "false")]
    [Arguments("$s12 != $s23", "true")]
    [Arguments("$s12 < $s03", "true")]
    [Arguments("$absent = 1", "false")]
    [Arguments("$absent != $absent", "false")]
    [Arguments("() = ()", "false")]
    [Arguments("$untyped = 10", "true")]
    [Arguments("$untyped = '10'", "true")]
    [Arguments("$untyped < 9", "false")]
    [Arguments("$untyped = 'a'", "false")]
    [Arguments("$text = 1", "XPTY0004")]
    [Arguments("$mixed = 1", "true")]
    public async Task GeneralComparison_IsLoweredAndEvaluated(string expression, string expected) =>
        await Assert.That(Result(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("$first is $first", "true")]
    [Arguments("$first is $second", "false")]
    [Arguments("$first << $second", "true")]
    [Arguments("$second << $first", "false")]
    [Arguments("$second >> $first", "true")]
    [Arguments("$first >> $second", "false")]
    [Arguments("$absent is $first", "()")]
    [Arguments("$first << $absent", "()")]
    [Arguments("$both is $first", "XPTY0004")]
    [Arguments("$first >> $both", "XPTY0004")]
    [Arguments("1 is $first", "XPTY0004")]
    public async Task NodeComparison_IsLoweredAndEvaluated(string expression, string expected) =>
        await Assert.That(Result(expression)).IsEqualTo(expected);

    [Test]
    [Arguments("1 + 1 = 2", "true")]
    [Arguments("2 = 1 + 1", "true")]
    [Arguments("1 + 2 lt 2 * 2", "true")]
    [Arguments("(1 = 1) = (2 = 2)", "true")]
    [Arguments("(1 lt 2) eq (3 gt 2)", "true")]
    [Arguments("(1 eq 1) ne (1 eq 2)", "true")]
    [Arguments("($first is $first) = (1 = 1)", "true")]
    public async Task Comparison_BindsLooserThanArithmetic_AndNestsThroughParentheses(
        string expression,
        string expected
    ) => await Assert.That(Result(expression)).IsEqualTo(expected);

    [Test]
    public async Task EveryOperatorOfTheSyntaxTree_HasALowering()
    {
        foreach (
            var token in new[] { "=", "!=", "<", "<=", ">", ">=", "eq", "ne", "lt", "le", "gt", "ge", "is", "<<", ">>" }
        )
        {
            var expression = token is "is" or "<<" or ">>" ? "$first " + token + " $second" : "1 " + token + " 2";

            await Assert.That(Result(expression)).IsNotEqualTo("XP0002");
        }
    }

    [Test]
    public async Task LoweredNodes_UseTheTableTheyWereRegisteredWith()
    {
        var registry = ArithmeticFixture.Registry(new ArithmeticOperatorTable());
        ComparisonLowerings.Register(registry, new ComparisonOperatorTable());
        _ = XPathParser.TryParse("1 eq 1".AsSpan(), out var syntax, out _);
        _ = ExpressionCompiler.TryCompile(syntax!, "1 eq 1", Statics, registry, out var compiled, out _);
        using var context = new XPathEvaluationContext();

        var error = ArithmeticFixture.Raised(() => compiled!.Evaluate(context));

        await Assert.That(error!.Diagnostic.Code).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task Register_Twice_Throws()
    {
        var registry = new LoweringRegistry();
        var table = Table();
        ComparisonLowerings.Register(registry, table);

        await Assert.That(() => ComparisonLowerings.Register(registry, table)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Register_WithANullArgument_Throws()
    {
        await Assert.That(() => ComparisonLowerings.Register(null!, Table())).Throws<ArgumentNullException>();
        await Assert
            .That(() => ComparisonLowerings.Register(new LoweringRegistry(), null!))
            .Throws<ArgumentNullException>();
    }
}
