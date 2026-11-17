namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Parser;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using static N;

/// <summary>Input text and the hand-built tree it must parse to, one block per grammar production.</summary>
internal static class ParserCases
{
    private static readonly Expression A = Child("a");
    private static readonly Expression B = Child("b");
    private static readonly Expression C = Child("c");

    /// <summary>Gets one case per production of XPath 2.0 appendix A.1, keyed by the input text.</summary>
    public static IReadOnlyDictionary<string, Expression> Productions { get; } =
        new Dictionary<string, Expression>(StringComparer.Ordinal)
        {
            // [2] Expr
            ["1,2,3"] = Seq(Int("1"), Int("2"), Int("3")),
            // [3] ForExpr, [4] SimpleForClause
            ["for $x in 1 return $x"] = For(Var("x"), Bind("x", Int("1"))),
            ["for $x in 1, $y in 2 return $x"] = For(Var("x"), Bind("x", Int("1")), Bind("y", Int("2"))),
            // [5] QuantifiedExpr
            ["some $x in 1 satisfies $x"] = Quant(Quantifier.Some, Var("x"), Bind("x", Int("1"))),
            ["every $x in 1, $y in 2 satisfies $x"] = Quant(
                Quantifier.Every,
                Var("x"),
                Bind("x", Int("1")),
                Bind("y", Int("2"))
            ),
            // [6] IfExpr
            ["if (a) then b else c"] = If(A, B, C),
            // [7] OrExpr, [8] AndExpr
            ["a or b"] = Or(A, B),
            ["a and b"] = And(A, B),
            // [9] ComparisonExpr, [23] GeneralComp, [24] ValueComp, [25] NodeComp
            ["a = b"] = Cmp(ComparisonOperator.GeneralEqual, A, B),
            ["a != b"] = Cmp(ComparisonOperator.GeneralNotEqual, A, B),
            ["a < b"] = Cmp(ComparisonOperator.GeneralLessThan, A, B),
            ["a <= b"] = Cmp(ComparisonOperator.GeneralLessThanOrEqual, A, B),
            ["a > b"] = Cmp(ComparisonOperator.GeneralGreaterThan, A, B),
            ["a >= b"] = Cmp(ComparisonOperator.GeneralGreaterThanOrEqual, A, B),
            ["a eq b"] = Cmp(ComparisonOperator.ValueEqual, A, B),
            ["a ne b"] = Cmp(ComparisonOperator.ValueNotEqual, A, B),
            ["a lt b"] = Cmp(ComparisonOperator.ValueLessThan, A, B),
            ["a le b"] = Cmp(ComparisonOperator.ValueLessThanOrEqual, A, B),
            ["a gt b"] = Cmp(ComparisonOperator.ValueGreaterThan, A, B),
            ["a ge b"] = Cmp(ComparisonOperator.ValueGreaterThanOrEqual, A, B),
            ["a is b"] = Cmp(ComparisonOperator.NodeIs, A, B),
            ["a << b"] = Cmp(ComparisonOperator.NodePrecedes, A, B),
            ["a >> b"] = Cmp(ComparisonOperator.NodeFollows, A, B),
            // [10] RangeExpr
            ["1 to 5"] = Range(Int("1"), Int("5")),
            // [11] AdditiveExpr, [12] MultiplicativeExpr
            ["1 + 2"] = Arith(ArithmeticOperator.Add, Int("1"), Int("2")),
            ["1 - 2"] = Arith(ArithmeticOperator.Subtract, Int("1"), Int("2")),
            ["1 * 2"] = Arith(ArithmeticOperator.Multiply, Int("1"), Int("2")),
            ["1 div 2"] = Arith(ArithmeticOperator.Divide, Int("1"), Int("2")),
            ["1 idiv 2"] = Arith(ArithmeticOperator.IntegerDivide, Int("1"), Int("2")),
            ["1 mod 2"] = Arith(ArithmeticOperator.Modulo, Int("1"), Int("2")),
            ["* * *"] = Arith(ArithmeticOperator.Multiply, Step(Axis.Child, AnyName()), Step(Axis.Child, AnyName())),
            // [13] UnionExpr, [14] IntersectExceptExpr
            ["a union b"] = Set(SetOperator.Union, A, B),
            ["a | b"] = Set(SetOperator.Union, A, B),
            ["a intersect b"] = Set(SetOperator.Intersect, A, B),
            ["a except b"] = Set(SetOperator.Except, A, B),
            // [15] InstanceofExpr, [16] TreatExpr, [17] CastableExpr, [18] CastExpr
            ["a instance of xs:integer"] = InstanceOf(A, SeqType(Atomic("integer", "xs"))),
            ["a instance of xs:integer?"] = InstanceOf(A, SeqType(Atomic("integer", "xs"), Occurrence.ZeroOrOne)),
            ["a treat as item()+"] = Treat(A, SeqType(Item(), Occurrence.OneOrMore)),
            ["a castable as xs:integer"] = Castable(A, Single("integer", "xs")),
            ["a castable as xs:integer?"] = Castable(A, Single("integer", "xs", true)),
            ["a cast as xs:integer"] = Cast(A, Single("integer", "xs")),
            ["a cast as integer?"] = Cast(A, Single("integer", null, true)),
            // [19] UnaryExpr
            ["-1"] = Unary(UnaryOperator.Minus, Int("1")),
            ["+1"] = Unary(UnaryOperator.Plus, Int("1")),
            ["- -1"] = Unary(UnaryOperator.Minus, Unary(UnaryOperator.Minus, Int("1"))),
            // [26] PathExpr, [27] RelativePathExpr
            ["/"] = Path(PathRoot.Root),
            ["/a"] = Path(PathRoot.Root, A),
            ["//a"] = Path(PathRoot.RootDescendants, A),
            ["a/b"] = Path(PathRoot.Relative, A, B),
            ["a//b"] = Path(PathRoot.Relative, A, DescendantOrSelf(), B),
            ["/a//b/c"] = Path(PathRoot.Root, A, DescendantOrSelf(), B, C),
            [".//a"] = Path(PathRoot.Relative, Dot(), DescendantOrSelf(), A),
            ["a/(b)"] = Path(PathRoot.Relative, A, Paren(B)),
            // [28] StepExpr, [29] AxisStep, [30] ForwardStep, [31] ForwardAxis
            ["a"] = A,
            ["child::a"] = Step(Axis.Child, Nm("a")),
            ["descendant::a"] = Step(Axis.Descendant, Nm("a")),
            ["attribute::a"] = Step(Axis.Attribute, Nm("a")),
            ["self::a"] = Step(Axis.Self, Nm("a")),
            ["descendant-or-self::a"] = Step(Axis.DescendantOrSelf, Nm("a")),
            ["following-sibling::a"] = Step(Axis.FollowingSibling, Nm("a")),
            ["following::a"] = Step(Axis.Following, Nm("a")),
            ["namespace::a"] = Step(Axis.Namespace, Nm("a")),
            // [32] AbbrevForwardStep
            ["@a"] = Step(Axis.Attribute, Nm("a")),
            ["@*"] = Step(Axis.Attribute, AnyName()),
            ["@attribute(a)"] = Step(Axis.Attribute, Attr(Q("a"))),
            ["attribute(a)"] = Step(Axis.Attribute, Attr(Q("a"))),
            ["schema-attribute(a)"] = Step(Axis.Attribute, SchemaAttr("a")),
            // [33] ReverseStep, [34] ReverseAxis, [35] AbbrevReverseStep
            ["parent::a"] = Step(Axis.Parent, Nm("a")),
            ["ancestor::a"] = Step(Axis.Ancestor, Nm("a")),
            ["preceding-sibling::a"] = Step(Axis.PrecedingSibling, Nm("a")),
            ["preceding::a"] = Step(Axis.Preceding, Nm("a")),
            ["ancestor-or-self::a"] = Step(Axis.AncestorOrSelf, Nm("a")),
            [".."] = Step(Axis.Parent, AnyKind()),
            ["..[1]"] = Step(Axis.Parent, AnyKind(), Int("1")),
            // [36] NodeTest, [37] NameTest, [38] WildcardNode
            ["p:a"] = Child("a", "p"),
            ["*"] = Step(Axis.Child, AnyName()),
            ["p:*"] = Step(Axis.Child, AnyLocal("p")),
            ["*:a"] = Step(Axis.Child, AnyNs("a")),
            // [39] FilterExpr, [40] PredicateList, [41] Predicate
            ["a[1]"] = Child("a", null, Int("1")),
            ["a[1][2]"] = Child("a", null, Int("1"), Int("2")),
            ["(1)[2]"] = Filter(Paren(Int("1")), Int("2")),
            ["$x[1][2]"] = Filter(Var("x"), Int("1"), Int("2")),
            ["f()[1]"] = Filter(Call("f"), Int("1")),
            // [42] PrimaryExpr, [43] Literal, [44] NumericLiteral, [45] VarRef
            ["1"] = Int("1"),
            ["1.5"] = Dec("1.5"),
            [".5"] = Dec(".5"),
            ["1e3"] = Dbl("1e3"),
            ["1.5E-3"] = Dbl("1.5E-3"),
            ["'a''b'"] = Str("a'b"),
            ["\"a\"\"b\""] = Str("a\"b"),
            ["''"] = Str(string.Empty),
            ["$x"] = Var("x"),
            ["$p:x"] = Var("x", "p"),
            ["$ x"] = Var("x"),
            // [46] ParenthesizedExpr, [47] ContextItemExpr
            ["()"] = Paren(),
            ["(1)"] = Paren(Int("1")),
            ["(1, 2)"] = Paren(Seq(Int("1"), Int("2"))),
            ["."] = Dot(),
            // [48] FunctionCall
            ["f()"] = Call("f"),
            ["f(1, 2)"] = Call("f", null, Int("1"), Int("2")),
            ["fn:f(1)"] = Call("f", "fn", Int("1")),
            ["f(1 to 2)"] = Call("f", null, Range(Int("1"), Int("2"))),
            // [49] SingleType, [50] SequenceType, [51] OccurrenceIndicator, [52] ItemType, [53] AtomicType
            ["a instance of empty-sequence()"] = InstanceOf(A, SeqType(null)),
            ["a instance of item()*"] = InstanceOf(A, SeqType(Item(), Occurrence.ZeroOrMore)),
            ["a instance of xs:string+"] = InstanceOf(A, SeqType(Atomic("string", "xs"), Occurrence.OneOrMore)),
            ["a instance of element()"] = InstanceOf(A, SeqType(KindItem(Elem()))),
            ["a instance of item()*, 2"] = Seq(InstanceOf(A, SeqType(Item(), Occurrence.ZeroOrMore)), Int("2")),
            // [54] KindTest, [55] AnyKindTest, [56] DocumentTest, [57] TextTest, [58] CommentTest
            ["node()"] = Step(Axis.Child, AnyKind()),
            ["text()"] = Step(Axis.Child, Text()),
            ["comment()"] = Step(Axis.Child, Comment()),
            ["document-node()"] = Step(Axis.Child, Doc()),
            ["document-node(element(a))"] = Step(Axis.Child, Doc(Elem(Q("a")))),
            ["document-node(schema-element(a))"] = Step(Axis.Child, Doc(SchemaElem("a"))),
            // [59] PITest
            ["processing-instruction()"] = Step(Axis.Child, PI()),
            ["processing-instruction(t)"] = Step(Axis.Child, PI("t")),
            ["processing-instruction('t')"] = Step(Axis.Child, PI("t")),
            ["processing-instruction(' t ')"] = Step(Axis.Child, PI("t")),
            // [60]-[64] AttributeTest, AttribNameOrWildcard, SchemaAttributeTest, AttributeDeclaration
            ["child::attribute()"] = Step(Axis.Child, Attr()),
            ["attribute(*)"] = Step(Axis.Attribute, Attr()),
            ["attribute(p:a, xs:string)"] = Step(Axis.Attribute, Attr(Q("a", "p"), Q("string", "xs"))),
            ["attribute(*, xs:string)"] = Step(Axis.Attribute, Attr(null, Q("string", "xs"))),
            // [65]-[69] ElementTest, ElementNameOrWildcard, SchemaElementTest, ElementDeclaration, TypeName
            ["element()"] = Step(Axis.Child, Elem()),
            ["element(*)"] = Step(Axis.Child, Elem()),
            ["element(a)"] = Step(Axis.Child, Elem(Q("a"))),
            ["element(a, t)"] = Step(Axis.Child, Elem(Q("a"), Q("t"))),
            ["element(a, t?)"] = Step(Axis.Child, Elem(Q("a"), Q("t"), true)),
            ["element(*, p:t?)"] = Step(Axis.Child, Elem(null, Q("t", "p"), true)),
            ["schema-element(p:a)"] = Step(Axis.Child, new SchemaElementTest(default, Q("a", "p"))),
            // Keyword-named steps and comments
            ["div"] = Child("div"),
            ["div div div"] = Arith(ArithmeticOperator.Divide, Child("div"), Child("div")),
            ["child::and"] = Step(Axis.Child, Nm("and")),
            ["/union/*"] = Path(PathRoot.Root, Child("union"), Step(Axis.Child, AnyName())),
            ["a (: comment :) | (: (: nested :) :) b"] = Set(SetOperator.Union, A, B),
        };

    /// <summary>Gets the precedence and associativity table, keyed by the input text.</summary>
    public static IReadOnlyDictionary<string, Expression> Precedence { get; } =
        new Dictionary<string, Expression>(StringComparer.Ordinal)
        {
            ["1 - 2 - 3"] = Arith(
                ArithmeticOperator.Subtract,
                Arith(ArithmeticOperator.Subtract, Int("1"), Int("2")),
                Int("3")
            ),
            ["1 - 2 * 3"] = Arith(
                ArithmeticOperator.Subtract,
                Int("1"),
                Arith(ArithmeticOperator.Multiply, Int("2"), Int("3"))
            ),
            ["1 * 2 - 3"] = Arith(
                ArithmeticOperator.Subtract,
                Arith(ArithmeticOperator.Multiply, Int("1"), Int("2")),
                Int("3")
            ),
            ["8 div 4 mod 3"] = Arith(
                ArithmeticOperator.Modulo,
                Arith(ArithmeticOperator.Divide, Int("8"), Int("4")),
                Int("3")
            ),
            ["a or b and c"] = Or(A, And(B, C)),
            ["a and b or c"] = Or(And(A, B), C),
            ["a or b or c"] = Or(Or(A, B), C),
            ["a = b and c"] = And(Cmp(ComparisonOperator.GeneralEqual, A, B), C),
            ["a = 1 to 2"] = Cmp(ComparisonOperator.GeneralEqual, A, Range(Int("1"), Int("2"))),
            ["1 + 2 to 3"] = Range(Arith(ArithmeticOperator.Add, Int("1"), Int("2")), Int("3")),
            ["-a | b"] = Set(SetOperator.Union, Unary(UnaryOperator.Minus, A), B),
            ["a | b intersect c"] = Set(SetOperator.Union, A, Set(SetOperator.Intersect, B, C)),
            ["a union b except c"] = Set(SetOperator.Except, Set(SetOperator.Union, A, B), C),
            ["1 * 2 | 3"] = Arith(ArithmeticOperator.Multiply, Int("1"), Set(SetOperator.Union, Int("2"), Int("3"))),
            ["1 + 2 instance of xs:integer"] = Arith(
                ArithmeticOperator.Add,
                Int("1"),
                InstanceOf(Int("2"), SeqType(Atomic("integer", "xs")))
            ),
            ["-1 cast as xs:integer"] = Cast(Unary(UnaryOperator.Minus, Int("1")), Single("integer", "xs")),
            ["1 - -2"] = Arith(ArithmeticOperator.Subtract, Int("1"), Unary(UnaryOperator.Minus, Int("2"))),
            ["a cast as xs:integer instance of xs:integer"] = InstanceOf(
                Cast(A, Single("integer", "xs")),
                SeqType(Atomic("integer", "xs"))
            ),
            ["a castable as xs:integer treat as xs:integer"] = Treat(
                Castable(A, Single("integer", "xs")),
                SeqType(Atomic("integer", "xs"))
            ),
            ["-1 to 2"] = Range(Unary(UnaryOperator.Minus, Int("1")), Int("2")),
            ["if (a) then b else c or d"] = If(A, B, Or(C, Child("d"))),
            ["a/b | c"] = Set(SetOperator.Union, Path(PathRoot.Relative, A, B), C),
        };
}
