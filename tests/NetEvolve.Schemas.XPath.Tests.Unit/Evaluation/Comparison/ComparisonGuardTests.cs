namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;

using System;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ComparisonFixture;

/// <summary>The argument guards at the boundaries of the comparison nodes.</summary>
public class ComparisonGuardTests
{
    private static ConstantOperand Operand => new(One("i:1"));

    [Test]
    public async Task ValueComparisonNode_RejectsNullArguments()
    {
        await Assert
            .That(() => new ValueComparisonNode(ComparisonOperator.ValueEqual, null!, Operand, Table()))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new ValueComparisonNode(ComparisonOperator.ValueEqual, Operand, null!, Table()))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new ValueComparisonNode(ComparisonOperator.ValueEqual, Operand, Operand, null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task GeneralComparisonNode_RejectsNullArguments()
    {
        await Assert
            .That(() => new GeneralComparisonNode(ComparisonOperator.GeneralEqual, null!, Operand, Table()))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new GeneralComparisonNode(ComparisonOperator.GeneralEqual, Operand, null!, Table()))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new GeneralComparisonNode(ComparisonOperator.GeneralEqual, Operand, Operand, null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task NodeComparisonNode_RejectsNullArguments()
    {
        await Assert
            .That(() => new NodeComparisonNode(ComparisonOperator.NodeIs, null!, Operand))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => new NodeComparisonNode(ComparisonOperator.NodeIs, Operand, null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments((int)ComparisonOperator.GeneralEqual)]
    [Arguments((int)ComparisonOperator.NodeIs)]
    [Arguments(-1)]
    [Arguments(15)]
    public async Task ValueComparisonNode_RejectsAnOperatorOfAnotherKind(int op) =>
        await Assert
            .That(() => new ValueComparisonNode((ComparisonOperator)op, Operand, Operand, Table()))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    [Arguments((int)ComparisonOperator.ValueEqual)]
    [Arguments((int)ComparisonOperator.NodeIs)]
    [Arguments(-1)]
    [Arguments(15)]
    public async Task GeneralComparisonNode_RejectsAnOperatorOfAnotherKind(int op) =>
        await Assert
            .That(() => new GeneralComparisonNode((ComparisonOperator)op, Operand, Operand, Table()))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    [Arguments((int)ComparisonOperator.GeneralEqual)]
    [Arguments((int)ComparisonOperator.ValueEqual)]
    [Arguments(-1)]
    [Arguments(15)]
    public async Task NodeComparisonNode_RejectsAnOperatorOfAnotherKind(int op) =>
        await Assert
            .That(() => new NodeComparisonNode((ComparisonOperator)op, Operand, Operand))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Evaluate_RejectsANullContext()
    {
        ExecutableNode[] nodes =
        [
            new ValueComparisonNode(ComparisonOperator.ValueEqual, Operand, Operand, Table()),
            new GeneralComparisonNode(ComparisonOperator.GeneralEqual, Operand, Operand, Table()),
            new NodeComparisonNode(ComparisonOperator.NodeIs, Operand, Operand),
        ];

        foreach (var node in nodes)
        {
            await Assert.That(() => node.Evaluate(null!)).Throws<ArgumentNullException>();
        }
    }

    [Test]
    [Arguments(-1)]
    [Arguments(6)]
    public async Task Relations_RejectARelationThatIsNotDefined_WhenTestingAnOrder(int relation) =>
        await Assert
            .That(() => ComparisonRelations.Holds((ComparisonRelation)relation, 0))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Relations_TestEveryDefinedRelation_AgainstTheOrderOfTwoValues()
    {
        await Assert.That(ComparisonRelations.Holds(ComparisonRelation.Equal, 0)).IsTrue();
        await Assert.That(ComparisonRelations.Holds(ComparisonRelation.NotEqual, 0)).IsFalse();
        await Assert.That(ComparisonRelations.Holds(ComparisonRelation.LessThan, -1)).IsTrue();
        await Assert.That(ComparisonRelations.Holds(ComparisonRelation.LessThanOrEqual, 1)).IsFalse();
        await Assert.That(ComparisonRelations.Holds(ComparisonRelation.GreaterThan, 1)).IsTrue();
        await Assert.That(ComparisonRelations.Holds(ComparisonRelation.GreaterThanOrEqual, -1)).IsFalse();
    }

    [Test]
    public async Task Relations_RejectAnOperatorOfTheNodeKind()
    {
        await Assert
            .That(() => ComparisonRelations.Of(ComparisonOperator.NodeIs))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => ComparisonRelations.Of((ComparisonOperator)(-1))).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TheFamilies_CompareTheirOwnItems()
    {
        var number = XdmItem.FromInteger(1);
        var text = XdmItem.FromString("a");
        var flag = XdmItem.FromBoolean(true);

        await Assert.That(NumericComparison.Compare(ComparisonRelation.Equal, number, number)).IsTrue();
        await Assert.That(TextComparison.Compare(ComparisonRelation.Equal, text, text)).IsTrue();
        await Assert.That(BooleanComparison.Compare(ComparisonRelation.Equal, flag, flag)).IsTrue();
    }

    [Test]
    public async Task TheFamilies_RaiseXpty0004_ForAnItemOfAnotherFamily()
    {
        var number = XdmItem.FromInteger(1);
        var text = XdmItem.FromString("a");

        await Assert
            .That(Raised(() => NumericComparison.Compare(ComparisonRelation.Equal, text, number)))
            .IsEqualTo("XPTY0004");
        await Assert
            .That(Raised(() => TextComparison.Compare(ComparisonRelation.Equal, number, text)))
            .IsEqualTo("XPTY0004");
        await Assert
            .That(Raised(() => BooleanComparison.Compare(ComparisonRelation.Equal, number, number)))
            .IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task TheFamilies_RejectARelationThatIsNotDefined()
    {
        var number = XdmItem.FromInteger(1);
        var text = XdmItem.FromString("a");
        var flag = XdmItem.FromBoolean(true);

        await Assert
            .That(() => NumericComparison.Compare((ComparisonRelation)6, number, number))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => TextComparison.Compare((ComparisonRelation)(-1), text, text))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => BooleanComparison.Compare((ComparisonRelation)6, flag, flag))
            .Throws<ArgumentOutOfRangeException>();
    }

    private static string Raised(Func<bool> action)
    {
        try
        {
            _ = action();
            return string.Empty;
        }
        catch (NetEvolve.Schemas.XPath.Errors.XPathException exception)
        {
            return exception.Diagnostic.Code;
        }
    }
}
