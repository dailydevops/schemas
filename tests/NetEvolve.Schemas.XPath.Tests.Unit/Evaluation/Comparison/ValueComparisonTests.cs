namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ComparisonFixture;

/// <summary>The value comparisons eq, ne, lt, le, gt, and ge (XPath 2.0, 3.5.1) with the operator mapping of B.2.</summary>
public class ValueComparisonTests
{
    private static readonly string[] Operators = ["eq", "ne", "lt", "le", "gt", "ge"];

    private static bool Holds(string op, int left, int right) =>
        op switch
        {
            "eq" => left == right,
            "ne" => left != right,
            "lt" => left < right,
            "le" => left <= right,
            "gt" => left > right,
            _ => left >= right,
        };

    /// <summary>Every pair of the four numeric types, three value pairs, and the six operators.</summary>
    public static IEnumerable<Func<(string Left, string Op, string Right, string Expected)>> NumericPairs()
    {
        int[][] values =
        [
            [1, 2],
            [2, 2],
            [2, 1],
        ];
        foreach (var leftKind in new[] { "i", "d", "f", "e" })
        {
            foreach (var rightKind in new[] { "i", "d", "f", "e" })
            {
                foreach (var pair in values)
                {
                    foreach (var op in Operators)
                    {
                        var left = leftKind + ":" + pair[0];
                        var right = rightKind + ":" + pair[1];
                        var expected = Holds(op, pair[0], pair[1]) ? "true" : "false";
                        var captured = op;
                        yield return () => (left, captured, right, expected);
                    }
                }
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(NumericPairs))]
    public async Task NumericPair_GivesTheResultOfTheOperator(string left, string op, string right, string expected) =>
        await Assert.That(Outcome(left, op, right)).IsEqualTo(expected);

    [Test]
    [Arguments("e:NaN", "e:NaN")]
    [Arguments("e:NaN", "e:1")]
    [Arguments("e:1", "e:NaN")]
    [Arguments("f:NaN", "f:NaN")]
    [Arguments("f:NaN", "e:NaN")]
    [Arguments("e:NaN", "i:1")]
    [Arguments("i:1", "f:NaN")]
    [Arguments("d:1.5", "e:NaN")]
    [Arguments("e:NaN", "w:18446744073709551616")]
    public async Task NaN_IsNotEqualToAnything_AndOnlyNeIsTrue(string left, string right)
    {
        await Assert.That(Outcome(left, "eq", right)).IsEqualTo("false");
        await Assert.That(Outcome(left, "ne", right)).IsEqualTo("true");
        await Assert.That(Outcome(left, "lt", right)).IsEqualTo("false");
        await Assert.That(Outcome(left, "le", right)).IsEqualTo("false");
        await Assert.That(Outcome(left, "gt", right)).IsEqualTo("false");
        await Assert.That(Outcome(left, "ge", right)).IsEqualTo("false");
    }

    [Test]
    [Arguments("e:0", "e:-0")]
    [Arguments("e:-0", "e:0")]
    [Arguments("f:0", "f:-0")]
    [Arguments("e:-0", "f:0")]
    [Arguments("e:-0", "d:0")]
    [Arguments("f:-0", "i:0")]
    [Arguments("e:-0", "e:-0")]
    public async Task PositiveAndNegativeZero_AreEqual(string left, string right)
    {
        await Assert.That(Outcome(left, "eq", right)).IsEqualTo("true");
        await Assert.That(Outcome(left, "ne", right)).IsEqualTo("false");
        await Assert.That(Outcome(left, "lt", right)).IsEqualTo("false");
        await Assert.That(Outcome(left, "le", right)).IsEqualTo("true");
        await Assert.That(Outcome(left, "gt", right)).IsEqualTo("false");
        await Assert.That(Outcome(left, "ge", right)).IsEqualTo("true");
    }

    [Test]
    [Arguments("e:INF", "e:INF", "eq", "true")]
    [Arguments("e:-INF", "e:-INF", "eq", "true")]
    [Arguments("e:INF", "e:-INF", "gt", "true")]
    [Arguments("e:INF", "i:9223372036854775807", "gt", "true")]
    [Arguments("e:-INF", "d:-0.5", "lt", "true")]
    [Arguments("f:INF", "e:INF", "eq", "true")]
    [Arguments("f:INF", "w:18446744073709551616", "gt", "true")]
    [Arguments("e:-INF", "w:-18446744073709551616", "lt", "true")]
    public async Task Infinity_OrdersBeyondEveryFiniteNumber(string left, string right, string op, string expected) =>
        await Assert.That(Outcome(left, op, right)).IsEqualTo(expected);

    [Test]
    [Arguments("i:9223372036854775807", "lt", "i:9223372036854775806", "false")]
    [Arguments("i:-9223372036854775808", "lt", "i:9223372036854775807", "true")]
    [Arguments("i:9007199254740993", "lt", "i:9007199254740994", "true")]
    [Arguments("w:18446744073709551616", "gt", "i:9223372036854775807", "true")]
    [Arguments("w:-18446744073709551616", "lt", "i:-9223372036854775808", "true")]
    [Arguments("w:18446744073709551616", "eq", "w:18446744073709551616", "true")]
    [Arguments("w:18446744073709551616", "lt", "w:18446744073709551617", "true")]
    [Arguments("w:18446744073709551616", "eq", "d:18446744073709551616.0", "true")]
    [Arguments("w:18446744073709551617", "gt", "d:18446744073709551616.5", "true")]
    public async Task Integer_IsComparedExactly_AtAnyWidth(string left, string op, string right, string expected) =>
        await Assert.That(Outcome(left, op, right)).IsEqualTo(expected);

    [Test]
    [Arguments("d:0.00000000000000000000000000001", "lt", "d:0.00000000000000000000000000002", "true")]
    [Arguments("d:1.0000000000000000000000000000001", "gt", "d:1", "true")]
    [Arguments("d:1.10", "eq", "d:1.1", "true")]
    [Arguments("d:0.1", "eq", "i:0", "false")]
    [Arguments("i:9223372036854775807", "lt", "d:9223372036854775807.5", "true")]
    [Arguments("d:-1.5", "lt", "i:-1", "true")]
    public async Task Decimal_IsComparedExactly_BeyondTwentyEightDigits(
        string left,
        string op,
        string right,
        string expected
    ) => await Assert.That(Outcome(left, op, right)).IsEqualTo(expected);

    [Test]
    [Arguments("i:9007199254740993", "eq", "e:9007199254740992", "true")]
    [Arguments("d:9007199254740993", "eq", "e:9007199254740992", "true")]
    [Arguments("i:16777217", "eq", "f:16777216", "true")]
    [Arguments("d:16777217", "eq", "f:16777216", "true")]
    [Arguments("f:0.1", "eq", "e:0.1", "false")]
    [Arguments("f:0.1", "gt", "e:0.1", "true")]
    [Arguments("d:0.1", "eq", "f:0.1", "true")]
    [Arguments("d:0.1", "eq", "e:0.1", "true")]
    public async Task MixedPrecision_PromotesToTheWiderFloatingType_AsB1Says(
        string left,
        string op,
        string right,
        string expected
    ) => await Assert.That(Outcome(left, op, right)).IsEqualTo(expected);

    [Test]
    [Arguments("s:a", "eq", "s:a", "true")]
    [Arguments("s:a", "ne", "s:a", "false")]
    [Arguments("s:a", "lt", "s:b", "true")]
    [Arguments("s:abc", "lt", "s:abd", "true")]
    [Arguments("s:ab", "lt", "s:abc", "true")]
    [Arguments("s:abc", "gt", "s:ab", "true")]
    [Arguments("s:", "lt", "s:a", "true")]
    [Arguments("s:", "eq", "s:", "true")]
    [Arguments("s:A", "lt", "s:a", "true")]
    [Arguments("s:a", "gt", "s:B", "true")]
    [Arguments("s:a", "eq", "s:A", "false")]
    [Arguments("s:a", "le", "s:a", "true")]
    [Arguments("s:a", "ge", "s:b", "false")]
    public async Task String_IsComparedByCodepoint(string left, string op, string right, string expected) =>
        await Assert.That(Outcome(left, op, right)).IsEqualTo(expected);

    [Test]
    public async Task String_BeyondTheBasicPlane_FollowsTheCodepointOrder_NotTheUtf16Order()
    {
        var supplementary = char.ConvertFromUtf32(0x10000);
        var highBasic = new string((char)0xFFFF, 1);

        await Assert.That(Outcome("s:" + highBasic, "lt", "s:" + supplementary)).IsEqualTo("true");
        await Assert.That(Outcome("s:" + supplementary, "gt", "s:" + highBasic)).IsEqualTo("true");
        await Assert.That(Outcome("s:" + supplementary, "eq", "s:" + supplementary)).IsEqualTo("true");
    }

    [Test]
    [Arguments("b:false", "eq", "b:false", "true")]
    [Arguments("b:true", "eq", "b:false", "false")]
    [Arguments("b:true", "ne", "b:false", "true")]
    [Arguments("b:false", "lt", "b:true", "true")]
    [Arguments("b:true", "lt", "b:false", "false")]
    [Arguments("b:true", "gt", "b:false", "true")]
    [Arguments("b:false", "le", "b:false", "true")]
    [Arguments("b:false", "ge", "b:true", "false")]
    [Arguments("b:true", "ge", "b:true", "true")]
    public async Task Boolean_OrdersFalseBeforeTrue(string left, string op, string right, string expected) =>
        await Assert.That(Outcome(left, op, right)).IsEqualTo(expected);

    [Test]
    [Arguments("a:http://a", "eq", "a:http://a", "true")]
    [Arguments("a:http://a", "lt", "a:http://b", "true")]
    [Arguments("a:http://a", "eq", "s:http://a", "true")]
    [Arguments("s:http://a", "eq", "a:http://a", "true")]
    [Arguments("a:http://b", "gt", "s:http://a", "true")]
    [Arguments("s:http://a", "ne", "a:http://b", "true")]
    public async Task AnyUri_IsPromotedToString_AsB1Says(string left, string op, string right, string expected) =>
        await Assert.That(Outcome(left, op, right)).IsEqualTo(expected);

    [Test]
    [Arguments("u:abc", "eq", "s:abc", "true")]
    [Arguments("s:abc", "eq", "u:abc", "true")]
    [Arguments("u:abc", "eq", "u:abc", "true")]
    [Arguments("u:abc", "lt", "u:abd", "true")]
    [Arguments("u:1", "eq", "u:1.0", "false")]
    [Arguments("u:abc", "eq", "a:abc", "true")]
    [Arguments("u:b", "gt", "s:a", "true")]
    public async Task UntypedAtomic_IsCastToString(string left, string op, string right, string expected) =>
        await Assert.That(Outcome(left, op, right)).IsEqualTo(expected);

    [Test]
    [Arguments("u:1", "i:1")]
    [Arguments("i:1", "u:1")]
    [Arguments("u:1", "e:1")]
    [Arguments("u:1.5", "d:1.5")]
    [Arguments("u:true", "b:true")]
    public async Task UntypedAtomic_AgainstANonStringType_IsXpty0004_BecauseItIsCastToString(string left, string right)
    {
        await Assert.That(Outcome(left, "eq", right)).IsEqualTo("XPTY0004");
        await Assert.That(Outcome(left, "lt", right)).IsEqualTo("XPTY0004");
    }

    [Test]
    [Arguments("s:1", "i:1")]
    [Arguments("i:1", "s:1")]
    [Arguments("s:true", "b:true")]
    [Arguments("b:true", "i:1")]
    [Arguments("i:1", "b:true")]
    [Arguments("b:false", "e:0")]
    [Arguments("a:http://a", "i:1")]
    [Arguments("s:a", "d:1.5")]
    [Arguments("f:1", "s:1")]
    public async Task IncomparableTypes_RaiseXpty0004(string left, string right)
    {
        foreach (var op in Operators)
        {
            await Assert.That(Outcome(left, op, right)).IsEqualTo("XPTY0004");
        }
    }

    [Test]
    [Arguments("eq")]
    [Arguments("ne")]
    [Arguments("lt")]
    [Arguments("le")]
    [Arguments("gt")]
    [Arguments("ge")]
    public async Task EmptyOperand_YieldsTheEmptySequence(string op)
    {
        var relation = Operator(op);

        await Assert.That(Show(Run(relation, XdmSequence.Empty, One("i:1")))).IsEqualTo("()");
        await Assert.That(Show(Run(relation, One("i:1"), XdmSequence.Empty))).IsEqualTo("()");
        await Assert.That(Show(Run(relation, XdmSequence.Empty, XdmSequence.Empty))).IsEqualTo("()");
    }

    [Test]
    public async Task EmptyOperand_WinsOverASequenceOfSeveralItemsOnTheOtherSide()
    {
        // XPath 2.0, 3.5.1: the empty sequence is checked before the length.
        await Assert.That(Show(Run(Operator("eq"), XdmSequence.Empty, Many("i:1", "i:2")))).IsEqualTo("()");
        await Assert.That(Show(Run(Operator("eq"), Many("i:1", "i:2"), XdmSequence.Empty))).IsEqualTo("()");
    }

    [Test]
    public async Task OperandOfSeveralItems_RaisesXpty0004()
    {
        await Assert.That(Outcome(() => Run(Operator("eq"), Many("i:1", "i:2"), One("i:1")))).IsEqualTo("XPTY0004");
        await Assert.That(Outcome(() => Run(Operator("lt"), One("i:1"), Many("i:1", "i:2")))).IsEqualTo("XPTY0004");
        await Assert.That(Outcome(() => Run(Operator("eq"), Many("s:a", "s:a"), One("s:a")))).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task NodeOperands_AreAtomized_AndAnUntypedNodeIsComparedAsAString()
    {
        var source = new FakeNodeSource();
        var attribute = XdmSequence.Create(XdmItem.FromNode(new XPathNode(source.Attribute)));
        var text = XdmSequence.Create(XdmItem.FromNode(new XPathNode(source.Text)));

        await Assert.That(Show(Run(Operator("eq"), attribute, One("s:1")))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("eq"), One("s:hello"), text))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("eq"), text, text))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("ne"), attribute, text))).IsEqualTo("true");
        await Assert.That(Outcome(() => Run(Operator("eq"), attribute, One("i:1")))).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task TheResult_IsABooleanItem()
    {
        var result = Run("i:1", "eq", "i:1");

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].TypeId).IsEqualTo(XdmTypeId.Boolean);
    }

    [Test]
    public async Task EveryValueOperator_MapsToItsRelation()
    {
        foreach (var op in Operators)
        {
            await Assert.That(ComparisonRelations.Of(Operator(op))).IsEqualTo(Relation(op));
            await Assert.That(ComparisonRelations.Of(Operator(General(op)))).IsEqualTo(Relation(op));
        }
    }
}
