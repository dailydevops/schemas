namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Temporal;

using System;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Evaluation.Temporal;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Pins the decision that an operator entry can read the dynamic context (#300 D1): the implicit timezone reaches the temporal
/// operators through a second kind of entry in the existing tables, and the entries without the context stay as they were.
/// </summary>
public class ContextualEntryTests
{
    private static readonly XdmItem Text = XdmItem.FromString("a");

    [Test]
    public async Task AContextualArithmeticEntry_ReceivesTheDynamicContextOfTheEvaluation()
    {
        var table = new ArithmeticOperatorTable();
        table.RegisterContextual(
            ArithmeticOperator.Add,
            XdmTypeId.XsString,
            XdmTypeId.XsString,
            static (in XdmItem _, in XdmItem _, XPathEvaluationContext context) =>
                XdmItem.FromInteger(context.ImplicitTimezoneMinutes)
        );
        var node = new ArithmeticNode(
            ArithmeticOperator.Add,
            new TemporalFixture.ConstantNode(XdmSequence.Create(Text)),
            new TemporalFixture.ConstantNode(XdmSequence.Create(Text)),
            table
        );

        using var west = new XPathEvaluationContext { ImplicitTimezoneMinutes = -300 };
        using var east = new XPathEvaluationContext { ImplicitTimezoneMinutes = 90 };

        await Assert.That(TemporalFixture.Describe(node.Evaluate(west))).IsEqualTo("XsInteger:-300");
        await Assert.That(TemporalFixture.Describe(node.Evaluate(east))).IsEqualTo("XsInteger:90");
    }

    [Test]
    public async Task AContextualComparisonEntry_ReceivesTheDynamicContext_ForTheValueAndTheGeneralComparison()
    {
        var table = new ComparisonOperatorTable();
        table.RegisterContextual(
            ComparisonRelation.Equal,
            XdmTypeId.XsString,
            XdmTypeId.XsString,
            static (ComparisonRelation _, in XdmItem _, in XdmItem _, XPathEvaluationContext context) =>
                context.ImplicitTimezoneMinutes > 0
        );
        var value = new ValueComparisonNode(
            ComparisonOperator.ValueEqual,
            new TemporalFixture.ConstantNode(XdmSequence.Create(Text)),
            new TemporalFixture.ConstantNode(XdmSequence.Create(Text)),
            table
        );
        var general = new GeneralComparisonNode(
            ComparisonOperator.GeneralEqual,
            new TemporalFixture.ConstantNode(XdmSequence.Create(Text)),
            new TemporalFixture.ConstantNode(XdmSequence.Create(Text)),
            table
        );

        using var west = new XPathEvaluationContext { ImplicitTimezoneMinutes = -60 };
        using var east = new XPathEvaluationContext { ImplicitTimezoneMinutes = 60 };

        await Assert.That(TemporalFixture.Describe(value.Evaluate(west))).IsEqualTo("Boolean:false");
        await Assert.That(TemporalFixture.Describe(value.Evaluate(east))).IsEqualTo("Boolean:true");
        await Assert.That(TemporalFixture.Describe(general.Evaluate(west))).IsEqualTo("Boolean:false");
        await Assert.That(TemporalFixture.Describe(general.Evaluate(east))).IsEqualTo("Boolean:true");
    }

    [Test]
    public async Task AnEntryWithoutTheContext_IsFoundByTheContextualLookup_AndCalledWithTheSameOperands()
    {
        var arithmetic = new ArithmeticOperatorTable();
        arithmetic.Register(
            ArithmeticOperator.Add,
            XdmTypeId.XsString,
            XdmTypeId.XsString,
            static (in XdmItem left, in XdmItem _) => left
        );
        var comparison = new ComparisonOperatorTable();
        comparison.Register(
            ComparisonRelation.NotEqual,
            XdmTypeId.XsString,
            XdmTypeId.XsString,
            static (ComparisonRelation _, in XdmItem _, in XdmItem _) => true
        );
        using var context = new XPathEvaluationContext();

        await Assert
            .That(
                arithmetic.TryGetContextual(ArithmeticOperator.Add, XdmTypeId.XsString, XdmTypeId.XsString, out var add)
            )
            .IsTrue();
        await Assert.That(add!(Text, Text, context)).IsEqualTo(Text);
        await Assert
            .That(
                comparison.TryGetContextual(
                    ComparisonRelation.NotEqual,
                    XdmTypeId.XsString,
                    XdmTypeId.XsString,
                    out var different
                )
            )
            .IsTrue();
        await Assert.That(different!(ComparisonRelation.NotEqual, Text, Text, context)).IsTrue();
    }

    [Test]
    public async Task AnEntryWithTheContext_IsNotFoundByTheLookupWithoutIt()
    {
        var arithmetic = new ArithmeticOperatorTable();
        arithmetic.RegisterContextual(
            ArithmeticOperator.Add,
            XdmTypeId.XsString,
            XdmTypeId.XsString,
            static (in XdmItem left, in XdmItem _, XPathEvaluationContext _) => left
        );
        var comparison = new ComparisonOperatorTable();
        comparison.RegisterContextual(
            ComparisonRelation.Equal,
            XdmTypeId.XsString,
            XdmTypeId.XsString,
            static (ComparisonRelation _, in XdmItem _, in XdmItem _, XPathEvaluationContext _) => true
        );

        await Assert
            .That(arithmetic.TryGet(ArithmeticOperator.Add, XdmTypeId.XsString, XdmTypeId.XsString, out var plain))
            .IsFalse();
        await Assert.That(plain).IsNull();
        await Assert
            .That(comparison.TryGet(ComparisonRelation.Equal, XdmTypeId.XsString, XdmTypeId.XsString, out var other))
            .IsFalse();
        await Assert.That(other).IsNull();
    }

    [Test]
    public async Task OneOperatorAndPairOfTypes_TakesOneEntryOfEitherKind()
    {
        var arithmetic = new ArithmeticOperatorTable();
        arithmetic.Register(
            ArithmeticOperator.Add,
            XdmTypeId.XsString,
            XdmTypeId.XsString,
            static (in XdmItem left, in XdmItem _) => left
        );
        var comparison = new ComparisonOperatorTable();
        comparison.Register(
            ComparisonRelation.Equal,
            XdmTypeId.XsString,
            XdmTypeId.XsString,
            static (ComparisonRelation _, in XdmItem _, in XdmItem _) => true
        );

        await Assert
            .That(() =>
                arithmetic.RegisterContextual(
                    ArithmeticOperator.Add,
                    XdmTypeId.XsString,
                    XdmTypeId.XsString,
                    static (in XdmItem left, in XdmItem _, XPathEvaluationContext _) => left
                )
            )
            .Throws<InvalidOperationException>();
        await Assert
            .That(() =>
                comparison.RegisterContextual(
                    ComparisonRelation.Equal,
                    XdmTypeId.XsString,
                    XdmTypeId.XsString,
                    static (ComparisonRelation _, in XdmItem _, in XdmItem _, XPathEvaluationContext _) => true
                )
            )
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task TheContextualRegistrationAndLookup_GuardTheirArguments()
    {
        var arithmetic = new ArithmeticOperatorTable();
        var comparison = new ComparisonOperatorTable();

        await Assert
            .That(() =>
                arithmetic.RegisterContextual(ArithmeticOperator.Add, XdmTypeId.XsString, XdmTypeId.XsString, null!)
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                comparison.RegisterContextual(ComparisonRelation.Equal, XdmTypeId.XsString, XdmTypeId.XsString, null!)
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                arithmetic.TryGetContextual((ArithmeticOperator)99, XdmTypeId.XsString, XdmTypeId.XsString, out _)
            )
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() =>
                comparison.TryGetContextual((ComparisonRelation)99, XdmTypeId.XsString, XdmTypeId.XsString, out _)
            )
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task TheTemporalImplementations_GuardTheContext()
    {
        var date = TemporalFixture.Run("xs:date('2000-01-01')").Result[0];

        await Assert.That(() => TemporalArithmetic.SubtractMoments(date, date, null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => TemporalComparison.CompareMoments(ComparisonRelation.Equal, date, date, null!))
            .Throws<ArgumentNullException>();
    }
}
