namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Flow;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Flow;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The guards at the boundaries of the flow evaluation: arguments that cannot be right fail at once, not later in an evaluation.</summary>
public class FlowGuardTests
{
    private static readonly XPathQName Name = new(string.Empty, "v");
    private static readonly SourceSpan Span = new(0, 1);
    private static readonly ExecutableNode Constant = new ConstantNode(XdmSequence.Empty);

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
    public async Task Register_WithANullRegistry_Throws() =>
        await Assert.That(Catch(() => FlowLowerings.Register(null!))).IsTypeOf<ArgumentNullException>();

    [Test]
    public async Task Register_TwiceInTheSameRegistry_Throws()
    {
        var registry = new LoweringRegistry();
        FlowLowerings.Register(registry);

        await Assert.That(Catch(() => FlowLowerings.Register(registry))).IsTypeOf<InvalidOperationException>();
    }

    [Test]
    public async Task LogicalNode_WithANullOperand_Throws()
    {
        await Assert
            .That(Catch(() => _ = new LogicalNode(LogicalOperator.And, null!, Constant)))
            .IsTypeOf<ArgumentNullException>();
        await Assert
            .That(Catch(() => _ = new LogicalNode(LogicalOperator.Or, Constant, null!)))
            .IsTypeOf<ArgumentNullException>();
    }

    [Test]
    [Arguments(-1)]
    [Arguments(2)]
    public async Task LogicalNode_WithAnUndefinedOperator_Throws(int value) =>
        await Assert
            .That(Catch(() => _ = new LogicalNode((LogicalOperator)value, Constant, Constant)))
            .IsTypeOf<ArgumentOutOfRangeException>();

    [Test]
    public async Task IfNode_WithANullOperand_Throws()
    {
        await Assert.That(Catch(() => _ = new IfNode(null!, Constant, Constant))).IsTypeOf<ArgumentNullException>();
        await Assert.That(Catch(() => _ = new IfNode(Constant, null!, Constant))).IsTypeOf<ArgumentNullException>();
        await Assert.That(Catch(() => _ = new IfNode(Constant, Constant, null!))).IsTypeOf<ArgumentNullException>();
    }

    [Test]
    public async Task RangeNode_WithANullOperand_Throws()
    {
        await Assert.That(Catch(() => _ = new RangeNode(null!, Constant, Span))).IsTypeOf<ArgumentNullException>();
        await Assert.That(Catch(() => _ = new RangeNode(Constant, null!, Span))).IsTypeOf<ArgumentNullException>();
    }

    [Test]
    public async Task SequenceNode_WithANullItemList_OrANullItem_Throws()
    {
        await Assert.That(Catch(() => _ = new SequenceNode(null!, Span))).IsTypeOf<ArgumentNullException>();
        await Assert.That(Catch(() => _ = new SequenceNode([Constant, null!], Span))).IsTypeOf<ArgumentNullException>();
    }

    [Test]
    public async Task ForNode_WithANullOperand_OrAnEmptyName_Throws()
    {
        await Assert.That(Catch(() => _ = new ForNode(Name, null!, Constant, Span))).IsTypeOf<ArgumentNullException>();
        await Assert.That(Catch(() => _ = new ForNode(Name, Constant, null!, Span))).IsTypeOf<ArgumentNullException>();
        await Assert
            .That(Catch(() => _ = new ForNode(default, Constant, Constant, Span)))
            .IsTypeOf<ArgumentException>();
    }

    [Test]
    public async Task QuantifiedNode_WithANullOperand_AnEmptyName_OrAnUndefinedQuantifier_Throws()
    {
        await Assert
            .That(Catch(() => _ = new QuantifiedNode(Quantifier.Some, Name, null!, Constant)))
            .IsTypeOf<ArgumentNullException>();
        await Assert
            .That(Catch(() => _ = new QuantifiedNode(Quantifier.Some, Name, Constant, null!)))
            .IsTypeOf<ArgumentNullException>();
        await Assert
            .That(Catch(() => _ = new QuantifiedNode(Quantifier.Every, default, Constant, Constant)))
            .IsTypeOf<ArgumentException>();
        await Assert
            .That(Catch(() => _ = new QuantifiedNode((Quantifier)2, Name, Constant, Constant)))
            .IsTypeOf<ArgumentOutOfRangeException>();
        await Assert
            .That(Catch(() => _ = new QuantifiedNode((Quantifier)(-1), Name, Constant, Constant)))
            .IsTypeOf<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task VariableNode_WithAnEmptyName_Throws() =>
        await Assert.That(Catch(() => _ = new VariableNode(default))).IsTypeOf<ArgumentException>();

    [Test]
    public async Task Evaluate_WithANullContext_Throws()
    {
        var nodes = new ExecutableNode[]
        {
            new LogicalNode(LogicalOperator.And, Constant, Constant),
            new IfNode(Constant, Constant, Constant),
            new RangeNode(Constant, Constant, Span),
            new SequenceNode([Constant], Span),
            new ForNode(Name, Constant, Constant, Span),
            new QuantifiedNode(Quantifier.Some, Name, Constant, Constant),
            new VariableNode(Name),
            Constant,
        };

        foreach (var node in nodes)
        {
            await Assert.That(Catch(() => node.Evaluate(null!))).IsTypeOf<ArgumentNullException>();
        }
    }
}
