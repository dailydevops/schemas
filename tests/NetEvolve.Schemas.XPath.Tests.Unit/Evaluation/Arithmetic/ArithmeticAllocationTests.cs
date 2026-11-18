namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;

using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ArithmeticFixture;

/// <summary>The hot-path rule of #123: an arithmetic expression allocates no managed memory in steady state.</summary>
public class ArithmeticAllocationTests
{
    private static async Task AssertNoAllocation(string op, string left, string right, string expected)
    {
        var node = new ArithmeticNode(
            Operator(op),
            new ConstantNode(XdmSequence.Create(Item(left))),
            new ConstantNode(XdmSequence.Create(Item(right))),
            Table()
        );
        using var context = new XPathEvaluationContext();
        var result = XdmSequence.Empty;

        AllocationAssert.NoAllocation(() => result = node.Evaluate(context), warmups: 50);

        await Assert.That(Describe(result)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("+", "i:2", "i:3", "integer:5")]
    [Arguments("-", "i:2", "i:3", "integer:-1")]
    [Arguments("*", "i:2", "i:3", "integer:6")]
    [Arguments("idiv", "i:7", "i:2", "integer:3")]
    [Arguments("mod", "i:7", "i:2", "integer:1")]
    public async Task IntegerArithmetic_AllocatesNothing(string op, string left, string right, string expected) =>
        await AssertNoAllocation(op, left, right, expected);

    [Test]
    [Arguments("+", "e:2", "e:3", "double:5")]
    [Arguments("-", "e:2", "e:3", "double:-1")]
    [Arguments("*", "e:2", "e:3", "double:6")]
    [Arguments("div", "e:3", "e:2", "double:1.5")]
    [Arguments("mod", "e:7", "e:2", "double:1")]
    [Arguments("+", "i:2", "e:3", "double:5")]
    public async Task DoubleArithmetic_AllocatesNothing(string op, string left, string right, string expected) =>
        await AssertNoAllocation(op, left, right, expected);

    [Test]
    [Arguments("+", "f:2", "f:3", "float:5")]
    [Arguments("div", "f:3", "f:2", "float:1.5")]
    [Arguments("+", "i:2", "f:3", "float:5")]
    public async Task FloatArithmetic_AllocatesNothing(string op, string left, string right, string expected) =>
        await AssertNoAllocation(op, left, right, expected);

    [Test]
    public async Task UnaryMinus_OfIntegerAndDouble_AllocatesNothing()
    {
        var integer = new UnaryArithmeticNode(
            UnaryOperator.Minus,
            new ConstantNode(XdmSequence.Create(Item("i:5"))),
            Table()
        );
        var number = new UnaryArithmeticNode(
            UnaryOperator.Minus,
            new ConstantNode(XdmSequence.Create(Item("e:5"))),
            Table()
        );
        using var context = new XPathEvaluationContext();
        var result = XdmSequence.Empty;

        AllocationAssert.NoAllocation(
            () =>
            {
                result = integer.Evaluate(context);
                result = number.Evaluate(context);
            },
            warmups: 50
        );

        await Assert.That(Describe(result)).IsEqualTo("double:-5");
    }

    [Test]
    public async Task ADerivedTypeLookup_AllocatesNothing()
    {
        var table = Table();
        var integerType = NetEvolve.Schemas.XPath.Types.XdmTypes.Get("int");
        var found = false;

        AllocationAssert.NoAllocation(
            () => found = table.TryGet(ArithmeticOperator.Add, integerType, integerType, out _),
            warmups: 50
        );

        await Assert.That(found).IsTrue();
    }
}
