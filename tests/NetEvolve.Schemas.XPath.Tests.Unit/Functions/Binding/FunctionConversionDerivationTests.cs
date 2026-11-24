namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Binding;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The derivation of the built-in types in function conversion (XPath 2.0, 2.5.4): an item keeps its own type when that type is the
/// expected one or derives from it. The expected types are the item types of a function signature.
/// </summary>
public class FunctionConversionDerivationTests
{
    private static XPathParameterType Type(XPathItemType type, Occurrence occurrence = Occurrence.ExactlyOne) =>
        new(type, occurrence);

    private static XdmSequence One(in XdmItem item) => XdmSequence.Create(item);

    private static XdmItem Int => XdmItem.FromInteger(5).WithTypeId(XdmTypes.Get("int"));

    private static XdmItem Token => XdmItem.FromString("a b").WithTypeId(XdmTypes.Get("token"));

    [Test]
    [Arguments(nameof(XPathItemType.Integer))]
    [Arguments(nameof(XPathItemType.Decimal))]
    [Arguments(nameof(XPathItemType.Numeric))]
    [Arguments(nameof(XPathItemType.AnyAtomic))]
    public async Task DerivedInteger_IsKeptForEveryBaseType(string targetName)
    {
        var item = One(Int);

        await BindingHarness.AssertConverts(Type(Enum.Parse<XPathItemType>(targetName)), item, item);
    }

    [Test]
    [Arguments(nameof(XPathItemType.String))]
    [Arguments(nameof(XPathItemType.AnyAtomic))]
    public async Task DerivedString_IsKeptForEveryBaseType(string targetName)
    {
        var item = One(Token);

        await BindingHarness.AssertConverts(Type(Enum.Parse<XPathItemType>(targetName)), item, item);
    }

    [Test]
    [Arguments(nameof(XPathItemType.Double))]
    [Arguments(nameof(XPathItemType.Boolean))]
    [Arguments(nameof(XPathItemType.AnyUri))]
    [Arguments(nameof(XPathItemType.UntypedAtomic))]
    public async Task DerivedString_IsNoSubtypeOfAnUnrelatedType_RaisesXPTY0004(string targetName) =>
        await BindingHarness.AssertFails(Type(Enum.Parse<XPathItemType>(targetName)), One(Token), "XPTY0004");

    [Test]
    public async Task DerivedInteger_IsPromotedToDouble() =>
        await BindingHarness.AssertConverts(Type(XPathItemType.Double), One(Int), One(XdmItem.FromDouble(5)));

    [Test]
    public async Task DerivedInteger_IsNoString_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(Type(XPathItemType.String), One(Int), "XPTY0004");

    [Test]
    public async Task Decimal_IsNoSubtypeOfTheDerivedInteger_RaisesXPTY0004() =>
        await BindingHarness.AssertFails(
            Type(XPathItemType.Integer),
            One(XdmItem.FromDecimal(new XsdDecimal(15, 1))),
            "XPTY0004"
        );
}
