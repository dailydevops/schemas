namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Binding;

using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.DateTime;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Duration;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Each conversion rule of XPath 2.0 3.1.5 and B.1, run through a built-in, a host, and a user registration with identical results.</summary>
public class FunctionConversionTests
{
    private static readonly FakeNodeSource Fake = new();

    private static XdmItem Text => XdmItem.FromNode(new XPathNode(Fake.Text));

    private static XdmItem Attribute => XdmItem.FromNode(new XPathNode(Fake.Attribute));

    private static XdmSequence One(in XdmItem item) => XdmSequence.Create(item);

    private static XdmSequence Many(params XdmItem[] items) => XdmSequence.Create(items);

    private static XPathParameterType Type(XPathItemType type, Occurrence occurrence = Occurrence.ExactlyOne) =>
        new(type, occurrence);

    private static XdmItem Decimal(string lexical) => XdmItem.FromDecimal(XsdDecimal.Parse(lexical).Value);

    // Atomization

    [Test]
    public async Task Atomization_TextNode_IsAtomizedAndCastToTheExpectedString() =>
        await BindingHarness.AssertConverts(Type(XPathItemType.String), One(Text), One(XdmItem.FromString("hello")));

    [Test]
    public async Task Atomization_AttributeNode_IsAtomizedAndCastToTheExpectedInteger() =>
        await BindingHarness.AssertConverts(Type(XPathItemType.Integer), One(Attribute), One(XdmItem.FromInteger(1)));

    [Test]
    public async Task Atomization_NodesInASequence_AreAtomizedItemByItem() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Double, Occurrence.ZeroOrMore),
            Many(Attribute, XdmItem.FromInteger(2), Attribute),
            Many(XdmItem.FromDouble(1), XdmItem.FromDouble(2), XdmItem.FromDouble(1))
        );

    // Cast of xs:untypedAtomic

    [Test]
    public async Task UntypedAtomic_ToDouble_IsCastAfterWhitespaceCollapse() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Double),
            One(XdmItem.FromUntypedAtomic(" 1.5 ")),
            One(XdmItem.FromDouble(1.5))
        );

    [Test]
    public async Task UntypedAtomic_ToNumeric_IsCastToDouble() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Numeric),
            One(XdmItem.FromUntypedAtomic("3")),
            One(XdmItem.FromDouble(3))
        );

    [Test]
    public async Task UntypedAtomic_ToString_BecomesAString() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.String),
            One(XdmItem.FromUntypedAtomic(" a  b ")),
            One(XdmItem.FromString(" a  b "))
        );

    [Test]
    public async Task UntypedAtomic_ToBoolean_UsesTheLexicalForms() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Boolean),
            Many(XdmItem.FromUntypedAtomic("1")),
            One(XdmItem.FromBoolean(true))
        );

    [Test]
    public async Task UntypedAtomic_ToInteger_IsCastToTheIntegerValue() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Integer),
            One(XdmItem.FromUntypedAtomic("+7")),
            One(XdmItem.FromInteger(7))
        );

    [Test]
    public async Task UntypedAtomic_ToDecimal_IsCastToTheDecimalValue() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Decimal),
            One(XdmItem.FromUntypedAtomic("2.50")),
            One(Decimal("2.50"))
        );

    [Test]
    public async Task UntypedAtomic_ToFloat_IsCastToTheFloatValue() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Float),
            One(XdmItem.FromUntypedAtomic("0.5")),
            One(XdmItem.FromFloat(0.5f))
        );

    [Test]
    public async Task UntypedAtomic_ToDate_IsCastToTheDateValue() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Date),
            One(XdmItem.FromUntypedAtomic("2026-01-02")),
            One(XdmItem.FromDateTime(XsdDateTime.Parse("2026-01-02", XsdDateTimeKind.Date, XsdVersion.V10).Value))
        );

    [Test]
    public async Task UntypedAtomic_ToDayTimeDuration_IsCastToTheDurationValue() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.DayTimeDuration),
            One(XdmItem.FromUntypedAtomic("PT1H")),
            One(XdmItem.FromDuration(XsdDuration.Parse("PT1H", DurationKind.DayTime).Value, DurationKind.DayTime))
        );

    [Test]
    public async Task UntypedAtomic_ToAnyUri_IsCastToTheUri() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.AnyUri),
            One(XdmItem.FromUntypedAtomic(" urn:a ")),
            One(XdmItem.FromAnyUri("urn:a"))
        );

    [Test]
    public async Task UntypedAtomic_ToAnyAtomic_StaysUntyped()
    {
        var untyped = One(XdmItem.FromUntypedAtomic("x"));

        await BindingHarness.AssertConverts(Type(XPathItemType.AnyAtomic), untyped, untyped);
    }

    [Test]
    public async Task UntypedAtomic_ToUntypedAtomic_StaysUntyped()
    {
        var untyped = One(XdmItem.FromUntypedAtomic("x"));

        await BindingHarness.AssertConverts(Type(XPathItemType.UntypedAtomic), untyped, untyped);
    }

    [Test]
    public async Task UntypedAtomic_ToDouble_AppliesTheXsd10LexicalRules() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Double),
            One(XdmItem.FromUntypedAtomic("INF")),
            One(XdmItem.FromDouble(double.PositiveInfinity))
        );

    [Test]
    public async Task UntypedAtomic_ToDouble_RejectsThePlusInfSpellingOfXsd11() =>
        await BindingHarness.AssertFails(
            Type(XPathItemType.Double),
            One(XdmItem.FromUntypedAtomic("+INF")),
            "FORG0001"
        );

    [Test]
    public async Task UntypedAtomic_InvalidLexicalForm_RaisesFORG0001() =>
        await BindingHarness.AssertFails(Type(XPathItemType.Double), One(XdmItem.FromUntypedAtomic("abc")), "FORG0001");

    [Test]
    public async Task UntypedAtomic_IntegerBeyondSixtyFourBits_RaisesFOCA0003() =>
        await BindingHarness.AssertFails(
            Type(XPathItemType.Integer),
            One(XdmItem.FromUntypedAtomic("99999999999999999999")),
            "FOCA0003"
        );

    // Numeric promotion, subtype substitution, and URI promotion

    [Test]
    public async Task Promotion_IntegerToDouble() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Double),
            One(XdmItem.FromInteger(3)),
            One(XdmItem.FromDouble(3))
        );

    [Test]
    public async Task Promotion_DecimalToDouble() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Double),
            One(Decimal("2.5")),
            One(XdmItem.FromDouble(2.5))
        );

    [Test]
    public async Task Promotion_FloatToDouble() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Double),
            One(XdmItem.FromFloat(0.5f)),
            One(XdmItem.FromDouble(0.5))
        );

    [Test]
    public async Task Promotion_IntegerToFloat() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Float),
            One(XdmItem.FromInteger(3)),
            One(XdmItem.FromFloat(3f))
        );

    [Test]
    public async Task Promotion_DecimalToFloat() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Float),
            One(Decimal("2.5")),
            One(XdmItem.FromFloat(2.5f))
        );

    [Test]
    public async Task Promotion_DoubleToFloat_IsATypeError() =>
        await BindingHarness.AssertFails(Type(XPathItemType.Float), One(XdmItem.FromDouble(1)), "XPTY0004");

    [Test]
    public async Task Promotion_DoubleToDecimal_IsATypeError() =>
        await BindingHarness.AssertFails(Type(XPathItemType.Decimal), One(XdmItem.FromDouble(1)), "XPTY0004");

    [Test]
    public async Task Substitution_IntegerForDecimal_StaysAnInteger()
    {
        var integer = One(XdmItem.FromInteger(3));

        await BindingHarness.AssertConverts(Type(XPathItemType.Decimal), integer, integer);
    }

    [Test]
    public async Task Substitution_AnyNumericForNumeric_IsNotPromoted()
    {
        var single = One(XdmItem.FromFloat(1.5f));

        await BindingHarness.AssertConverts(Type(XPathItemType.Numeric), single, single);
    }

    [Test]
    public async Task Promotion_AnyUriToString() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.String),
            One(XdmItem.FromAnyUri("urn:a")),
            One(XdmItem.FromString("urn:a"))
        );

    // XPTY0004

    [Test]
    public async Task TypeError_WrongItemType_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(Type(XPathItemType.Integer), One(XdmItem.FromString("1")), "XPTY0004");

    [Test]
    public async Task TypeError_BooleanForDouble_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(Type(XPathItemType.Double), One(XdmItem.FromBoolean(true)), "XPTY0004");

    [Test]
    public async Task TypeError_SequenceWhereOneItemIsExpected_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(
            Type(XPathItemType.String),
            Many(XdmItem.FromString("a"), XdmItem.FromString("b")),
            "XPTY0004"
        );

    [Test]
    public async Task TypeError_EmptySequenceWhereOneItemIsRequired_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(Type(XPathItemType.String), XdmSequence.Empty, "XPTY0004");

    [Test]
    public async Task TypeError_TwoItemsForZeroOrOne_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(
            Type(XPathItemType.String, Occurrence.ZeroOrOne),
            Many(XdmItem.FromString("a"), XdmItem.FromString("b")),
            "XPTY0004"
        );

    [Test]
    public async Task TypeError_EmptySequenceForOneOrMore_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(
            Type(XPathItemType.String, Occurrence.OneOrMore),
            XdmSequence.Empty,
            "XPTY0004"
        );

    [Test]
    public async Task TypeError_AtomicValueWhereNodeIsExpected_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(Type(XPathItemType.Node), One(XdmItem.FromInteger(1)), "XPTY0004");

    [Test]
    public async Task TypeError_OneBadItemInASequence_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(
            Type(XPathItemType.Integer, Occurrence.ZeroOrMore),
            Many(XdmItem.FromInteger(1), XdmItem.FromString("x")),
            "XPTY0004"
        );

    // Occurrence and item() without conversion

    [Test]
    public async Task Occurrence_EmptySequenceForZeroOrOne_IsAccepted() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Integer, Occurrence.ZeroOrOne),
            XdmSequence.Empty,
            XdmSequence.Empty
        );

    [Test]
    public async Task Occurrence_ZeroOrMore_ConvertsEveryItem() =>
        await BindingHarness.AssertConverts(
            Type(XPathItemType.Double, Occurrence.OneOrMore),
            Many(XdmItem.FromUntypedAtomic("1"), XdmItem.FromInteger(2)),
            Many(XdmItem.FromDouble(1), XdmItem.FromDouble(2))
        );

    [Test]
    public async Task ItemType_AnyItem_PassesNodesAndAtomicValuesUnchanged()
    {
        var mixed = Many(Text, XdmItem.FromInteger(1), XdmItem.FromUntypedAtomic("u"));

        await BindingHarness.AssertConverts(Type(XPathItemType.Item, Occurrence.ZeroOrMore), mixed, mixed);
    }

    [Test]
    public async Task ItemType_Node_PassesNodesUnchanged()
    {
        var nodes = Many(Text, Attribute);

        await BindingHarness.AssertConverts(Type(XPathItemType.Node, Occurrence.OneOrMore), nodes, nodes);
    }
}
