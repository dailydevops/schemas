namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Tree;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Builds hand-made syntax trees and remembers the location every node was built with.</summary>
internal sealed class TreeFactory
{
    private int _next;

    /// <summary>Gets the location each node was built with, keyed by node identity.</summary>
    public Dictionary<SyntaxNode, SourceSpan> Built { get; } = new(ReferenceEqualityComparer.Instance);

    public T Make<T>(Func<SourceSpan, T> create)
        where T : SyntaxNode
    {
        var span = new SourceSpan(_next, 3);
        _next += 5;
        var node = create(span);
        Built[node] = span;
        return node;
    }

    public QualifiedName Q(string local, string? prefix = null) => Make(s => new QualifiedName(s, prefix, local));

    public IntegerLiteral Int(string lexeme = "1") => Make(s => new IntegerLiteral(s, lexeme));

    public DecimalLiteral Dec(string lexeme = "1.5") => Make(s => new DecimalLiteral(s, lexeme));

    public DoubleLiteral Dbl(string lexeme = "1e3") => Make(s => new DoubleLiteral(s, lexeme));

    public StringLiteral Str(string value = "a") => Make(s => new StringLiteral(s, value));

    public VariableReference Var(string name = "x") => Make(s => new VariableReference(s, Q(name)));

    public ParenthesizedExpression Paren(Expression? inner = null) => Make(s => new ParenthesizedExpression(s, inner));

    public ContextItemExpression Dot() => Make(s => new ContextItemExpression(s));

    public FunctionCall Call(string name, params Expression[] args) => Make(s => new FunctionCall(s, Q(name), args));

    public SequenceExpression Seq(params Expression[] items) => Make(s => new SequenceExpression(s, items));

    public VariableBinding Bind(string name = "v", Expression? source = null) =>
        Make(s => new VariableBinding(s, Q(name), source ?? Int()));

    public ForExpression For(params VariableBinding[] bindings) => Make(s => new ForExpression(s, bindings, Var("v")));

    public QuantifiedExpression Quantified(Quantifier quantifier) =>
        Make(s => new QuantifiedExpression(s, quantifier, new[] { Bind("a"), Bind("b") }, Var("a")));

    public IfExpression If() => Make(s => new IfExpression(s, Var("c"), Int("1"), Int("2")));

    public LogicalExpression Logical(LogicalOperator op) => Make(s => new LogicalExpression(s, op, Var("a"), Var("b")));

    public ComparisonExpression Compare(ComparisonOperator op) =>
        Make(s => new ComparisonExpression(s, op, Var("a"), Var("b")));

    public RangeExpression Range() => Make(s => new RangeExpression(s, Int("1"), Int("5")));

    public ArithmeticExpression Arith(ArithmeticOperator op) =>
        Make(s => new ArithmeticExpression(s, op, Int("1"), Int("2")));

    public SetExpression Set(SetOperator op) => Make(s => new SetExpression(s, op, Var("a"), Var("b")));

    public UnaryExpression Unary(UnaryOperator op) => Make(s => new UnaryExpression(s, op, Int()));

    public SequenceType SeqType(ItemType? item, Occurrence occurrence = Occurrence.ExactlyOne) =>
        Make(s => new SequenceType(s, item, occurrence));

    public SingleType Single(bool allowsEmpty = false) => Make(s => new SingleType(s, Q("integer", "xs"), allowsEmpty));

    public AtomicType Atomic() => Make(s => new AtomicType(s, Q("string", "xs")));

    public AnyItemType Item() => Make(s => new AnyItemType(s));

    public KindItemType KindItem(KindTest test) => Make(s => new KindItemType(s, test));

    public NameTest Name(string local = "a", string? prefix = null) =>
        Make(s => new NameTest(s, NameTestKind.QualifiedName, prefix, local));

    public NameTest Wildcard(NameTestKind kind) =>
        Make(s =>
            kind switch
            {
                NameTestKind.AnyName => new NameTest(s, kind, null, null),
                NameTestKind.AnyLocalName => new NameTest(s, kind, "p", null),
                _ => new NameTest(s, kind, null, "l"),
            }
        );

    public AnyKindTest AnyKind() => Make(s => new AnyKindTest(s));

    public TextTest Text() => Make(s => new TextTest(s));

    public CommentTest Comment() => Make(s => new CommentTest(s));

    public ProcessingInstructionTest PI(string? target = null) => Make(s => new ProcessingInstructionTest(s, target));

    public AttributeTest AttrTest(QualifiedName? name = null, QualifiedName? type = null) =>
        Make(s => new AttributeTest(s, name, type));

    public SchemaAttributeTest SchemaAttr() => Make(s => new SchemaAttributeTest(s, Q("a")));

    public ElementTest ElemTest(QualifiedName? name = null, QualifiedName? type = null, bool nillable = false) =>
        Make(s => new ElementTest(s, name, type, nillable));

    public SchemaElementTest SchemaElem() => Make(s => new SchemaElementTest(s, Q("e")));

    public DocumentTest DocTest(KindTest? element = null) => Make(s => new DocumentTest(s, element));

    public AxisStep Step(Axis axis, NodeTest? test = null, params Expression[] predicates) =>
        Make(s => new AxisStep(s, axis, test ?? Name(), predicates));

    public FilterExpression Filter(Expression? primary = null, params Expression[] predicates) =>
        Make(s => new FilterExpression(s, primary ?? Var("x"), predicates));

    public PathExpression Path(PathRoot root, params Expression[] steps) =>
        Make(s => new PathExpression(s, root, steps));

    public InstanceOfExpression InstanceOf() =>
        Make(s => new InstanceOfExpression(s, Var("x"), SeqType(Atomic(), Occurrence.ZeroOrMore)));

    public TreatExpression Treat() =>
        Make(s => new TreatExpression(s, Var("x"), SeqType(Item(), Occurrence.OneOrMore)));

    public CastableExpression Castable() => Make(s => new CastableExpression(s, Var("x"), Single(true)));

    public CastExpression Cast() => Make(s => new CastExpression(s, Var("x"), Single()));

    /// <summary>Builds one tree that contains every node kind at least once.</summary>
    public SyntaxNode AllKinds() =>
        Seq(
            For(Bind("i"), Bind("j")),
            Quantified(Quantifier.Some),
            Quantified(Quantifier.Every),
            If(),
            Logical(LogicalOperator.Or),
            Compare(ComparisonOperator.GeneralEqual),
            Range(),
            Arith(ArithmeticOperator.Add),
            Set(SetOperator.Union),
            InstanceOf(),
            Make(s => new TreatExpression(s, Var("x"), SeqType(null))),
            Treat(),
            Castable(),
            Cast(),
            Unary(UnaryOperator.Minus),
            Dec(),
            Dbl(),
            Str(),
            Paren(),
            Paren(Dot()),
            Call("f", Int()),
            Path(
                PathRoot.Root,
                Step(Axis.Child, Name("a", "p"), Int()),
                Step(Axis.Attribute, AttrTest(Q("n"), Q("t"))),
                Step(Axis.Child, SchemaAttr()),
                Step(Axis.Child, ElemTest(Q("e"), Q("t"), true)),
                Step(Axis.Child, SchemaElem()),
                Step(Axis.Child, DocTest(ElemTest())),
                Step(Axis.Child, Text()),
                Step(Axis.Child, Comment()),
                Step(Axis.Child, PI("t")),
                Step(Axis.Parent, AnyKind()),
                Filter(Var("x"), Int("1"))
            ),
            Make(s => new InstanceOfExpression(s, Var("x"), SeqType(KindItem(AnyKind()))))
        );
}
