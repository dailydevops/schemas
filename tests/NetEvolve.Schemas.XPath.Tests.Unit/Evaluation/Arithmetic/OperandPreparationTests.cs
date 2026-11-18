namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;

using System;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Axes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ArithmeticFixture;

public class OperandPreparationTests
{
    private static XdmSequence One(string spec) => XdmSequence.Create(Item(spec));

    private static XdmSequence Two(string first, string second) => XdmSequence.Create([Item(first), Item(second)]);

    private static async Task AssertRaises(Func<XdmSequence> action, XPathErrorCode code)
    {
        var error = Raised(action);

        await Assert.That(error).IsNotNull();
        await Assert.That(error!.Diagnostic.QName).IsEqualTo(code);
    }

    [Test]
    [Arguments("+")]
    [Arguments("-")]
    [Arguments("*")]
    [Arguments("div")]
    [Arguments("idiv")]
    [Arguments("mod")]
    public async Task EmptyOperand_YieldsTheEmptySequence(string token)
    {
        await Assert.That(Run(Operator(token), XdmSequence.Empty, One("i:1")).IsEmpty).IsTrue();
        await Assert.That(Run(Operator(token), One("i:1"), XdmSequence.Empty).IsEmpty).IsTrue();
        await Assert.That(Run(Operator(token), XdmSequence.Empty, XdmSequence.Empty).IsEmpty).IsTrue();
    }

    [Test]
    public async Task EmptyOperand_WinsOverASequenceOfSeveralItemsOnTheOtherSide()
    {
        // XPath 2.0, 3.4: the empty sequence is checked before the length.
        await Assert.That(Run(ArithmeticOperator.Add, XdmSequence.Empty, Two("i:1", "i:2")).IsEmpty).IsTrue();
        await Assert.That(Run(ArithmeticOperator.Add, Two("i:1", "i:2"), XdmSequence.Empty).IsEmpty).IsTrue();
    }

    [Test]
    public async Task OperandOfSeveralItems_RaisesXpty0004()
    {
        await AssertRaises(() => Run(ArithmeticOperator.Add, Two("i:1", "i:2"), One("i:1")), XPathErrorCodes.XPTY0004);
        await AssertRaises(
            () => Run(ArithmeticOperator.Multiply, One("i:1"), Two("i:1", "i:2")),
            XPathErrorCodes.XPTY0004
        );
    }

    [Test]
    [Arguments("u:3", "+", "i:4", "double:7")]
    [Arguments("i:4", "+", "u:3", "double:7")]
    [Arguments("u:3", "+", "u:4", "double:7")]
    [Arguments("u:1e1", "*", "i:2", "double:20")]
    [Arguments("u:  5.5  ", "+", "i:1", "double:6.5")]
    [Arguments("u:7", "idiv", "i:2", "integer:3")]
    [Arguments("u:7", "div", "i:2", "double:3.5")]
    [Arguments("u:7", "mod", "i:4", "double:3")]
    [Arguments("u:INF", "+", "i:1", "double:INF")]
    [Arguments("u:NaN", "+", "i:1", "double:NaN")]
    public async Task UntypedAtomicOperand_IsCastToDouble(string left, string op, string right, string expected) =>
        await Assert.That(Describe(Run(left, op, right))).IsEqualTo(expected);

    [Test]
    [Arguments("u:abc")]
    [Arguments("u:")]
    [Arguments("u:1,5")]
    public async Task UntypedAtomicOperand_ThatIsNotADouble_RaisesForg0001(string operand) =>
        await AssertRaises(() => Run(Operator("+"), One(operand), One("i:1")), XPathErrorCodes.FORG0001);

    [Test]
    [Arguments("s:1", "+", "i:1")]
    [Arguments("i:1", "+", "s:1")]
    [Arguments("s:a", "*", "s:b")]
    [Arguments("b:true", "+", "i:1")]
    [Arguments("i:1", "div", "b:false")]
    [Arguments("s:1", "idiv", "i:1")]
    [Arguments("i:1", "mod", "s:1")]
    public async Task NonNumericOperand_RaisesXpty0004(string left, string op, string right) =>
        await AssertRaises(() => Run(left, op, right), XPathErrorCodes.XPTY0004);

    [Test]
    public async Task NodeOperand_IsAtomized_ThenTreatedAsUntypedAtomic()
    {
        // The attribute id="1" has the untyped value "1".
        var attribute = XdmSequence.Create(XdmItem.FromNode(AxisFixture.Node("id")));

        var result = Run(ArithmeticOperator.Add, attribute, One("i:1"));

        await Assert.That(Describe(result)).IsEqualTo("double:2");
    }

    [Test]
    public async Task NodeOperand_WithANonNumericValue_RaisesForg0001()
    {
        var text = XdmSequence.Create(XdmItem.FromNode(AxisFixture.Node("t1")));

        await AssertRaises(() => Run(ArithmeticOperator.Add, text, One("i:1")), XPathErrorCodes.FORG0001);
    }

    [Test]
    public async Task CommentNode_AtomizesToAString_AndRaisesXpty0004()
    {
        var comment = XdmSequence.Create(XdmItem.FromNode(AxisFixture.Node("c")));

        await AssertRaises(() => Run(ArithmeticOperator.Add, comment, One("i:1")), XPathErrorCodes.XPTY0004);
    }

    [Test]
    public async Task OperandsAreEvaluatedLeftBeforeRight()
    {
        var order = new System.Collections.Generic.List<string>();
        var node = new ArithmeticNode(
            ArithmeticOperator.Add,
            new Recording("left", order, One("i:1")),
            new Recording("right", order, One("i:2")),
            Table()
        );

        using var context = new NetEvolve.Schemas.XPath.Context.XPathEvaluationContext();
        _ = node.Evaluate(context);

        await Assert.That(string.Join(",", order)).IsEqualTo("left,right");
    }

    private sealed class Recording(string name, System.Collections.Generic.List<string> order, XdmSequence value)
        : NetEvolve.Schemas.XPath.Compilation.ExecutableNode
    {
        public override XdmSequence Evaluate(NetEvolve.Schemas.XPath.Context.XPathEvaluationContext context)
        {
            order.Add(name);
            return value;
        }
    }

    [Test]
    [Arguments("i:5", "integer:5")]
    [Arguments("d:2.5", "decimal:2.5")]
    [Arguments("f:1.5", "float:1.5")]
    [Arguments("e:1.5", "double:1.5")]
    [Arguments("w:100000000000000000000", "integer:100000000000000000000")]
    [Arguments("u:3", "double:3")]
    public async Task UnaryPlus_KeepsTheValueAndTheType(string operand, string expected) =>
        await Assert.That(Describe(Run(UnaryOperator.Plus, operand))).IsEqualTo(expected);

    [Test]
    [Arguments("i:5", "integer:-5")]
    [Arguments("i:-5", "integer:5")]
    [Arguments("i:0", "integer:0")]
    [Arguments("d:2.5", "decimal:-2.5")]
    [Arguments("d:0", "decimal:0")]
    [Arguments("f:1.5", "float:-1.5")]
    [Arguments("e:1.5", "double:-1.5")]
    [Arguments("e:0", "double:-0")]
    [Arguments("e:-0", "double:0")]
    [Arguments("e:INF", "double:-INF")]
    [Arguments("e:NaN", "double:NaN")]
    [Arguments("u:3", "double:-3")]
    [Arguments("i:-9223372036854775808", "integer:9223372036854775808")]
    [Arguments("w:9223372036854775808", "integer:-9223372036854775808")]
    [Arguments("w:100000000000000000000", "integer:-100000000000000000000")]
    public async Task UnaryMinus_NegatesTheValueAndKeepsTheType(string operand, string expected) =>
        await Assert.That(Describe(Run(UnaryOperator.Minus, operand))).IsEqualTo(expected);

    [Test]
    public async Task UnaryOperators_WithAnEmptyOperand_YieldTheEmptySequence()
    {
        await Assert.That(Run(UnaryOperator.Plus, XdmSequence.Empty).IsEmpty).IsTrue();
        await Assert.That(Run(UnaryOperator.Minus, XdmSequence.Empty).IsEmpty).IsTrue();
    }

    [Test]
    public async Task UnaryOperators_WithSeveralItems_RaiseXpty0004()
    {
        await AssertRaises(() => Run(UnaryOperator.Minus, Two("i:1", "i:2")), XPathErrorCodes.XPTY0004);
        await AssertRaises(() => Run(UnaryOperator.Plus, Two("i:1", "i:2")), XPathErrorCodes.XPTY0004);
    }

    [Test]
    [Arguments("s:1")]
    [Arguments("b:true")]
    public async Task UnaryOperators_WithANonNumericOperand_RaiseXpty0004(string operand)
    {
        await AssertRaises(() => Run(UnaryOperator.Minus, operand), XPathErrorCodes.XPTY0004);
        await AssertRaises(() => Run(UnaryOperator.Plus, operand), XPathErrorCodes.XPTY0004);
    }

    [Test]
    public async Task UnaryOperators_WithAnUntypedAtomicThatIsNotADouble_RaiseForg0001() =>
        await AssertRaises(() => Run(UnaryOperator.Minus, "u:abc"), XPathErrorCodes.FORG0001);
}
