namespace NetEvolve.Schemas.XPath.Tests.Unit.Contracts;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class HostSlotContractTests
{
    [Test]
    public async Task HostSlots_AreEmptyByDefault()
    {
        var slots = new XPathHostSlots();

        await Assert.That(slots.CurrentItem).IsNull();
        await Assert.That(slots.CurrentGroup).IsNull();
        await Assert.That(slots.RegexGroups).IsNull();
        await Assert.That(slots.DecimalFormats).IsNull();
        await Assert.That(slots.Keys).IsNull();
    }

    [Test]
    public async Task HostSlots_CurrentItemAndCurrentGroup_RoundTrip()
    {
        var slots = new XPathHostSlots
        {
            CurrentItem = XdmItem.FromString("current"),
            CurrentGroup = XdmSequence.Create(XdmItem.FromInteger(1)),
        };

        await Assert.That(slots.CurrentItem!.Value.TryGetString(out var text)).IsTrue();
        await Assert.That(text).IsEqualTo("current");
        await Assert.That(slots.CurrentGroup!.Value.Count).IsEqualTo(1);
    }

    [Test]
    public async Task HostSlots_RegexGroups_RoundTripByIndex()
    {
        var groups = new XPathRegexGroups("abcdef", [new TextRange(1, 3), new TextRange(2, 1)]);
        var slots = new XPathHostSlots { RegexGroups = groups };

        await Assert.That(slots.RegexGroups!.Value.Count).IsEqualTo(2);
        await Assert.That(slots.RegexGroups!.Value.Subject).IsEqualTo("abcdef");
        await Assert.That(slots.RegexGroups!.Value.GetGroup(0).ToString()).IsEqualTo("bcd");
        await Assert.That(slots.RegexGroups!.Value.GetGroup(1).ToString()).IsEqualTo("c");
        await Assert.That(default(XPathRegexGroups).Count).IsEqualTo(0);
        await Assert.That(default(XPathRegexGroups).Subject).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task HostSlots_DecimalFormatsAndKeys_RoundTripThroughProviders()
    {
        var format = new XPathDecimalFormat { DecimalSeparator = ',', GroupingSeparator = '.' };
        var slots = new XPathHostSlots { DecimalFormats = new FakeDecimalFormats(format), Keys = new FakeKeys() };
        var name = new XPathQName("", "k");

        await Assert.That(slots.DecimalFormats!.TryGetDecimalFormat(name, out var found)).IsTrue();
        await Assert.That(found).IsSameReferenceAs(format);
        await Assert.That(slots.Keys!.TryGetNodes(name, XdmItem.FromString("v"), out var nodes)).IsTrue();
        await Assert.That(nodes.IsEmpty).IsTrue();
        await Assert.That(new XPathDecimalFormat().DecimalSeparator).IsEqualTo('.');
    }

    [Test]
    public async Task HostSlots_Clear_EmptiesEverySlot()
    {
        var slots = new XPathHostSlots { CurrentItem = XdmItem.FromBoolean(true), Keys = new FakeKeys() };

        slots.Clear();

        await Assert.That(slots.CurrentItem).IsNull();
        await Assert.That(slots.Keys).IsNull();
    }

    private sealed class FakeDecimalFormats(XPathDecimalFormat format) : IXPathDecimalFormatProvider
    {
        public bool TryGetDecimalFormat(XPathQName name, out XPathDecimalFormat? decimalFormat)
        {
            decimalFormat = format;
            return true;
        }
    }

    private sealed class FakeKeys : IXPathKeyProvider
    {
        public bool TryGetNodes(XPathQName name, in XdmItem value, out XdmSequence nodes)
        {
            nodes = XdmSequence.Empty;
            return true;
        }
    }
}
