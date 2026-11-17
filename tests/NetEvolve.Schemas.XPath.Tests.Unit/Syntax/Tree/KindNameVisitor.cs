namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Tree;

using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Returns the node kind name for every visited node.</summary>
internal sealed class KindNameVisitor : SyntaxVisitor<string>
{
    public override string? VisitQualifiedName(QualifiedName node) => nameof(QualifiedName);

    public override string? VisitSequenceExpression(SequenceExpression node) => nameof(SequenceExpression);

    public override string? VisitVariableBinding(VariableBinding node) => nameof(VariableBinding);

    public override string? VisitForExpression(ForExpression node) => nameof(ForExpression);

    public override string? VisitQuantifiedExpression(QuantifiedExpression node) => nameof(QuantifiedExpression);

    public override string? VisitIfExpression(IfExpression node) => nameof(IfExpression);

    public override string? VisitLogicalExpression(LogicalExpression node) => nameof(LogicalExpression);

    public override string? VisitComparisonExpression(ComparisonExpression node) => nameof(ComparisonExpression);

    public override string? VisitRangeExpression(RangeExpression node) => nameof(RangeExpression);

    public override string? VisitArithmeticExpression(ArithmeticExpression node) => nameof(ArithmeticExpression);

    public override string? VisitSetExpression(SetExpression node) => nameof(SetExpression);

    public override string? VisitSequenceType(SequenceType node) => nameof(SequenceType);

    public override string? VisitSingleType(SingleType node) => nameof(SingleType);

    public override string? VisitInstanceOfExpression(InstanceOfExpression node) => nameof(InstanceOfExpression);

    public override string? VisitTreatExpression(TreatExpression node) => nameof(TreatExpression);

    public override string? VisitCastableExpression(CastableExpression node) => nameof(CastableExpression);

    public override string? VisitCastExpression(CastExpression node) => nameof(CastExpression);

    public override string? VisitUnaryExpression(UnaryExpression node) => nameof(UnaryExpression);

    public override string? VisitPathExpression(PathExpression node) => nameof(PathExpression);

    public override string? VisitAxisStep(AxisStep node) => nameof(AxisStep);

    public override string? VisitFilterExpression(FilterExpression node) => nameof(FilterExpression);

    public override string? VisitIntegerLiteral(IntegerLiteral node) => nameof(IntegerLiteral);

    public override string? VisitDecimalLiteral(DecimalLiteral node) => nameof(DecimalLiteral);

    public override string? VisitDoubleLiteral(DoubleLiteral node) => nameof(DoubleLiteral);

    public override string? VisitStringLiteral(StringLiteral node) => nameof(StringLiteral);

    public override string? VisitVariableReference(VariableReference node) => nameof(VariableReference);

    public override string? VisitParenthesizedExpression(ParenthesizedExpression node) =>
        nameof(ParenthesizedExpression);

    public override string? VisitContextItemExpression(ContextItemExpression node) => nameof(ContextItemExpression);

    public override string? VisitFunctionCall(FunctionCall node) => nameof(FunctionCall);

    public override string? VisitNameTest(NameTest node) => nameof(NameTest);

    public override string? VisitAnyKindTest(AnyKindTest node) => nameof(AnyKindTest);

    public override string? VisitDocumentTest(DocumentTest node) => nameof(DocumentTest);

    public override string? VisitTextTest(TextTest node) => nameof(TextTest);

    public override string? VisitCommentTest(CommentTest node) => nameof(CommentTest);

    public override string? VisitProcessingInstructionTest(ProcessingInstructionTest node) =>
        nameof(ProcessingInstructionTest);

    public override string? VisitAttributeTest(AttributeTest node) => nameof(AttributeTest);

    public override string? VisitSchemaAttributeTest(SchemaAttributeTest node) => nameof(SchemaAttributeTest);

    public override string? VisitElementTest(ElementTest node) => nameof(ElementTest);

    public override string? VisitSchemaElementTest(SchemaElementTest node) => nameof(SchemaElementTest);

    public override string? VisitAtomicType(AtomicType node) => nameof(AtomicType);

    public override string? VisitAnyItemType(AnyItemType node) => nameof(AnyItemType);

    public override string? VisitKindItemType(KindItemType node) => nameof(KindItemType);
}
