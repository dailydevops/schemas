namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Tree;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Syntax.Tree;

public class ProductionTreeTests
{
    // XML Path Language (XPath) 2.0 (Second Edition), A.1 EBNF, productions [1] to [70].
    private static readonly string[] _grammarProductions =
    [
        "XPath", "Expr", "ExprSingle", "ForExpr", "SimpleForClause", "QuantifiedExpr", "IfExpr", "OrExpr", "AndExpr",
        "ComparisonExpr", "RangeExpr", "AdditiveExpr", "MultiplicativeExpr", "UnionExpr", "IntersectExceptExpr",
        "InstanceofExpr", "TreatExpr", "CastableExpr", "CastExpr", "UnaryExpr", "ValueExpr", "GeneralComp", "ValueComp",
        "NodeComp", "PathExpr", "RelativePathExpr", "StepExpr", "AxisStep", "ForwardStep", "ForwardAxis",
        "AbbrevForwardStep", "ReverseStep", "ReverseAxis", "AbbrevReverseStep", "NodeTest", "NameTest", "Wildcard",
        "FilterExpr", "PredicateList", "Predicate", "PrimaryExpr", "Literal", "NumericLiteral", "VarRef", "VarName",
        "ParenthesizedExpr", "ContextItemExpr", "FunctionCall", "SingleType", "SeqType", "OccurrenceIndicator",
        "ItemType", "AtomicType", "KindTest", "AnyKindTest", "DocumentTest", "TextTest", "CommentTest", "PITest",
        "AttributeTest", "AttribNameOrWildcard", "SchemaAttributeTest", "AttributeDeclaration", "ElementTest",
        "ElementNameOrWildcard", "SchemaElementTest", "ElementDeclaration", "AttributeName", "ElementName", "TypeName",
    ];

    private static Dictionary<string, (Type Expected, Func<TreeFactory, SyntaxNode> Build)> Trees() =>
        new(StringComparer.Ordinal)
        {
            ["XPath"] = (typeof(SequenceExpression), f => f.Seq(f.Int(), f.Int())),
            ["Expr"] = (typeof(SequenceExpression), f => f.Seq(f.Int("1"), f.Str("a"), f.Var())),
            ["ExprSingle"] = (typeof(IfExpression), f => f.If()),
            ["ForExpr"] = (typeof(ForExpression), f => f.For(f.Bind("i"))),
            ["SimpleForClause"] = (typeof(ForExpression), f => f.For(f.Bind("i"), f.Bind("j"))),
            ["QuantifiedExpr"] = (typeof(QuantifiedExpression), f => f.Quantified(Quantifier.Every)),
            ["IfExpr"] = (typeof(IfExpression), f => f.If()),
            ["OrExpr"] = (typeof(LogicalExpression), f => f.Logical(LogicalOperator.Or)),
            ["AndExpr"] = (typeof(LogicalExpression), f => f.Logical(LogicalOperator.And)),
            ["ComparisonExpr"] = (typeof(ComparisonExpression), f => f.Compare(ComparisonOperator.ValueLessThan)),
            ["RangeExpr"] = (typeof(RangeExpression), f => f.Range()),
            ["AdditiveExpr"] = (typeof(ArithmeticExpression), f => f.Arith(ArithmeticOperator.Subtract)),
            ["MultiplicativeExpr"] = (typeof(ArithmeticExpression), f => f.Arith(ArithmeticOperator.IntegerDivide)),
            ["UnionExpr"] = (typeof(SetExpression), f => f.Set(SetOperator.Union)),
            ["IntersectExceptExpr"] = (typeof(SetExpression), f => f.Set(SetOperator.Except)),
            ["InstanceofExpr"] = (typeof(InstanceOfExpression), f => f.InstanceOf()),
            ["TreatExpr"] = (typeof(TreatExpression), f => f.Treat()),
            ["CastableExpr"] = (typeof(CastableExpression), f => f.Castable()),
            ["CastExpr"] = (typeof(CastExpression), f => f.Cast()),
            ["UnaryExpr"] = (typeof(UnaryExpression), f => f.Unary(UnaryOperator.Plus)),
            ["ValueExpr"] = (typeof(PathExpression), f => f.Path(PathRoot.Relative, f.Step(Axis.Child))),
            ["GeneralComp"] = (typeof(ComparisonExpression), f => f.Compare(ComparisonOperator.GeneralNotEqual)),
            ["ValueComp"] = (typeof(ComparisonExpression), f => f.Compare(ComparisonOperator.ValueGreaterThanOrEqual)),
            ["NodeComp"] = (typeof(ComparisonExpression), f => f.Compare(ComparisonOperator.NodePrecedes)),
            ["PathExpr"] = (typeof(PathExpression), f => f.Path(PathRoot.RootDescendants, f.Step(Axis.Child))),
            ["RelativePathExpr"] = (
                typeof(PathExpression),
                f => f.Path(PathRoot.Relative, f.Step(Axis.Child), f.Step(Axis.Descendant))
            ),
            ["StepExpr"] = (typeof(FilterExpression), f => f.Filter(f.Call("f"))),
            ["AxisStep"] = (typeof(AxisStep), f => f.Step(Axis.Child, f.Name(), f.Int())),
            ["ForwardStep"] = (typeof(AxisStep), f => f.Step(Axis.FollowingSibling)),
            ["ForwardAxis"] = (typeof(AxisStep), f => f.Step(Axis.Namespace)),
            ["AbbrevForwardStep"] = (typeof(AxisStep), f => f.Step(Axis.Attribute, f.Name("a"))),
            ["ReverseStep"] = (typeof(AxisStep), f => f.Step(Axis.PrecedingSibling)),
            ["ReverseAxis"] = (typeof(AxisStep), f => f.Step(Axis.AncestorOrSelf)),
            ["AbbrevReverseStep"] = (typeof(AxisStep), f => f.Step(Axis.Parent, f.AnyKind())),
            ["NodeTest"] = (typeof(NameTest), f => f.Name()),
            ["NameTest"] = (typeof(NameTest), f => f.Name("a", "p")),
            ["Wildcard"] = (typeof(NameTest), f => f.Wildcard(NameTestKind.AnyNamespace)),
            ["FilterExpr"] = (typeof(FilterExpression), f => f.Filter(f.Var(), f.Int())),
            ["PredicateList"] = (typeof(FilterExpression), f => f.Filter(f.Var(), f.Int("1"), f.Int("2"))),
            ["Predicate"] = (typeof(AxisStep), f => f.Step(Axis.Child, f.Name(), f.Var("p"))),
            ["PrimaryExpr"] = (typeof(ParenthesizedExpression), f => f.Paren(f.Int())),
            ["Literal"] = (typeof(StringLiteral), f => f.Str("lit")),
            ["NumericLiteral"] = (typeof(DoubleLiteral), f => f.Dbl()),
            ["VarRef"] = (typeof(VariableReference), f => f.Var("x")),
            ["VarName"] = (typeof(QualifiedName), f => f.Q("x", "p")),
            ["ParenthesizedExpr"] = (typeof(ParenthesizedExpression), f => f.Paren()),
            ["ContextItemExpr"] = (typeof(ContextItemExpression), f => f.Dot()),
            ["FunctionCall"] = (typeof(FunctionCall), f => f.Call("f", f.Int(), f.Int())),
            ["SingleType"] = (typeof(SingleType), f => f.Single(true)),
            ["SeqType"] = (typeof(SequenceType), f => f.SeqType(null)),
            ["OccurrenceIndicator"] = (typeof(SequenceType), f => f.SeqType(f.Atomic(), Occurrence.OneOrMore)),
            ["ItemType"] = (typeof(AnyItemType), f => f.Item()),
            ["AtomicType"] = (typeof(AtomicType), f => f.Atomic()),
            ["KindTest"] = (typeof(KindItemType), f => f.KindItem(f.Text())),
            ["AnyKindTest"] = (typeof(AnyKindTest), f => f.AnyKind()),
            ["DocumentTest"] = (typeof(DocumentTest), f => f.DocTest(f.SchemaElem())),
            ["TextTest"] = (typeof(TextTest), f => f.Text()),
            ["CommentTest"] = (typeof(CommentTest), f => f.Comment()),
            ["PITest"] = (typeof(ProcessingInstructionTest), f => f.PI("target")),
            ["AttributeTest"] = (typeof(AttributeTest), f => f.AttrTest(f.Q("a"), f.Q("t", "xs"))),
            ["AttribNameOrWildcard"] = (typeof(AttributeTest), f => f.AttrTest()),
            ["SchemaAttributeTest"] = (typeof(SchemaAttributeTest), f => f.SchemaAttr()),
            ["AttributeDeclaration"] = (typeof(SchemaAttributeTest), f => f.SchemaAttr()),
            ["ElementTest"] = (typeof(ElementTest), f => f.ElemTest(f.Q("e"), f.Q("t"), true)),
            ["ElementNameOrWildcard"] = (typeof(ElementTest), f => f.ElemTest()),
            ["SchemaElementTest"] = (typeof(SchemaElementTest), f => f.SchemaElem()),
            ["ElementDeclaration"] = (typeof(SchemaElementTest), f => f.SchemaElem()),
            ["AttributeName"] = (typeof(QualifiedName), f => f.Q("a")),
            ["ElementName"] = (typeof(QualifiedName), f => f.Q("e")),
            ["TypeName"] = (typeof(QualifiedName), f => f.Q("t", "xs")),
        };

    public static IEnumerable<string> Productions() => _grammarProductions;

    [Test]
    public async Task Productions_ListHasSeventyEntriesWithoutDuplicates()
    {
        await Assert.That(_grammarProductions).HasCount(70);
        await Assert.That(new HashSet<string>(_grammarProductions, StringComparer.Ordinal)).HasCount(70);
    }

    [Test]
    public async Task Productions_EveryGrammarProductionHasAHandBuiltTree()
    {
        var trees = Trees();
        var missing = new List<string>();
        foreach (var production in _grammarProductions)
        {
            if (!trees.ContainsKey(production))
            {
                missing.Add(production);
            }
        }

        var unknown = new List<string>();
        foreach (var key in trees.Keys)
        {
            if (Array.IndexOf(_grammarProductions, key) < 0)
            {
                unknown.Add(key);
            }
        }

        await Assert.That(missing).IsEmpty();
        await Assert.That(unknown).IsEmpty();
    }

    [Test]
    [MethodDataSource(nameof(Productions))]
    public async Task Production_HandBuiltTree_HasExpectedRootAndIsComplete(string production)
    {
        var (expected, build) = Trees()[production];
        var factory = new TreeFactory();

        var root = build(factory);

        await Assert.That(root.GetType()).IsEqualTo(expected);
        foreach (var node in TreeWalker.Descendants(root))
        {
            await Assert.That(factory.Built.ContainsKey(node)).IsTrue();
        }
    }

    [Test]
    public async Task Operators_KeepTheirValues()
    {
        var factory = new TreeFactory();

        await Assert.That(factory.Compare(ComparisonOperator.NodeFollows).Operator).IsEqualTo(ComparisonOperator.NodeFollows);
        await Assert.That(factory.Quantified(Quantifier.Some).Quantifier).IsEqualTo(Quantifier.Some);
        await Assert.That(factory.Path(PathRoot.Root).Steps).IsEmpty();
        await Assert.That(factory.Wildcard(NameTestKind.AnyLocalName).Prefix).IsEqualTo("p");
        await Assert.That(factory.Wildcard(NameTestKind.AnyLocalName).LocalName).IsNull();
    }
}
