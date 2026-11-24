namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Tree;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Counts every visited node by kind and walks into the children of each node.</summary>
internal sealed class CountingVisitor : SyntaxVisitor
{
    public Dictionary<Type, int> Counts { get; } = [];

    private void Count(SyntaxNode node)
    {
        Counts[node.GetType()] = Counts.TryGetValue(node.GetType(), out var count) ? count + 1 : 1;
        foreach (var child in node.Children)
        {
            child.Accept(this);
        }
    }

    public override void VisitQualifiedName(QualifiedName node) => Count(node);

    public override void VisitSequenceExpression(SequenceExpression node) => Count(node);

    public override void VisitVariableBinding(VariableBinding node) => Count(node);

    public override void VisitForExpression(ForExpression node) => Count(node);

    public override void VisitQuantifiedExpression(QuantifiedExpression node) => Count(node);

    public override void VisitIfExpression(IfExpression node) => Count(node);

    public override void VisitLogicalExpression(LogicalExpression node) => Count(node);

    public override void VisitComparisonExpression(ComparisonExpression node) => Count(node);

    public override void VisitRangeExpression(RangeExpression node) => Count(node);

    public override void VisitArithmeticExpression(ArithmeticExpression node) => Count(node);

    public override void VisitSetExpression(SetExpression node) => Count(node);

    public override void VisitSequenceType(SequenceType node) => Count(node);

    public override void VisitSingleType(SingleType node) => Count(node);

    public override void VisitInstanceOfExpression(InstanceOfExpression node) => Count(node);

    public override void VisitTreatExpression(TreatExpression node) => Count(node);

    public override void VisitCastableExpression(CastableExpression node) => Count(node);

    public override void VisitCastExpression(CastExpression node) => Count(node);

    public override void VisitUnaryExpression(UnaryExpression node) => Count(node);

    public override void VisitPathExpression(PathExpression node) => Count(node);

    public override void VisitAxisStep(AxisStep node) => Count(node);

    public override void VisitFilterExpression(FilterExpression node) => Count(node);

    public override void VisitIntegerLiteral(IntegerLiteral node) => Count(node);

    public override void VisitDecimalLiteral(DecimalLiteral node) => Count(node);

    public override void VisitDoubleLiteral(DoubleLiteral node) => Count(node);

    public override void VisitStringLiteral(StringLiteral node) => Count(node);

    public override void VisitVariableReference(VariableReference node) => Count(node);

    public override void VisitParenthesizedExpression(ParenthesizedExpression node) => Count(node);

    public override void VisitContextItemExpression(ContextItemExpression node) => Count(node);

    public override void VisitFunctionCall(FunctionCall node) => Count(node);

    public override void VisitNameTest(NameTest node) => Count(node);

    public override void VisitAnyKindTest(AnyKindTest node) => Count(node);

    public override void VisitDocumentTest(DocumentTest node) => Count(node);

    public override void VisitTextTest(TextTest node) => Count(node);

    public override void VisitCommentTest(CommentTest node) => Count(node);

    public override void VisitProcessingInstructionTest(ProcessingInstructionTest node) => Count(node);

    public override void VisitAttributeTest(AttributeTest node) => Count(node);

    public override void VisitSchemaAttributeTest(SchemaAttributeTest node) => Count(node);

    public override void VisitElementTest(ElementTest node) => Count(node);

    public override void VisitSchemaElementTest(SchemaElementTest node) => Count(node);

    public override void VisitAtomicType(AtomicType node) => Count(node);

    public override void VisitAnyItemType(AnyItemType node) => Count(node);

    public override void VisitKindItemType(KindItemType node) => Count(node);

    public override void VisitLetExpression(LetExpression node) => Count(node);

    public override void VisitSimpleMapExpression(SimpleMapExpression node) => Count(node);

    public override void VisitStringConcatExpression(StringConcatExpression node) => Count(node);

    public override void VisitArgumentPlaceholder(ArgumentPlaceholder node) => Count(node);

    public override void VisitNamespaceNodeTest(NamespaceNodeTest node) => Count(node);

    public override void VisitAnyFunctionTest(AnyFunctionTest node) => Count(node);

    public override void VisitNamedFunctionReference(NamedFunctionReference node) => Count(node);

    public override void VisitDynamicFunctionCall(DynamicFunctionCall node) => Count(node);

    public override void VisitFunctionParameter(FunctionParameter node) => Count(node);

    public override void VisitInlineFunctionExpression(InlineFunctionExpression node) => Count(node);

    public override void VisitTypedFunctionTest(TypedFunctionTest node) => Count(node);

    public override void VisitParenthesizedItemType(ParenthesizedItemType node) => Count(node);
}
