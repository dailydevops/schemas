namespace NetEvolve.Schemas.XPath.Tests.Unit.Xdm.Values;

using System;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class XdmAnyUriTests
{
    [Test]
    public async Task FromAnyUri_RoundTripsWithItsOwnKindAndType()
    {
        var item = XdmItem.FromAnyUri("urn:a");

        await Assert.That(item.Kind).IsEqualTo(XdmItemKind.AnyUri);
        await Assert.That(item.TypeId).IsEqualTo(XdmTypeId.AnyUri);
        await Assert.That(item.TryGetAnyUri(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo("urn:a");
        await Assert.That(item.TryGetString(out _)).IsFalse();
        await Assert.That(XdmItem.FromString("urn:a").TryGetAnyUri(out _)).IsFalse();
    }

    [Test]
    public async Task FromAnyUri_EqualityIsOrdinalAndDistinctFromString()
    {
        var left = XdmItem.FromAnyUri("a");
        var right = XdmItem.FromAnyUri(new string('a', 1));

        await Assert.That(left == right).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        await Assert.That(left == XdmItem.FromString("a")).IsFalse();
    }

    [Test]
    public async Task FromAnyUri_Null_Throws() =>
        await Assert.That(() => XdmItem.FromAnyUri(null!)).Throws<ArgumentNullException>();
}
