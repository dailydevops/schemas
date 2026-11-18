namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;

using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ComparisonFixture;

/// <summary>
/// The hot-path rule of #123 for a general comparison of an <c>xs:untypedAtomic</c> operand, such as an atomized attribute value,
/// with a number: the cast, including the collapse of surrounding whitespace, allocates no managed memory in steady state (#636).
/// </summary>
public class GeneralComparisonAllocationTests
{
    [Test]
    [Arguments("u: 2.5 ", "e:2.5")]
    [Arguments("u:\t2.5\n", "e:2.5")]
    [Arguments("u: 2.5 ", "f:2.5")]
    [Arguments("u: 42 ", "i:42")]
    public async Task UntypedOperandWithWhitespace_ComparedWithANumber_AllocatesNothing(string left, string right)
    {
        var node = new GeneralComparisonNode(
            ComparisonOperator.GeneralEqual,
            new ConstantOperand(One(left)),
            new ConstantOperand(One(right)),
            Table()
        );
        using var context = new XPathEvaluationContext();
        var result = XdmSequence.Empty;

        AllocationAssert.NoAllocation(() => result = node.Evaluate(context), warmups: 50);

        await Assert.That(Show(result)).IsEqualTo("true");
    }
}
