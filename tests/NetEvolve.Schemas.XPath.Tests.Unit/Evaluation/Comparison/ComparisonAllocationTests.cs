namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;

using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ComparisonFixture;

/// <summary>The hot-path rule of #123: a comparison allocates no managed memory in steady state.</summary>
public class ComparisonAllocationTests
{
    private static ExecutableNode Create(ComparisonOperator op, XdmSequence left, XdmSequence right) =>
        op >= ComparisonOperator.ValueEqual
            ? new ValueComparisonNode(op, new ConstantOperand(left), new ConstantOperand(right), Table())
            : new GeneralComparisonNode(op, new ConstantOperand(left), new ConstantOperand(right), Table());

    private static async Task AssertNoAllocation(string left, string op, string right, string expected)
    {
        var node = Create(Operator(op), One(left), One(right));
        using var context = new XPathEvaluationContext();
        var result = XdmSequence.Empty;

        AllocationAssert.NoAllocation(() => result = node.Evaluate(context), warmups: 50);

        await Assert.That(Show(result)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("i:2", "eq", "i:2", "true")]
    [Arguments("i:2", "lt", "i:3", "true")]
    [Arguments("i:2", "ge", "i:3", "false")]
    [Arguments("i:2", "ne", "i:3", "true")]
    public async Task IntegerValueComparison_AllocatesNothing(string left, string op, string right, string expected) =>
        await AssertNoAllocation(left, op, right, expected);

    [Test]
    [Arguments("e:2", "eq", "e:2", "true")]
    [Arguments("e:2", "lt", "i:3", "true")]
    [Arguments("e:NaN", "ne", "e:NaN", "true")]
    [Arguments("f:2", "gt", "f:1", "true")]
    [Arguments("f:2", "le", "i:3", "true")]
    public async Task FloatingPointValueComparison_AllocatesNothing(
        string left,
        string op,
        string right,
        string expected
    ) => await AssertNoAllocation(left, op, right, expected);

    [Test]
    [Arguments("s:abc", "eq", "s:abc", "true")]
    [Arguments("s:abc", "lt", "s:abd", "true")]
    [Arguments("u:abc", "eq", "s:abc", "true")]
    [Arguments("a:http://a", "eq", "s:http://a", "true")]
    [Arguments("b:false", "lt", "b:true", "true")]
    public async Task StringAndBooleanValueComparison_AllocatesNothing(
        string left,
        string op,
        string right,
        string expected
    ) => await AssertNoAllocation(left, op, right, expected);

    [Test]
    [Arguments("i:2", "=", "i:2", "true")]
    [Arguments("e:2", "<", "i:3", "true")]
    [Arguments("s:abc", "!=", "s:abd", "true")]
    [Arguments("b:true", "=", "b:true", "true")]
    [Arguments("u:abc", "=", "u:abc", "true")]
    [Arguments("u:abc", "=", "s:abc", "true")]
    public async Task GeneralComparisonOfSingletons_AllocatesNothing(
        string left,
        string op,
        string right,
        string expected
    ) => await AssertNoAllocation(left, op, right, expected);

    [Test]
    public async Task GeneralComparisonOfSeveralNumbers_AllocatesNothing()
    {
        var node = new GeneralComparisonNode(
            ComparisonOperator.GeneralEqual,
            new ConstantOperand(Many("i:1", "i:2", "i:3")),
            new ConstantOperand(Many("i:7", "i:8", "i:3")),
            Table()
        );
        using var context = new XPathEvaluationContext();
        var result = XdmSequence.Empty;

        AllocationAssert.NoAllocation(() => result = node.Evaluate(context), warmups: 50);

        await Assert.That(Show(result)).IsEqualTo("true");
    }

    [Test]
    [Arguments("is")]
    [Arguments("<<")]
    [Arguments(">>")]
    public async Task NodeComparison_AllocatesNothing(string op)
    {
        var tree = new CountingTree(3);
        var node = new NodeComparisonNode(
            Operator(op),
            new ConstantOperand(XdmSequence.Create(XdmItem.FromNode(tree.At(1)))),
            new ConstantOperand(XdmSequence.Create(XdmItem.FromNode(tree.At(2))))
        );
        using var context = new XPathEvaluationContext();
        var result = XdmSequence.Empty;

        AllocationAssert.NoAllocation(() => result = node.Evaluate(context), warmups: 50);

        await Assert.That(result.Count).IsEqualTo(1);
    }

    [Test]
    public async Task TheCodepointComparison_AllocatesNothing()
    {
        var high = "abc" + (char)0xFFFF;
        var supplementary = "abc" + char.ConvertFromUtf32(0x10000);
        var order = 0;

        AllocationAssert.NoAllocation(() => order = CodepointComparison.Compare(high, supplementary), warmups: 50);

        await Assert.That(order).IsLessThan(0);
    }
}
