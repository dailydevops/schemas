namespace NetEvolve.Schemas.XPath.Tests.Unit.Compatibility;

using System;
using NetEvolve.Schemas.XPath.Compatibility;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Tests.Unit.Functions.Numerics;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The guards at the boundaries of the compatibility nodes: arguments that cannot be right fail at once, not later in an evaluation.</summary>
public class CompatibilityGuardTests
{
    private static readonly ExecutableNode Constant = new ArithmeticFixture.ConstantNode(XdmSequence.Empty);
    private static readonly ArithmeticOperatorTable Arithmetic = ArithmeticFixture.Table();
    private static readonly ComparisonOperatorTable Comparison = ComparisonFixture.Table();

    private static Exception? Catch(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    [Test]
    public async Task ArithmeticNode_WithANullArgument_Throws()
    {
        await Assert
            .That(Catch(() => _ = new CompatibilityArithmeticNode(ArithmeticOperator.Add, null!, Constant, Arithmetic)))
            .IsTypeOf<ArgumentNullException>();
        await Assert
            .That(Catch(() => _ = new CompatibilityArithmeticNode(ArithmeticOperator.Add, Constant, null!, Arithmetic)))
            .IsTypeOf<ArgumentNullException>();
        await Assert
            .That(Catch(() => _ = new CompatibilityArithmeticNode(ArithmeticOperator.Add, Constant, Constant, null!)))
            .IsTypeOf<ArgumentNullException>();
    }

    [Test]
    public async Task ArithmeticNode_WithAnUndefinedOperator_Throws() =>
        await Assert
            .That(
                Catch(() => _ = new CompatibilityArithmeticNode((ArithmeticOperator)99, Constant, Constant, Arithmetic))
            )
            .IsTypeOf<ArgumentOutOfRangeException>();

    [Test]
    public async Task UnaryNode_WithANullArgumentOrAnUndefinedOperator_Throws()
    {
        await Assert
            .That(Catch(() => _ = new CompatibilityUnaryNode(UnaryOperator.Minus, null!, Arithmetic)))
            .IsTypeOf<ArgumentNullException>();
        await Assert
            .That(Catch(() => _ = new CompatibilityUnaryNode(UnaryOperator.Minus, Constant, null!)))
            .IsTypeOf<ArgumentNullException>();
        await Assert
            .That(Catch(() => _ = new CompatibilityUnaryNode((UnaryOperator)99, Constant, Arithmetic)))
            .IsTypeOf<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task GeneralComparisonNode_WithANullArgumentOrANonGeneralOperator_Throws()
    {
        await Assert
            .That(
                Catch(() =>
                    _ = new CompatibilityGeneralComparisonNode(
                        ComparisonOperator.GeneralEqual,
                        null!,
                        Constant,
                        Comparison
                    )
                )
            )
            .IsTypeOf<ArgumentNullException>();
        await Assert
            .That(
                Catch(() =>
                    _ = new CompatibilityGeneralComparisonNode(
                        ComparisonOperator.GeneralEqual,
                        Constant,
                        Constant,
                        null!
                    )
                )
            )
            .IsTypeOf<ArgumentNullException>();
        await Assert
            .That(
                Catch(() =>
                    _ = new CompatibilityGeneralComparisonNode(
                        ComparisonOperator.ValueEqual,
                        Constant,
                        Constant,
                        Comparison
                    )
                )
            )
            .IsTypeOf<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task CallNode_WithANullArgumentOrAWrongArity_Throws()
    {
        await Assert.That(NumericsHarness.TryResolve("string-length", 1, out var function)).IsTrue();

        await Assert.That(Catch(() => _ = new CompatibilityCallNode(null!, []))).IsTypeOf<ArgumentNullException>();
        await Assert
            .That(Catch(() => _ = new CompatibilityCallNode(function!, null!)))
            .IsTypeOf<ArgumentNullException>();
        await Assert
            .That(Catch(() => _ = new CompatibilityCallNode(function!, [Constant, null!])))
            .IsTypeOf<ArgumentOutOfRangeException>();
        await Assert
            .That(Catch(() => _ = new CompatibilityCallNode(function!, [null!])))
            .IsTypeOf<ArgumentNullException>();
    }
}
