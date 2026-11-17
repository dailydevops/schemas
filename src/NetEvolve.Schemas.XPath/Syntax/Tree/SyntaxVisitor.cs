namespace NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Visitor over the syntax tree with one method per node kind; every method does nothing unless overridden.</summary>
internal abstract class SyntaxVisitor
{
    /// <summary>Visits a <see cref="QualifiedName"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitQualifiedName(QualifiedName node) { }

    /// <summary>Visits a <see cref="SequenceExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitSequenceExpression(SequenceExpression node) { }

    /// <summary>Visits a <see cref="VariableBinding"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitVariableBinding(VariableBinding node) { }

    /// <summary>Visits a <see cref="ForExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitForExpression(ForExpression node) { }

    /// <summary>Visits a <see cref="QuantifiedExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitQuantifiedExpression(QuantifiedExpression node) { }

    /// <summary>Visits a <see cref="IfExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitIfExpression(IfExpression node) { }

    /// <summary>Visits a <see cref="LogicalExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitLogicalExpression(LogicalExpression node) { }

    /// <summary>Visits a <see cref="ComparisonExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitComparisonExpression(ComparisonExpression node) { }

    /// <summary>Visits a <see cref="RangeExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitRangeExpression(RangeExpression node) { }

    /// <summary>Visits a <see cref="ArithmeticExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitArithmeticExpression(ArithmeticExpression node) { }

    /// <summary>Visits a <see cref="SetExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitSetExpression(SetExpression node) { }

    /// <summary>Visits a <see cref="SequenceType"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitSequenceType(SequenceType node) { }

    /// <summary>Visits a <see cref="SingleType"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitSingleType(SingleType node) { }

    /// <summary>Visits a <see cref="InstanceOfExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitInstanceOfExpression(InstanceOfExpression node) { }

    /// <summary>Visits a <see cref="TreatExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitTreatExpression(TreatExpression node) { }

    /// <summary>Visits a <see cref="CastableExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitCastableExpression(CastableExpression node) { }

    /// <summary>Visits a <see cref="CastExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitCastExpression(CastExpression node) { }

    /// <summary>Visits a <see cref="UnaryExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitUnaryExpression(UnaryExpression node) { }

    /// <summary>Visits a <see cref="PathExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitPathExpression(PathExpression node) { }

    /// <summary>Visits a <see cref="AxisStep"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitAxisStep(AxisStep node) { }

    /// <summary>Visits a <see cref="FilterExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitFilterExpression(FilterExpression node) { }

    /// <summary>Visits a <see cref="IntegerLiteral"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitIntegerLiteral(IntegerLiteral node) { }

    /// <summary>Visits a <see cref="DecimalLiteral"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitDecimalLiteral(DecimalLiteral node) { }

    /// <summary>Visits a <see cref="DoubleLiteral"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitDoubleLiteral(DoubleLiteral node) { }

    /// <summary>Visits a <see cref="StringLiteral"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitStringLiteral(StringLiteral node) { }

    /// <summary>Visits a <see cref="VariableReference"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitVariableReference(VariableReference node) { }

    /// <summary>Visits a <see cref="ParenthesizedExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitParenthesizedExpression(ParenthesizedExpression node) { }

    /// <summary>Visits a <see cref="ContextItemExpression"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitContextItemExpression(ContextItemExpression node) { }

    /// <summary>Visits a <see cref="FunctionCall"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitFunctionCall(FunctionCall node) { }

    /// <summary>Visits a <see cref="NameTest"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitNameTest(NameTest node) { }

    /// <summary>Visits a <see cref="AnyKindTest"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitAnyKindTest(AnyKindTest node) { }

    /// <summary>Visits a <see cref="DocumentTest"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitDocumentTest(DocumentTest node) { }

    /// <summary>Visits a <see cref="TextTest"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitTextTest(TextTest node) { }

    /// <summary>Visits a <see cref="CommentTest"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitCommentTest(CommentTest node) { }

    /// <summary>Visits a <see cref="ProcessingInstructionTest"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitProcessingInstructionTest(ProcessingInstructionTest node) { }

    /// <summary>Visits a <see cref="AttributeTest"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitAttributeTest(AttributeTest node) { }

    /// <summary>Visits a <see cref="SchemaAttributeTest"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitSchemaAttributeTest(SchemaAttributeTest node) { }

    /// <summary>Visits a <see cref="ElementTest"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitElementTest(ElementTest node) { }

    /// <summary>Visits a <see cref="SchemaElementTest"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitSchemaElementTest(SchemaElementTest node) { }

    /// <summary>Visits a <see cref="AtomicType"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitAtomicType(AtomicType node) { }

    /// <summary>Visits a <see cref="AnyItemType"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitAnyItemType(AnyItemType node) { }

    /// <summary>Visits a <see cref="KindItemType"/>.</summary>
    /// <param name="node">The node.</param>
    public virtual void VisitKindItemType(KindItemType node) { }
}
