namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Parser;

using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Hand-built expected trees; locations are ignored by the structural comparison.</summary>
internal static class N
{
    private static SourceSpan S => default;

    public static QualifiedName Q(string local, string? prefix = null) => new(S, prefix, local);

    public static IntegerLiteral Int(string lexeme) => new(S, lexeme);

    public static DecimalLiteral Dec(string lexeme) => new(S, lexeme);

    public static DoubleLiteral Dbl(string lexeme) => new(S, lexeme);

    public static StringLiteral Str(string value) => new(S, value);

    public static VariableReference Var(string local, string? prefix = null) => new(S, Q(local, prefix));

    public static ContextItemExpression Dot() => new(S);

    public static ParenthesizedExpression Paren(Expression? inner = null) => new(S, inner);

    public static SequenceExpression Seq(params Expression[] items) => new(S, items);

    public static FunctionCall Call(string local, string? prefix = null, params Expression[] args) =>
        new(S, Q(local, prefix), args);

    public static VariableBinding Bind(string name, Expression source) => new(S, Q(name), source);

    public static ForExpression For(Expression ret, params VariableBinding[] bindings) => new(S, bindings, ret);

    public static QuantifiedExpression Quant(Quantifier q, Expression satisfies, params VariableBinding[] bindings) =>
        new(S, q, bindings, satisfies);

    public static IfExpression If(Expression c, Expression t, Expression e) => new(S, c, t, e);

    public static LogicalExpression Or(Expression l, Expression r) => new(S, LogicalOperator.Or, l, r);

    public static LogicalExpression And(Expression l, Expression r) => new(S, LogicalOperator.And, l, r);

    public static ComparisonExpression Cmp(ComparisonOperator op, Expression l, Expression r) => new(S, op, l, r);

    public static RangeExpression Range(Expression from, Expression to) => new(S, from, to);

    public static ArithmeticExpression Arith(ArithmeticOperator op, Expression l, Expression r) => new(S, op, l, r);

    public static SetExpression Set(SetOperator op, Expression l, Expression r) => new(S, op, l, r);

    public static UnaryExpression Unary(UnaryOperator op, Expression operand) => new(S, op, operand);

    public static InstanceOfExpression InstanceOf(Expression e, SequenceType t) => new(S, e, t);

    public static TreatExpression Treat(Expression e, SequenceType t) => new(S, e, t);

    public static CastableExpression Castable(Expression e, SingleType t) => new(S, e, t);

    public static CastExpression Cast(Expression e, SingleType t) => new(S, e, t);

    public static SingleType Single(string local, string? prefix = null, bool empty = false) =>
        new(S, Q(local, prefix), empty);

    public static SequenceType SeqType(ItemType? item, Occurrence o = Occurrence.ExactlyOne) => new(S, item, o);

    public static AtomicType Atomic(string local, string? prefix = null) => new(S, Q(local, prefix));

    public static AnyItemType Item() => new(S);

    public static KindItemType KindItem(KindTest t) => new(S, t);

    public static NameTest Nm(string local, string? prefix = null) => new(S, NameTestKind.QualifiedName, prefix, local);

    public static NameTest AnyName() => new(S, NameTestKind.AnyName, null, null);

    public static NameTest AnyLocal(string prefix) => new(S, NameTestKind.AnyLocalName, prefix, null);

    public static NameTest AnyNs(string local) => new(S, NameTestKind.AnyNamespace, null, local);

    public static AxisStep Step(Axis axis, NodeTest test, params Expression[] predicates) =>
        new(S, axis, test, predicates);

    public static AxisStep Child(string local, string? prefix = null, params Expression[] predicates) =>
        Step(Axis.Child, Nm(local, prefix), predicates);

    public static AxisStep DescendantOrSelf() => Step(Axis.DescendantOrSelf, new AnyKindTest(S));

    public static FilterExpression Filter(Expression primary, params Expression[] predicates) =>
        new(S, primary, predicates);

    public static PathExpression Path(PathRoot root, params Expression[] steps) => new(S, root, steps);

    public static AnyKindTest AnyKind() => new(S);

    public static TextTest Text() => new(S);

    public static CommentTest Comment() => new(S);

    public static ProcessingInstructionTest PI(string? target = null) => new(S, target);

    public static DocumentTest Doc(KindTest? element = null) => new(S, element);

    public static ElementTest Elem(QualifiedName? name = null, QualifiedName? type = null, bool nillable = false) =>
        new(S, name, type, nillable);

    public static AttributeTest Attr(QualifiedName? name = null, QualifiedName? type = null) => new(S, name, type);

    public static SchemaElementTest SchemaElem(string local) => new(S, Q(local));

    public static SchemaAttributeTest SchemaAttr(string local) => new(S, Q(local));
}
