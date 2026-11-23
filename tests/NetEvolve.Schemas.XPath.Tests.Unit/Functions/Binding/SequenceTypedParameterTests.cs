namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Binding;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Tests.Unit.Types;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A function parameter declared as a sequence type (XPath 2.0, 2.5.3): the function conversion rules apply to the item types they
/// name, a derived atomic type matches by derivation, and a kind test or <c>empty-sequence()</c> is matched by the shared matcher.
/// </summary>
public class SequenceTypedParameterTests
{
    private static readonly FakeNodeSource Fake = new();

    private static XPathParameterType Parameter(string type) =>
        XPathParameterType.FromSequenceType(TypeFixture.ParseType(type), TypeFixture.Statics);

    private static XdmSequence One(in XdmItem item) => XdmSequence.Create(item);

    private static XdmItem Int(long value) => XdmItem.FromInteger(value).WithTypeId(XdmTypes.Get("int"));

    private static XdmItem Element => XdmItem.FromNode(new XPathNode(Fake.Element));

    private static XdmItem Attribute => XdmItem.FromNode(new XPathNode(Fake.Attribute));

    // The item types of a signature keep their conversion rules.

    [Test]
    [Arguments("item()", nameof(XPathItemType.Item), nameof(Occurrence.ExactlyOne))]
    [Arguments("item()*", nameof(XPathItemType.Item), nameof(Occurrence.ZeroOrMore))]
    [Arguments("node()?", nameof(XPathItemType.Node), nameof(Occurrence.ZeroOrOne))]
    [Arguments("xs:anyAtomicType+", nameof(XPathItemType.AnyAtomic), nameof(Occurrence.OneOrMore))]
    [Arguments("xs:untypedAtomic", nameof(XPathItemType.UntypedAtomic), nameof(Occurrence.ExactlyOne))]
    [Arguments("xs:string?", nameof(XPathItemType.String), nameof(Occurrence.ZeroOrOne))]
    [Arguments("xs:boolean", nameof(XPathItemType.Boolean), nameof(Occurrence.ExactlyOne))]
    [Arguments("xs:integer", nameof(XPathItemType.Integer), nameof(Occurrence.ExactlyOne))]
    [Arguments("xs:decimal", nameof(XPathItemType.Decimal), nameof(Occurrence.ExactlyOne))]
    [Arguments("xs:float", nameof(XPathItemType.Float), nameof(Occurrence.ExactlyOne))]
    [Arguments("xs:double*", nameof(XPathItemType.Double), nameof(Occurrence.ZeroOrMore))]
    [Arguments("xs:anyURI", nameof(XPathItemType.AnyUri), nameof(Occurrence.ExactlyOne))]
    [Arguments("xs:dateTime", nameof(XPathItemType.DateTime), nameof(Occurrence.ExactlyOne))]
    [Arguments("xs:date", nameof(XPathItemType.Date), nameof(Occurrence.ExactlyOne))]
    [Arguments("xs:time", nameof(XPathItemType.Time), nameof(Occurrence.ExactlyOne))]
    [Arguments("xs:duration", nameof(XPathItemType.Duration), nameof(Occurrence.ExactlyOne))]
    [Arguments("xs:yearMonthDuration", nameof(XPathItemType.YearMonthDuration), nameof(Occurrence.ExactlyOne))]
    [Arguments("xs:dayTimeDuration", nameof(XPathItemType.DayTimeDuration), nameof(Occurrence.ExactlyOne))]
    public async Task NamedItemType_IsTheItemTypeOfASignature(string type, string expected, string occurrence)
    {
        var parameter = Parameter(type);

        await Assert.That(parameter.ItemType).IsEqualTo(Enum.Parse<XPathItemType>(expected));
        await Assert.That(parameter.Occurrence).IsEqualTo(Enum.Parse<Occurrence>(occurrence));
    }

    [Test]
    public async Task Double_PromotesAnInteger() =>
        await BindingHarness.AssertConverts(
            Parameter("xs:double"),
            One(XdmItem.FromInteger(3)),
            One(XdmItem.FromDouble(3))
        );

    [Test]
    public async Task Double_CastsAnUntypedValue() =>
        await BindingHarness.AssertConverts(
            Parameter("xs:double?"),
            One(XdmItem.FromUntypedAtomic(" 1.5 ")),
            One(XdmItem.FromDouble(1.5))
        );

    [Test]
    public async Task String_PromotesAnUri() =>
        await BindingHarness.AssertConverts(
            Parameter("xs:string"),
            One(XdmItem.FromAnyUri("urn:a")),
            One(XdmItem.FromString("urn:a"))
        );

    [Test]
    public async Task String_AtomizesANode() =>
        await BindingHarness.AssertConverts(Parameter("xs:string"), One(Attribute), One(XdmItem.FromString("1")));

    [Test]
    public async Task Occurrence_IsChecked_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(Parameter("xs:string"), XdmSequence.Empty, "XPTY0004");

    // A derived atomic type matches by derivation.

    [Test]
    public async Task DerivedAtomicType_AcceptsAnItemOfThatType()
    {
        var item = One(Int(5));

        await BindingHarness.AssertConverts(Parameter("xs:int"), item, item);
    }

    [Test]
    public async Task DerivedAtomicType_AcceptsAnItemOfADerivedType()
    {
        var item = One(XdmItem.FromInteger(5).WithTypeId(XdmTypes.Get("byte")));

        await BindingHarness.AssertConverts(Parameter("xs:short"), item, item);
    }

    [Test]
    public async Task DerivedAtomicType_RejectsItsBaseType_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(Parameter("xs:int"), One(XdmItem.FromInteger(5)), "XPTY0004");

    [Test]
    public async Task DerivedAtomicType_RejectsAnUnrelatedType_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(Parameter("xs:int"), One(XdmItem.FromString("5")), "XPTY0004");

    [Test]
    public async Task DerivedAtomicType_IsNotPromoted_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(Parameter("xs:float"), One(XdmItem.FromDouble(1)), "XPTY0004");

    [Test]
    public async Task DerivedAtomicType_AppliesTheOccurrence()
    {
        var items = XdmSequence.Create([Int(1), Int(2)]);

        await BindingHarness.AssertConverts(Parameter("xs:int+"), items, items);
        await BindingHarness.AssertFails(Parameter("xs:int?"), items, "XPTY0004");
    }

    [Test]
    public async Task DerivedAtomicType_CastsAnUntypedValue() =>
        await BindingHarness.AssertConverts(Parameter("xs:int"), One(XdmItem.FromUntypedAtomic("5")), One(Int(5)));

    [Test]
    public async Task DerivedAtomicType_CastsAnUntypedValueThatDoesNotFit_RaisesFORG0001() =>
        await BindingHarness.AssertFails(Parameter("xs:byte"), One(XdmItem.FromUntypedAtomic("300")), "FORG0001");

    // A kind test is matched, not atomized.

    [Test]
    public async Task ElementTest_AcceptsAnElementAsItIs()
    {
        var element = One(Element);

        await BindingHarness.AssertConverts(Parameter("element()"), element, element);
    }

    [Test]
    public async Task ElementTest_RejectsAnAttributeAndAnAtomicValue_RaisesXPTY0004()
    {
        await BindingHarness.AssertFails(Parameter("element()"), One(Attribute), "XPTY0004");
        await BindingHarness.AssertFails(Parameter("element()"), One(XdmItem.FromString("x")), "XPTY0004");
    }

    [Test]
    public async Task ElementTest_WithAName_RejectsAnotherName_RaisesXPTY0004()
    {
        var element = One(Element);

        await BindingHarness.AssertConverts(Parameter("element(root)"), element, element);
        await BindingHarness.AssertFails(Parameter("element(other)"), element, "XPTY0004");
    }

    [Test]
    public async Task AttributeTest_IsNotAtomized()
    {
        var attribute = One(Attribute);

        await BindingHarness.AssertConverts(Parameter("attribute()"), attribute, attribute);
    }

    [Test]
    public async Task KindTest_AppliesTheOccurrence()
    {
        var element = One(Element);

        await BindingHarness.AssertConverts(Parameter("element()?"), XdmSequence.Empty, XdmSequence.Empty);
        await BindingHarness.AssertConverts(Parameter("element()*"), element, element);
        await BindingHarness.AssertFails(Parameter("element()"), XdmSequence.Empty, "XPTY0004");
        await BindingHarness.AssertFails(Parameter("element()?"), XdmSequence.Create([Element, Element]), "XPTY0004");
    }

    // empty-sequence()

    [Test]
    public async Task EmptySequence_AcceptsTheEmptySequenceOnly()
    {
        await BindingHarness.AssertConverts(Parameter("empty-sequence()"), XdmSequence.Empty, XdmSequence.Empty);
        await BindingHarness.AssertFails(Parameter("empty-sequence()"), One(XdmItem.FromInteger(1)), "XPTY0004");
    }

    // Guards

    [Test]
    public async Task Constructor_OtherItemType_IsNotASignatureType() =>
        await Assert
            .That(() => new XPathParameterType(XPathItemType.Other, Occurrence.ExactlyOne))
            .Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task FromSequenceType_WithoutASyntaxOrAContext_Throws()
    {
        await Assert
            .That(() => XPathParameterType.FromSequenceType(null!, TypeFixture.Statics))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => XPathParameterType.FromSequenceType(TypeFixture.ParseType("xs:int"), null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task FromSequenceType_UnknownTypeName_RaisesXPST0051()
    {
        var failure = await Assert.That(() => Parameter("xs:nonsense")).Throws<XPathException>();

        await Assert.That(failure!.Diagnostic.Code).IsEqualTo("XPST0051");
    }
}
