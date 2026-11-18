namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Comparison;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Evaluation.Comparison;
using NetEvolve.Schemas.XPath.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static ComparisonFixture;

/// <summary>The general comparisons =, !=, &lt;, &lt;=, &gt;, and &gt;= (XPath 2.0, 3.5.2) in the mode without 1.0 compatibility.</summary>
public class GeneralComparisonTests
{
    private static readonly string[] Operators = ["=", "!=", "<", "<=", ">", ">="];

    /// <summary>The same rows as the value comparisons of two numbers: a singleton pair gives the result of the value operator.</summary>
    public static IEnumerable<Func<(string Left, string Op, string Right, string Expected)>> NumericPairs()
    {
        foreach (var row in ValueComparisonTests.NumericPairs())
        {
            var (left, op, right, expected) = row();
            yield return () => (left, General(op), right, expected);
        }
    }

    [Test]
    [MethodDataSource(nameof(NumericPairs))]
    public async Task SingletonNumbers_GiveTheResultOfTheValueOperator(
        string left,
        string op,
        string right,
        string expected
    ) => await Assert.That(Outcome(left, op, right)).IsEqualTo(expected);

    [Test]
    public async Task Sequences_AreComparedExistentially_AsTheSpecificationExamplesShow()
    {
        // XPath 2.0, 3.5.2: general comparisons are not transitive, and = and != are not inverses.
        await Assert.That(Show(Run(Operator("="), Many("i:1", "i:2"), Many("i:2", "i:3")))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("="), Many("i:2", "i:3"), Many("i:3", "i:4")))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("="), Many("i:1", "i:2"), Many("i:3", "i:4")))).IsEqualTo("false");
        await Assert.That(Show(Run(Operator("!="), Many("i:1", "i:2"), Many("i:2", "i:3")))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("!="), Many("i:2"), Many("i:2", "i:2")))).IsEqualTo("false");
    }

    [Test]
    public async Task Sequences_OrderExistentially()
    {
        await Assert.That(Show(Run(Operator("<"), Many("i:5", "i:1"), Many("i:2", "i:0")))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("<"), Many("i:5", "i:6"), Many("i:2", "i:0")))).IsEqualTo("false");
        await Assert.That(Show(Run(Operator(">"), Many("i:5", "i:1"), Many("i:5", "i:0")))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("<="), Many("i:5", "i:6"), Many("i:5", "i:0")))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator(">="), Many("i:1", "i:2"), Many("i:3", "i:4")))).IsEqualTo("false");
    }

    [Test]
    [Arguments("=")]
    [Arguments("!=")]
    [Arguments("<")]
    [Arguments("<=")]
    [Arguments(">")]
    [Arguments(">=")]
    public async Task EmptyOperand_IsFalseForEveryOperator(string op)
    {
        var comparison = Operator(op);

        await Assert.That(Show(Run(comparison, XdmSequence.Empty, One("i:1")))).IsEqualTo("false");
        await Assert.That(Show(Run(comparison, One("i:1"), XdmSequence.Empty))).IsEqualTo("false");
        await Assert.That(Show(Run(comparison, XdmSequence.Empty, XdmSequence.Empty))).IsEqualTo("false");
        await Assert.That(Show(Run(comparison, XdmSequence.Empty, Many("s:a", "i:1")))).IsEqualTo("false");
    }

    [Test]
    [Arguments("u:1", "i:1", "true")]
    [Arguments("u:1.0", "i:1", "true")]
    [Arguments("u:1e0", "i:1", "true")]
    [Arguments("u: 1 ", "i:1", "true")]
    [Arguments("i:1", "u:1", "true")]
    [Arguments("u:1", "d:1.0", "true")]
    [Arguments("u:1", "f:1", "true")]
    [Arguments("u:1", "e:1", "true")]
    [Arguments("u:2", "i:1", "false")]
    [Arguments("u:10", "i:9", "false")]
    [Arguments("u:NaN", "e:NaN", "false")]
    [Arguments("u:INF", "e:INF", "true")]
    [Arguments("u:18446744073709551616", "w:18446744073709551616", "true")]
    public async Task UntypedAtomic_AgainstANumber_IsCastToDouble(string left, string right, string expected) =>
        await Assert.That(Outcome(left, "=", right)).IsEqualTo(expected);

    [Test]
    public async Task UntypedAtomic_AgainstANumber_OrdersAsADouble()
    {
        await Assert.That(Outcome("u:10", "<", "i:9")).IsEqualTo("false");
        await Assert.That(Outcome("u:10", ">", "i:9")).IsEqualTo("true");
        await Assert.That(Outcome("i:9", "<", "u:10")).IsEqualTo("true");
        await Assert.That(Outcome("u:1.5", ">=", "d:1.5")).IsEqualTo("true");
        await Assert.That(Outcome("u:NaN", "!=", "e:NaN")).IsEqualTo("true");
    }

    [Test]
    [Arguments("u:abc", "i:1")]
    [Arguments("i:1", "u:abc")]
    [Arguments("u:", "e:1")]
    [Arguments("u:1,5", "d:1.5")]
    [Arguments("u:0x10", "i:16")]
    public async Task UntypedAtomic_ThatIsNotADouble_RaisesForg0001(string left, string right)
    {
        await Assert.That(Outcome(left, "=", right)).IsEqualTo("FORG0001");
        await Assert.That(Outcome(left, "!=", right)).IsEqualTo("FORG0001");
    }

    // A lexical form beyond the digit cap of the datatype layer is an implementation limit, not an invalid form (#621).
    [Test]
    public async Task UntypedAtomic_BeyondTheDigitCap_RaisesFoca0001()
    {
        var digits = "u:" + new string('9', 5000);

        await Assert.That(Outcome(digits, "=", "i:1")).IsEqualTo("FOCA0001");
        await Assert.That(Outcome("d:1", "<", digits)).IsEqualTo("FOCA0001");
    }

    [Test]
    [Arguments("u:abc", "s:abc", "true")]
    [Arguments("s:abc", "u:abc", "true")]
    [Arguments("u:1", "s:1", "true")]
    [Arguments("u:1", "s:1.0", "false")]
    [Arguments("u:abc", "s:abd", "false")]
    [Arguments("u:abc", "a:abc", "true")]
    public async Task UntypedAtomic_AgainstAString_IsCastToString(string left, string right, string expected) =>
        await Assert.That(Outcome(left, "=", right)).IsEqualTo(expected);

    [Test]
    [Arguments("u:abc", "u:abc", "=", "true")]
    [Arguments("u:1", "u:1.0", "=", "false")]
    [Arguments("u:1", "u:1.0", "!=", "true")]
    [Arguments("u:10", "u:9", "<", "true")]
    [Arguments("u:a", "u:b", "<", "true")]
    [Arguments("u:b", "u:a", "<=", "false")]
    public async Task UntypedAtomic_AgainstUntypedAtomic_IsComparedAsStrings(
        string left,
        string right,
        string op,
        string expected
    ) => await Assert.That(Outcome(left, op, right)).IsEqualTo(expected);

    [Test]
    [Arguments("u:true", "b:true", "true")]
    [Arguments("u:1", "b:true", "true")]
    [Arguments("u:0", "b:false", "true")]
    [Arguments("u:false", "b:true", "false")]
    [Arguments("b:true", "u:true", "true")]
    public async Task UntypedAtomic_AgainstABoolean_IsCastToBoolean(string left, string right, string expected) =>
        await Assert.That(Outcome(left, "=", right)).IsEqualTo(expected);

    [Test]
    public async Task UntypedAtomic_AgainstABoolean_ThatIsNotLexicallyABoolean_RaisesForg0001()
    {
        await Assert.That(Outcome("u:yes", "=", "b:true")).IsEqualTo("FORG0001");
        await Assert.That(Outcome("b:true", "=", "u:2")).IsEqualTo("FORG0001");
    }

    [Test]
    public async Task UntypedAtomic_AgainstAnAnyUri_IsCastToAnyUri()
    {
        await Assert.That(Outcome("u:http://a", "=", "a:http://a")).IsEqualTo("true");
        await Assert.That(Outcome("a:http://a", "=", "u:http://b")).IsEqualTo("false");
    }

    [Test]
    public async Task TheSpecificationExample_ComparesTheUntypedValuesAsNumbersOrStrings()
    {
        // XPath 2.0, 3.5.2: $a, $b, and $c are untyped with the string values "1", "2", and "2.0".
        var left = Many("u:1", "u:2");

        await Assert.That(Show(Run(Operator("="), left, Many("u:2.0", "d:3.0")))).IsEqualTo("false");
        await Assert.That(Show(Run(Operator("="), left, Many("u:2.0", "d:2.0")))).IsEqualTo("true");
    }

    [Test]
    [Arguments("s:1", "i:1")]
    [Arguments("i:1", "s:1")]
    [Arguments("b:true", "i:1")]
    [Arguments("i:1", "b:true")]
    [Arguments("s:true", "b:true")]
    [Arguments("a:http://a", "i:1")]
    [Arguments("s:a", "d:1.5")]
    public async Task IncomparableTypes_RaiseXpty0004(string left, string right)
    {
        foreach (var op in Operators)
        {
            await Assert.That(Outcome(left, op, right)).IsEqualTo("XPTY0004");
        }
    }

    [Test]
    public async Task AnEarlyMatch_ComesBeforeAnIncomparablePair_AndALaterOneDoesNot()
    {
        // XPath 2.0, 3.5.2: a general comparison may return true as soon as it finds a pair, so an error after it is never seen.
        await Assert.That(Show(Run(Operator("="), Many("i:1", "s:a"), One("i:1")))).IsEqualTo("true");
        await Assert.That(Outcome(() => Run(Operator("="), Many("s:a", "i:1"), One("i:1")))).IsEqualTo("XPTY0004");
        await Assert.That(Show(Run(Operator("="), One("i:1"), Many("i:1", "s:a")))).IsEqualTo("true");
    }

    [Test]
    public async Task NodeOperands_AreAtomized()
    {
        var source = new FakeNodeSource();
        var attribute = XdmSequence.Create(XdmItem.FromNode(new XPathNode(source.Attribute)));
        var text = XdmSequence.Create(XdmItem.FromNode(new XPathNode(source.Text)));
        var element = XdmSequence.Create(XdmItem.FromNode(new XPathNode(source.Element)));

        await Assert.That(Show(Run(Operator("="), attribute, One("i:1")))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("="), attribute, One("s:1")))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("<"), attribute, One("i:2")))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("="), text, One("s:hello")))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("="), element, text))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("!="), element, attribute))).IsEqualTo("true");
        await Assert.That(Outcome(() => Run(Operator("="), text, One("i:1")))).IsEqualTo("FORG0001");
    }

    [Test]
    public async Task Booleans_AreComparedAsBooleans_AndNotByTheirEffectiveValue()
    {
        // Limitation: the rules of XPath 1.0 compatibility mode, which convert the other operand to a boolean, are not applied (XP-20).
        await Assert.That(Outcome("b:true", "=", "b:true")).IsEqualTo("true");
        await Assert.That(Outcome("b:false", "<", "b:true")).IsEqualTo("true");
        await Assert.That(Outcome("b:true", "=", "i:1")).IsEqualTo("XPTY0004");
        await Assert.That(Outcome("b:true", "=", "s:a")).IsEqualTo("XPTY0004");
    }

    [Test]
    public async Task String_IsComparedByCodepoint()
    {
        await Assert.That(Outcome("s:a", ">", "s:B")).IsEqualTo("true");
        await Assert.That(Outcome("s:" + (char)0xFFFF, "<", "s:" + char.ConvertFromUtf32(0x10000))).IsEqualTo("true");
        await Assert.That(Show(Run(Operator("<"), Many("s:z", "s:b"), One("s:c")))).IsEqualTo("true");
    }

    [Test]
    public async Task TheResult_IsABooleanItem()
    {
        var result = Run(Operator("="), Many("i:1", "i:2"), Many("i:2"));

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].TypeId).IsEqualTo(XdmTypeId.Boolean);
    }

    /// <summary>Counts the pairs it is asked to compare, with an implementation registered from the test project for integers.</summary>
    private sealed class CountingEntry
    {
        public int Calls { get; private set; }

        public ComparisonOperatorTable Table()
        {
            var table = new ComparisonOperatorTable();
            table.Register(ComparisonRelation.Equal, XdmTypeId.XsInteger, XdmTypeId.XsInteger, Compare);
            table.Register(ComparisonRelation.LessThan, XdmTypeId.XsInteger, XdmTypeId.XsInteger, Compare);
            return table;
        }

        private bool Compare(ComparisonRelation relation, in XdmItem left, in XdmItem right)
        {
            Calls++;
            _ = left.TryGetInteger(out var a);
            _ = right.TryGetInteger(out var b);
            return relation == ComparisonRelation.Equal ? a == b : a < b;
        }
    }

    [Test]
    public async Task AGeneralComparison_StopsAtTheFirstMatchingPair()
    {
        var counting = new CountingEntry();

        var result = Run(Operator("="), Many("i:1", "i:2", "i:3"), Many("i:1", "i:2", "i:3"), counting.Table());

        await Assert.That(Show(result)).IsEqualTo("true");
        await Assert.That(counting.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task AGeneralComparison_StopsAtTheMatchingPairInTheOrderOfTheOperands()
    {
        var counting = new CountingEntry();

        // The pairs are (1,9), (1,2), (2,9), (2,2): the fourth matches.
        var result = Run(Operator("="), Many("i:1", "i:2", "i:3"), Many("i:9", "i:2"), counting.Table());

        await Assert.That(Show(result)).IsEqualTo("true");
        await Assert.That(counting.Calls).IsEqualTo(4);
    }

    [Test]
    public async Task AGeneralComparison_WithoutAMatch_ComparesEveryPair()
    {
        var counting = new CountingEntry();

        var result = Run(Operator("="), Many("i:1", "i:2", "i:3"), Many("i:7", "i:8"), counting.Table());

        await Assert.That(Show(result)).IsEqualTo("false");
        await Assert.That(counting.Calls).IsEqualTo(6);
    }

    [Test]
    public async Task AGeneralComparison_ThatIsDecidedByTheFirstPair_ForTheOtherOperators()
    {
        var counting = new CountingEntry();

        var result = Run(Operator("<"), Many("i:1", "i:2", "i:3"), Many("i:5", "i:6"), counting.Table());

        await Assert.That(Show(result)).IsEqualTo("true");
        await Assert.That(counting.Calls).IsEqualTo(1);
    }

    [Test]
    public async Task AnEmptyOperand_ComparesNoPair()
    {
        var counting = new CountingEntry();

        _ = Run(Operator("="), XdmSequence.Empty, Many("i:1", "i:2"), counting.Table());
        _ = Run(Operator("="), Many("i:1", "i:2"), XdmSequence.Empty, counting.Table());

        await Assert.That(counting.Calls).IsEqualTo(0);
    }
}
