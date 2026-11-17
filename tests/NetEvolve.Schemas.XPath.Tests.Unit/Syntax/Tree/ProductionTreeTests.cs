namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Tree;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Syntax.Tree;

public class ProductionTreeTests
{
    // XML Path Language (XPath) 2.0 (Second Edition), A.1 EBNF, productions [1] to [70].
    private static readonly string[] _grammarProductions =
    [
        "XPath",
        "Expr",
        "ExprSingle",
        "ForExpr",
        "SimpleForClause",
        "QuantifiedExpr",
        "IfExpr",
        "OrExpr",
        "AndExpr",
        "ComparisonExpr",
        "RangeExpr",
        "AdditiveExpr",
        "MultiplicativeExpr",
        "UnionExpr",
        "IntersectExceptExpr",
        "InstanceofExpr",
        "TreatExpr",
        "CastableExpr",
        "CastExpr",
        "UnaryExpr",
        "ValueExpr",
        "GeneralComp",
        "ValueComp",
        "NodeComp",
        "PathExpr",
        "RelativePathExpr",
        "StepExpr",
        "AxisStep",
        "ForwardStep",
        "ForwardAxis",
        "AbbrevForwardStep",
        "ReverseStep",
        "ReverseAxis",
        "AbbrevReverseStep",
        "NodeTest",
        "NameTest",
        "Wildcard",
        "FilterExpr",
        "PredicateList",
        "Predicate",
        "PrimaryExpr",
        "Literal",
        "NumericLiteral",
        "VarRef",
        "VarName",
        "ParenthesizedExpr",
        "ContextItemExpr",
        "FunctionCall",
        "SingleType",
        "SeqType",
        "OccurrenceIndicator",
        "ItemType",
        "AtomicType",
        "KindTest",
        "AnyKindTest",
        "DocumentTest",
        "TextTest",
        "CommentTest",
        "PITest",
        "AttributeTest",
        "AttribNameOrWildcard",
        "SchemaAttributeTest",
        "AttributeDeclaration",
        "ElementTest",
        "ElementNameOrWildcard",
        "SchemaElementTest",
        "ElementDeclaration",
        "AttributeName",
        "ElementName",
        "TypeName",
    ];

    private static Dictionary<string, (Type Expected, Func<TreeFactory, SyntaxNode> Build)> Trees()
    {
        var trees = new Dictionary<string, (Type Expected, Func<TreeFactory, SyntaxNode> Build)>(
            StringComparer.Ordinal
        );
        AddExpressionTrees(trees);
        AddTestAndTypeTrees(trees);
        return trees;
    }

    private static void AddExpressionTrees(
        Dictionary<string, (Type Expected, Func<TreeFactory, SyntaxNode> Build)> trees
    )
    {
        trees["XPath"] = (typeof(SequenceExpression), f => f.Seq(f.Int(), f.Int()));
        trees["Expr"] = (typeof(SequenceExpression), f => f.Seq(f.Int("1"), f.Str("a"), f.Var()));
        trees["ExprSingle"] = (typeof(IfExpression), f => f.If());
        trees["ForExpr"] = (typeof(ForExpression), f => f.For(f.Bind("i")));
        trees["SimpleForClause"] = (typeof(ForExpression), f => f.For(f.Bind("i"), f.Bind("j")));
        trees["QuantifiedExpr"] = (typeof(QuantifiedExpression), f => f.Quantified(Quantifier.Every));
        trees["IfExpr"] = (typeof(IfExpression), f => f.If());
        trees["OrExpr"] = (typeof(LogicalExpression), f => f.Logical(LogicalOperator.Or));
        trees["AndExpr"] = (typeof(LogicalExpression), f => f.Logical(LogicalOperator.And));
        trees["ComparisonExpr"] = (typeof(ComparisonExpression), f => f.Compare(ComparisonOperator.ValueLessThan));
        trees["RangeExpr"] = (typeof(RangeExpression), f => f.Range());
        trees["AdditiveExpr"] = (typeof(ArithmeticExpression), f => f.Arith(ArithmeticOperator.Subtract));
        trees["MultiplicativeExpr"] = (typeof(ArithmeticExpression), f => f.Arith(ArithmeticOperator.IntegerDivide));
        trees["UnionExpr"] = (typeof(SetExpression), f => f.Set(SetOperator.Union));
        trees["IntersectExceptExpr"] = (typeof(SetExpression), f => f.Set(SetOperator.Except));
        trees["InstanceofExpr"] = (typeof(InstanceOfExpression), f => f.InstanceOf());
        trees["TreatExpr"] = (typeof(TreatExpression), f => f.Treat());
        trees["CastableExpr"] = (typeof(CastableExpression), f => f.Castable());
        trees["CastExpr"] = (typeof(CastExpression), f => f.Cast());
        trees["UnaryExpr"] = (typeof(UnaryExpression), f => f.Unary(UnaryOperator.Plus));
        trees["ValueExpr"] = (typeof(PathExpression), f => f.Path(PathRoot.Relative, f.Step(Axis.Child)));
        trees["GeneralComp"] = (typeof(ComparisonExpression), f => f.Compare(ComparisonOperator.GeneralNotEqual));
        trees["ValueComp"] = (typeof(ComparisonExpression), f => f.Compare(ComparisonOperator.ValueGreaterThanOrEqual));
        trees["NodeComp"] = (typeof(ComparisonExpression), f => f.Compare(ComparisonOperator.NodePrecedes));
        trees["PathExpr"] = (typeof(PathExpression), f => f.Path(PathRoot.RootDescendants, f.Step(Axis.Child)));
        trees["RelativePathExpr"] = (typeof(PathExpression), f =>
            f.Path(PathRoot.Relative, f.Step(Axis.Child), f.Step(Axis.Descendant))
        );
        trees["StepExpr"] = (typeof(FilterExpression), f => f.Filter(f.Call("f")));
        trees["AxisStep"] = (typeof(AxisStep), f => f.Step(Axis.Child, f.Name(), f.Int()));
        trees["ForwardStep"] = (typeof(AxisStep), f => f.Step(Axis.FollowingSibling));
        trees["ForwardAxis"] = (typeof(AxisStep), f => f.Step(Axis.Namespace));
        trees["AbbrevForwardStep"] = (typeof(AxisStep), f => f.Step(Axis.Attribute, f.Name("a")));
        trees["ReverseStep"] = (typeof(AxisStep), f => f.Step(Axis.PrecedingSibling));
        trees["ReverseAxis"] = (typeof(AxisStep), f => f.Step(Axis.AncestorOrSelf));
        trees["AbbrevReverseStep"] = (typeof(AxisStep), f => f.Step(Axis.Parent, f.AnyKind()));
    }

    private static void AddTestAndTypeTrees(
        Dictionary<string, (Type Expected, Func<TreeFactory, SyntaxNode> Build)> trees
    )
    {
        trees["NodeTest"] = (typeof(NameTest), f => f.Name());
        trees["NameTest"] = (typeof(NameTest), f => f.Name("a", "p"));
        trees["Wildcard"] = (typeof(NameTest), f => f.Wildcard(NameTestKind.AnyNamespace));
        trees["FilterExpr"] = (typeof(FilterExpression), f => f.Filter(f.Var(), f.Int()));
        trees["PredicateList"] = (typeof(FilterExpression), f => f.Filter(f.Var(), f.Int("1"), f.Int("2")));
        trees["Predicate"] = (typeof(AxisStep), f => f.Step(Axis.Child, f.Name(), f.Var("p")));
        trees["PrimaryExpr"] = (typeof(ParenthesizedExpression), f => f.Paren(f.Int()));
        trees["Literal"] = (typeof(StringLiteral), f => f.Str("lit"));
        trees["NumericLiteral"] = (typeof(DoubleLiteral), f => f.Dbl());
        trees["VarRef"] = (typeof(VariableReference), f => f.Var("x"));
        trees["VarName"] = (typeof(QualifiedName), f => f.Q("x", "p"));
        trees["ParenthesizedExpr"] = (typeof(ParenthesizedExpression), f => f.Paren());
        trees["ContextItemExpr"] = (typeof(ContextItemExpression), f => f.Dot());
        trees["FunctionCall"] = (typeof(FunctionCall), f => f.Call("f", f.Int(), f.Int()));
        trees["SingleType"] = (typeof(SingleType), f => f.Single(true));
        trees["SeqType"] = (typeof(SequenceType), f => f.SeqType(null));
        trees["OccurrenceIndicator"] = (typeof(SequenceType), f => f.SeqType(f.Atomic(), Occurrence.OneOrMore));
        trees["ItemType"] = (typeof(AnyItemType), f => f.Item());
        trees["AtomicType"] = (typeof(AtomicType), f => f.Atomic());
        trees["KindTest"] = (typeof(KindItemType), f => f.KindItem(f.Text()));
        trees["AnyKindTest"] = (typeof(AnyKindTest), f => f.AnyKind());
        trees["DocumentTest"] = (typeof(DocumentTest), f => f.DocTest(f.SchemaElem()));
        trees["TextTest"] = (typeof(TextTest), f => f.Text());
        trees["CommentTest"] = (typeof(CommentTest), f => f.Comment());
        trees["PITest"] = (typeof(ProcessingInstructionTest), f => f.PI("target"));
        trees["AttributeTest"] = (typeof(AttributeTest), f => f.AttrTest(f.Q("a"), f.Q("t", "xs")));
        trees["AttribNameOrWildcard"] = (typeof(AttributeTest), f => f.AttrTest());
        trees["SchemaAttributeTest"] = (typeof(SchemaAttributeTest), f => f.SchemaAttr());
        trees["AttributeDeclaration"] = (typeof(SchemaAttributeTest), f => f.SchemaAttr());
        trees["ElementTest"] = (typeof(ElementTest), f => f.ElemTest(f.Q("e"), f.Q("t"), true));
        trees["ElementNameOrWildcard"] = (typeof(ElementTest), f => f.ElemTest());
        trees["SchemaElementTest"] = (typeof(SchemaElementTest), f => f.SchemaElem());
        trees["ElementDeclaration"] = (typeof(SchemaElementTest), f => f.SchemaElem());
        trees["AttributeName"] = (typeof(QualifiedName), f => f.Q("a"));
        trees["ElementName"] = (typeof(QualifiedName), f => f.Q("e"));
        trees["TypeName"] = (typeof(QualifiedName), f => f.Q("t", "xs"));
    }

    public static IEnumerable<string> Productions() => _grammarProductions;

    [Test]
    public async Task Productions_ListHasSeventyEntriesWithoutDuplicates()
    {
        await Assert.That(_grammarProductions.Length).IsEqualTo(70);
        await Assert.That(new HashSet<string>(_grammarProductions, StringComparer.Ordinal).Count).IsEqualTo(70);
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

        await Assert
            .That(factory.Compare(ComparisonOperator.NodeFollows).Operator)
            .IsEqualTo(ComparisonOperator.NodeFollows);
        await Assert.That(factory.Quantified(Quantifier.Some).Quantifier).IsEqualTo(Quantifier.Some);
        await Assert.That(factory.Path(PathRoot.Root).Steps).IsEmpty();
        await Assert.That(factory.Wildcard(NameTestKind.AnyLocalName).Prefix).IsEqualTo("p");
        await Assert.That(factory.Wildcard(NameTestKind.AnyLocalName).LocalName).IsNull();
    }
}
