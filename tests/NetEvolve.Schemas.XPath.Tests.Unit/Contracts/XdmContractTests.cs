namespace NetEvolve.Schemas.XPath.Tests.Unit.Contracts;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class XdmContractTests
{
    // XDM (Second Edition) section 2.5: the primitive atomic types of XPath 2.0, xs:untypedAtomic, and xs:integer (own fast path).
    private static readonly string[] AtomicFamilies =
    [
        "Boolean",
        "Integer",
        "Decimal",
        "Double",
        "Float",
        "String",
        "UntypedAtomic",
        "AnyUri",
        "QName",
        "Notation",
        "Duration",
        "YearMonthDuration",
        "DayTimeDuration",
        "DateTime",
        "Date",
        "Time",
        "GYearMonth",
        "GYear",
        "GMonthDay",
        "GDay",
        "GMonth",
        "HexBinary",
        "Base64Binary",
    ];

    [Test]
    public async Task XdmItem_Kinds_CoverAllXdmAtomicFamilies()
    {
        var names = Enum.GetNames<XdmItemKind>();

        await Assert.That(names).Contains("None").And.Contains("Node");
        foreach (var family in AtomicFamilies)
        {
            await Assert.That(names).Contains(family);
        }

        await Assert.That(names.Length).IsEqualTo(AtomicFamilies.Length + 2);
        await Assert.That(default(XdmItem).Kind).IsEqualTo(XdmItemKind.None);
    }

    [Test]
    public async Task XdmItem_FromBooleanIntegerDoubleString_RoundTrip()
    {
        var boolean = XdmItem.FromBoolean(true);
        var integer = XdmItem.FromInteger(-42);
        var number = XdmItem.FromDouble(1.5);
        var text = XdmItem.FromString("abc");

        await Assert.That(boolean.Kind).IsEqualTo(XdmItemKind.Boolean);
        await Assert.That(boolean.TryGetBoolean(out var b)).IsTrue();
        await Assert.That(b).IsTrue();
        await Assert.That(XdmItem.FromBoolean(false).TryGetBoolean(out var f)).IsTrue();
        await Assert.That(f).IsFalse();
        await Assert.That(integer.Kind).IsEqualTo(XdmItemKind.Integer);
        await Assert.That(integer.TryGetInteger(out var i)).IsTrue();
        await Assert.That(i).IsEqualTo(-42L);
        await Assert.That(number.Kind).IsEqualTo(XdmItemKind.Double);
        await Assert.That(number.TryGetDouble(out var d)).IsTrue();
        await Assert.That(d).IsEqualTo(1.5);
        await Assert.That(text.Kind).IsEqualTo(XdmItemKind.String);
        await Assert.That(text.TryGetString(out var s)).IsTrue();
        await Assert.That(s).IsEqualTo("abc");
        await Assert.That(boolean.IsNode).IsFalse();
        await Assert.That(boolean.TypeId).IsEqualTo(XdmTypeId.Boolean);
        await Assert.That(integer.TypeId).IsEqualTo(XdmTypeId.Integer);
        await Assert.That(number.TypeId).IsEqualTo(XdmTypeId.Double);
        await Assert.That(text.TypeId).IsEqualTo(XdmTypeId.String);
    }

    [Test]
    public async Task XdmItem_TryGetOfOtherKind_Fails()
    {
        var text = XdmItem.FromString("abc");

        await Assert.That(text.TryGetBoolean(out _)).IsFalse();
        await Assert.That(text.TryGetInteger(out _)).IsFalse();
        await Assert.That(text.TryGetDouble(out _)).IsFalse();
        await Assert.That(text.TryGetNode(out _)).IsFalse();
        await Assert.That(XdmItem.FromInteger(1).TryGetString(out _)).IsFalse();
    }

    [Test]
    public async Task XdmItem_FromNull_IsRejected() =>
        await Assert.That(() => XdmItem.FromString(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Limitation_DecimalAndDateTimeFactories_NotInContract()
    {
        // Q3 of #168: these members arrive with #185 together with its snapshot update.
        string[] deferred =
        [
            "FromDecimal",
            "FromDateTime",
            "FromDuration",
            "TryGetDecimal",
            "TryGetDateTime",
            "TryGetDuration",
        ];

        foreach (var name in deferred)
        {
            await Assert.That(typeof(XdmItem).GetMethod(name)).IsNull();
        }
    }

    [Test]
    public async Task XdmItem_FromNode_RoundTripsTheNodeAndItsAnnotation()
    {
        var source = new FakeNodeSource();
        var item = XdmItem.FromNode(new XPathNode(source.Attribute));

        await Assert.That(item.IsNode).IsTrue();
        await Assert.That(item.Kind).IsEqualTo(XdmItemKind.Node);
        await Assert.That(item.TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
        await Assert.That(item.TryGetNode(out var node)).IsTrue();
        await Assert.That(node.Node == source.Attribute).IsTrue();
        await Assert.That(() => XdmItem.FromNode(default)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task FakeNodeSource_UnannotatedNode_ReportsUntypedDefault()
    {
        var source = new FakeNodeSource();

        await Assert.That(new XPathNode(source.Document).TypeAnnotation).IsEqualTo(XdmTypeId.Untyped);
        await Assert.That(new XPathNode(source.Element).TypeAnnotation).IsEqualTo(XdmTypeId.Untyped);
        await Assert.That(new XPathNode(source.Attribute).TypeAnnotation).IsEqualTo(XdmTypeId.UntypedAtomic);
        await Assert.That(new XPathNode(source.Text).TypeAnnotation).IsEqualTo(XdmTypeId.UntypedAtomic);
    }

    [Test]
    public async Task XPathNode_IdentityAndDocumentOrder_DelegateToTheNodeSource()
    {
        var source = new FakeNodeSource();
        var element = new XPathNode(source.Element);
        var attribute = new XPathNode(source.Attribute);

        await Assert.That(element.IsSameNode(new XPathNode(source.Element))).IsTrue();
        await Assert.That(element.IsSameNode(attribute)).IsFalse();
        await Assert.That(element.CompareDocumentOrder(attribute)).IsLessThan(0);
        await Assert.That(attribute.CompareDocumentOrder(element)).IsGreaterThan(0);
        await Assert.That(element.Tree).IsSameReferenceAs(source);
        await Assert.That(element.Kind).IsEqualTo(XdmNodeKind.Element);
        await Assert.That(() => new XPathNode(default)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task XdmNodeKind_MirrorsXmlNodeKind()
    {
        var xml = Enum.GetNames<XmlNodeKind>();
        var xdm = Enum.GetNames<XdmNodeKind>();

        await Assert.That(xdm).IsEquivalentTo(xml);
        foreach (var value in Enum.GetValues<XmlNodeKind>())
        {
            await Assert.That((int)Enum.Parse<XdmNodeKind>(value.ToString())).IsEqualTo((int)value);
        }
    }

    [Test]
    public async Task XdmSequence_IsFlatAndSingletonEqualsItsItem()
    {
        var item = XdmItem.FromInteger(5);
        var single = XdmSequence.Create(item);
        var pair = XdmSequence.Create([item, XdmItem.FromString("x")]);

        await Assert.That(XdmSequence.Empty.IsEmpty).IsTrue();
        await Assert.That(default(XdmSequence).Count).IsEqualTo(0);
        await Assert.That(single.Count).IsEqualTo(1);
        await Assert.That(single[0].TryGetInteger(out var five)).IsTrue();
        await Assert.That(five).IsEqualTo(5L);
        await Assert.That(pair.Count).IsEqualTo(2);

        var kinds = new System.Collections.Generic.List<XdmItemKind>();
        foreach (var each in pair)
        {
            kinds.Add(each.Kind);
        }

        await Assert.That(kinds).IsEquivalentTo([XdmItemKind.Integer, XdmItemKind.String]);
        await Assert.That(() => pair[2]).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => XdmSequence.Empty[0]).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task XdmTypeId_Defaults_AreDistinctAndComparable()
    {
        XdmTypeId[] ids =
        [
            XdmTypeId.Untyped,
            XdmTypeId.UntypedAtomic,
            XdmTypeId.String,
            XdmTypeId.Boolean,
            XdmTypeId.Integer,
            XdmTypeId.Double,
        ];

        await Assert.That(new XdmTypeId(ids[0].Value) == ids[0]).IsTrue();
        await Assert.That(ids[0] != ids[1]).IsTrue();
        await Assert.That(ids[0].Equals((object)ids[0])).IsTrue();
        await Assert.That(ids[0].Equals("x")).IsFalse();
        await Assert.That(ids[0].GetHashCode()).IsEqualTo(ids[0].Value);
        await Assert.That(new System.Collections.Generic.HashSet<XdmTypeId>(ids).Count).IsEqualTo(ids.Length);
    }

    [Test]
    public async Task XPathQName_EqualityIsOrdinalOnBothParts()
    {
        var a = new XPathQName("urn:a", "x");

        await Assert.That(a == new XPathQName("urn:a", "x")).IsTrue();
        await Assert.That(a != new XPathQName("urn:b", "x")).IsTrue();
        await Assert.That(a.Equals((object)new XPathQName("urn:a", "X"))).IsFalse();
        await Assert.That(a.Equals("x")).IsFalse();
        await Assert.That(a.GetHashCode()).IsEqualTo(new XPathQName("urn:a", "x").GetHashCode());
        await Assert.That(a.ToString()).IsEqualTo("Q{urn:a}x");
        await Assert.That(default(XPathQName).NamespaceUri).IsEqualTo(string.Empty);
        await Assert.That(default(XPathQName).LocalName).IsEqualTo(string.Empty);
        await Assert.That(() => new XPathQName("urn:a", "")).Throws<ArgumentException>();
        await Assert.That(() => new XPathQName(null!, "x")).Throws<ArgumentNullException>();
    }
}
