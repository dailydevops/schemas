namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Visitor over the syntax tree with one method per node kind that returns a result; every method returns the default value unless overridden.</summary>
/// <typeparam name="TResult">The result type.</typeparam>
internal abstract class SyntaxVisitor<TResult>
{
    /// <summary>Visits a <see cref="QualifiedName"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitQualifiedName(QualifiedName node) => default;

    /// <summary>Visits a <see cref="SequenceExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitSequenceExpression(SequenceExpression node) => default;

    /// <summary>Visits a <see cref="VariableBinding"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitVariableBinding(VariableBinding node) => default;

    /// <summary>Visits a <see cref="ForExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitForExpression(ForExpression node) => default;

    /// <summary>Visits a <see cref="QuantifiedExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitQuantifiedExpression(QuantifiedExpression node) => default;

    /// <summary>Visits a <see cref="IfExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitIfExpression(IfExpression node) => default;

    /// <summary>Visits a <see cref="LogicalExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitLogicalExpression(LogicalExpression node) => default;

    /// <summary>Visits a <see cref="ComparisonExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitComparisonExpression(ComparisonExpression node) => default;

    /// <summary>Visits a <see cref="RangeExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitRangeExpression(RangeExpression node) => default;

    /// <summary>Visits a <see cref="ArithmeticExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitArithmeticExpression(ArithmeticExpression node) => default;

    /// <summary>Visits a <see cref="SetExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitSetExpression(SetExpression node) => default;

    /// <summary>Visits a <see cref="SequenceType"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitSequenceType(SequenceType node) => default;

    /// <summary>Visits a <see cref="SingleType"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitSingleType(SingleType node) => default;

    /// <summary>Visits a <see cref="InstanceOfExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitInstanceOfExpression(InstanceOfExpression node) => default;

    /// <summary>Visits a <see cref="TreatExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitTreatExpression(TreatExpression node) => default;

    /// <summary>Visits a <see cref="CastableExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitCastableExpression(CastableExpression node) => default;

    /// <summary>Visits a <see cref="CastExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitCastExpression(CastExpression node) => default;

    /// <summary>Visits a <see cref="UnaryExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitUnaryExpression(UnaryExpression node) => default;

    /// <summary>Visits a <see cref="PathExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitPathExpression(PathExpression node) => default;

    /// <summary>Visits a <see cref="AxisStep"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitAxisStep(AxisStep node) => default;

    /// <summary>Visits a <see cref="FilterExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitFilterExpression(FilterExpression node) => default;

    /// <summary>Visits a <see cref="IntegerLiteral"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitIntegerLiteral(IntegerLiteral node) => default;

    /// <summary>Visits a <see cref="DecimalLiteral"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitDecimalLiteral(DecimalLiteral node) => default;

    /// <summary>Visits a <see cref="DoubleLiteral"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitDoubleLiteral(DoubleLiteral node) => default;

    /// <summary>Visits a <see cref="StringLiteral"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitStringLiteral(StringLiteral node) => default;

    /// <summary>Visits a <see cref="VariableReference"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitVariableReference(VariableReference node) => default;

    /// <summary>Visits a <see cref="ParenthesizedExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitParenthesizedExpression(ParenthesizedExpression node) => default;

    /// <summary>Visits a <see cref="ContextItemExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitContextItemExpression(ContextItemExpression node) => default;

    /// <summary>Visits a <see cref="FunctionCall"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitFunctionCall(FunctionCall node) => default;

    /// <summary>Visits a <see cref="NameTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitNameTest(NameTest node) => default;

    /// <summary>Visits a <see cref="AnyKindTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitAnyKindTest(AnyKindTest node) => default;

    /// <summary>Visits a <see cref="DocumentTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitDocumentTest(DocumentTest node) => default;

    /// <summary>Visits a <see cref="TextTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitTextTest(TextTest node) => default;

    /// <summary>Visits a <see cref="CommentTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitCommentTest(CommentTest node) => default;

    /// <summary>Visits a <see cref="ProcessingInstructionTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitProcessingInstructionTest(ProcessingInstructionTest node) => default;

    /// <summary>Visits a <see cref="AttributeTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitAttributeTest(AttributeTest node) => default;

    /// <summary>Visits a <see cref="SchemaAttributeTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitSchemaAttributeTest(SchemaAttributeTest node) => default;

    /// <summary>Visits a <see cref="ElementTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitElementTest(ElementTest node) => default;

    /// <summary>Visits a <see cref="SchemaElementTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitSchemaElementTest(SchemaElementTest node) => default;

    /// <summary>Visits a <see cref="AtomicType"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitAtomicType(AtomicType node) => default;

    /// <summary>Visits a <see cref="AnyItemType"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitAnyItemType(AnyItemType node) => default;

    /// <summary>Visits a <see cref="KindItemType"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitKindItemType(KindItemType node) => default;

    /// <summary>Visits a <see cref="LetExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitLetExpression(LetExpression node) => default;

    /// <summary>Visits a <see cref="SimpleMapExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitSimpleMapExpression(SimpleMapExpression node) => default;

    /// <summary>Visits a <see cref="StringConcatExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitStringConcatExpression(StringConcatExpression node) => default;

    /// <summary>Visits a <see cref="ArgumentPlaceholder"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitArgumentPlaceholder(ArgumentPlaceholder node) => default;

    /// <summary>Visits a <see cref="NamespaceNodeTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitNamespaceNodeTest(NamespaceNodeTest node) => default;

    /// <summary>Visits a <see cref="AnyFunctionTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitAnyFunctionTest(AnyFunctionTest node) => default;

    /// <summary>Visits a <see cref="NamedFunctionReference"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitNamedFunctionReference(NamedFunctionReference node) => default;

    /// <summary>Visits a <see cref="DynamicFunctionCall"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitDynamicFunctionCall(DynamicFunctionCall node) => default;

    /// <summary>Visits a <see cref="FunctionParameter"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitFunctionParameter(FunctionParameter node) => default;

    /// <summary>Visits a <see cref="InlineFunctionExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitInlineFunctionExpression(InlineFunctionExpression node) => default;

    /// <summary>Visits a <see cref="TypedFunctionTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitTypedFunctionTest(TypedFunctionTest node) => default;

    /// <summary>Visits a <see cref="ParenthesizedItemType"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitParenthesizedItemType(ParenthesizedItemType node) => default;

    /// <summary>Visits a <see cref="MapEntry"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitMapEntry(MapEntry node) => default;

    /// <summary>Visits a <see cref="MapConstructor"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitMapConstructor(MapConstructor node) => default;

    /// <summary>Visits a <see cref="SquareArrayConstructor"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitSquareArrayConstructor(SquareArrayConstructor node) => default;

    /// <summary>Visits a <see cref="CurlyArrayConstructor"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitCurlyArrayConstructor(CurlyArrayConstructor node) => default;

    /// <summary>Visits a <see cref="LookupExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitLookupExpression(LookupExpression node) => default;

    /// <summary>Visits a <see cref="ArrowExpression"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitArrowExpression(ArrowExpression node) => default;

    /// <summary>Visits a <see cref="AnyMapTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitAnyMapTest(AnyMapTest node) => default;

    /// <summary>Visits a <see cref="TypedMapTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitTypedMapTest(TypedMapTest node) => default;

    /// <summary>Visits a <see cref="AnyArrayTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitAnyArrayTest(AnyArrayTest node) => default;

    /// <summary>Visits a <see cref="TypedArrayTest"/>.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The result.</returns>
    public virtual TResult? VisitTypedArrayTest(TypedArrayTest node) => default;
}
