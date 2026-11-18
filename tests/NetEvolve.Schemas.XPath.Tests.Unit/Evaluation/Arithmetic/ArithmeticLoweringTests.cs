namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ArithmeticFixture;

public class ArithmeticLoweringTests
{
    private static readonly XPathQName Float = new(string.Empty, "float");
    private static readonly XPathQName Untyped = new(string.Empty, "untyped");
    private static readonly XPathQName Text = new(string.Empty, "text");
    private static readonly XPathQName Absent = new(string.Empty, "absent");

    private static readonly XPathStaticContext Statics = XPathStaticContext
        .Default.ToBuilder()
        .WithVariable(Float)
        .WithVariable(Untyped)
        .WithVariable(Text)
        .WithVariable(Absent)
        .Build();

    private static CompiledExpression? Compile(string expression, out XPathDiagnostic diagnostic)
    {
        var parsed = XPathParser.TryParse(expression.AsSpan(), out var syntax, out diagnostic);
        if (!parsed)
        {
            throw new InvalidOperationException("The test expression does not parse: " + diagnostic.Code);
        }

        _ = ExpressionCompiler.TryCompile(
            syntax!,
            expression,
            Statics,
            Registry(Table()),
            out var compiled,
            out diagnostic
        );
        return compiled;
    }

    private static XdmSequence Evaluate(string expression)
    {
        var compiled = Compile(expression, out var diagnostic)
            ?? throw new InvalidOperationException("The test expression does not compile: " + diagnostic.Code);

        using var context = new XPathEvaluationContext();
        context.Variables.Set(Float, XdmItem.FromFloat(1.5f));
        context.Variables.Set(Untyped, XdmItem.FromUntypedAtomic("10"));
        context.Variables.Set(Text, XdmItem.FromString("a"));
        context.Variables.Set(Absent, XdmSequence.Empty);
        return compiled.Evaluate(context);
    }

    [Test]
    [Arguments("1 + 2", "integer:3")]
    [Arguments("7 - 10", "integer:-3")]
    [Arguments("6 * 7", "integer:42")]
    [Arguments("7 div 2", "decimal:3.5")]
    [Arguments("7 idiv 2", "integer:3")]
    [Arguments("7 mod 2", "integer:1")]
    [Arguments("1.5 + 2", "decimal:3.5")]
    [Arguments("1.5e0 + 2", "double:3.5")]
    [Arguments("1 div 0.5e0", "double:2")]
    [Arguments("$float * 2", "float:3")]
    [Arguments("$untyped + 1", "double:11")]
    [Arguments("$absent + 1", "()")]
    public async Task BinaryExpression_IsLoweredAndEvaluated(string expression, string expected) =>
        await Assert.That(Describe(Evaluate(expression))).IsEqualTo(expected);

    [Test]
    [Arguments("2 + 3 * 4", "integer:14")]
    [Arguments("(2 + 3) * 4", "integer:20")]
    [Arguments("10 - 4 - 3", "integer:3")]
    [Arguments("100 div 10 div 5", "decimal:2")]
    [Arguments("2 * 3 mod 4", "integer:2")]
    [Arguments("17 idiv 5 * 5 + 17 mod 5", "integer:17")]
    public async Task NestedExpression_FollowsPrecedenceAndAssociativity(string expression, string expected) =>
        await Assert.That(Describe(Evaluate(expression))).IsEqualTo(expected);

    [Test]
    [Arguments("-5", "integer:-5")]
    [Arguments("+5", "integer:5")]
    [Arguments("- -5", "integer:5")]
    [Arguments("-(2 + 3)", "integer:-5")]
    [Arguments("-2.5", "decimal:-2.5")]
    [Arguments("-$float", "float:-1.5")]
    [Arguments("-$untyped", "double:-10")]
    [Arguments("-$absent", "()")]
    [Arguments("1 - -1", "integer:2")]
    [Arguments("-9223372036854775808", "integer:-9223372036854775808")]
    [Arguments("-9223372036854775809", "integer:-9223372036854775809")]
    public async Task UnaryExpression_IsLoweredAndEvaluated(string expression, string expected) =>
        await Assert.That(Describe(Evaluate(expression))).IsEqualTo(expected);

    [Test]
    [Arguments("$text + 1")]
    [Arguments("1 * $text")]
    [Arguments("-$text")]
    public async Task NonNumericOperand_RaisesXpty0004WhenEvaluated(string expression)
    {
        var error = Raised(() => Evaluate(expression));

        await Assert.That(error!.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPTY0004);
    }

    [Test]
    [Arguments("1 div 0", "FOAR0001")]
    [Arguments("1 idiv 0", "FOAR0001")]
    [Arguments("1 mod 0", "FOAR0001")]
    [Arguments("1.5 div 0.0", "FOAR0001")]
    [Arguments("1e0 idiv 0e0", "FOAR0001")]
    public async Task DivisionByZero_RaisesTheCodeOfTheSpecification(string expression, string code)
    {
        var error = Raised(() => Evaluate(expression));

        await Assert.That(error!.Diagnostic.Code).IsEqualTo(code);
        await Assert.That(error.Diagnostic.Offset).IsEqualTo(-1);
    }

    [Test]
    public async Task UndeclaredVariable_FailsToCompileWithXpst0008AtTheVariable()
    {
        var compiled = Compile("1 + $nothing", out var diagnostic);

        await Assert.That(compiled).IsNull();
        await Assert.That(diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0008);
        await Assert.That(diagnostic.Offset).IsGreaterThanOrEqualTo(4);
    }

    [Test]
    public async Task CompiledExpression_IsReusableAcrossEvaluations()
    {
        var compiled = Compile("$untyped * 2 + 1", out _)!;

        using var first = new XPathEvaluationContext();
        first.Variables.Set(Untyped, XdmItem.FromUntypedAtomic("1"));
        using var second = new XPathEvaluationContext();
        second.Variables.Set(Untyped, XdmItem.FromUntypedAtomic("5"));

        await Assert.That(Describe(compiled.Evaluate(first))).IsEqualTo("double:3");
        await Assert.That(Describe(compiled.Evaluate(second))).IsEqualTo("double:11");
        await Assert.That(Describe(compiled.Evaluate(first))).IsEqualTo("double:3");
    }

    [Test]
    public async Task Register_AddsTheLoweringsOfBothNodeKindsAndCannotRunTwice()
    {
        var registry = new LoweringRegistry();
        var table = Table();
        ArithmeticLowerings.Register(registry, table);

        await Assert.That(registry.TryGet(typeof(ArithmeticExpression), out var binary)).IsTrue();
        await Assert.That(registry.TryGet(typeof(UnaryExpression), out var unary)).IsTrue();
        await Assert.That(binary).IsNotNull();
        await Assert.That(unary).IsNotNull();
        await Assert.That(() => ArithmeticLowerings.Register(registry, table)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Register_NullArguments_Throw()
    {
        await Assert.That(() => ArithmeticLowerings.Register(null!, Table())).Throws<ArgumentNullException>();
        await Assert
            .That(() => ArithmeticLowerings.Register(new LoweringRegistry(), null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task LoweredNodes_UseTheTableTheyWereRegisteredWith()
    {
        // An entry that a later folder adds to the shared table is visible to a lowering registered before the entry exists.
        var table = new ArithmeticOperatorTable();
        var registry = Registry(table);
        table.Register(
            ArithmeticOperator.Add,
            XdmTypeId.XsString,
            XdmTypeId.XsString,
            static (in XdmItem l, in XdmItem r) => XdmItem.FromString("joined")
        );
        var parsed = XPathParser.TryParse("'a' + 'b'".AsSpan(), out var syntax, out _);
        _ = ExpressionCompiler.TryCompile(syntax!, "'a' + 'b'", Statics, registry, out var compiled, out _);

        using var context = new XPathEvaluationContext();

        await Assert.That(parsed).IsTrue();
        await Assert.That(Describe(compiled!.Evaluate(context))).IsEqualTo("string:joined");
    }

    [Test]
    public async Task Nodes_NullArguments_Throw()
    {
        var node = new ConstantNode(XdmSequence.Empty);
        var table = Table();

        await Assert
            .That(() => new ArithmeticNode(ArithmeticOperator.Add, null!, node, table))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new ArithmeticNode(ArithmeticOperator.Add, node, null!, table))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new ArithmeticNode(ArithmeticOperator.Add, node, node, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new UnaryArithmeticNode(UnaryOperator.Minus, null!, table))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new UnaryArithmeticNode(UnaryOperator.Minus, node, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new ArithmeticNode(ArithmeticOperator.Add, node, node, table).Evaluate(null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new UnaryArithmeticNode(UnaryOperator.Minus, node, table).Evaluate(null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Nodes_OperatorOutsideTheEnumeration_Throws()
    {
        var node = new ConstantNode(XdmSequence.Empty);
        var table = Table();

        await Assert
            .That(() => new ArithmeticNode((ArithmeticOperator)99, node, node, table))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => new UnaryArithmeticNode((UnaryOperator)99, node, table))
            .Throws<ArgumentOutOfRangeException>();
    }
}
