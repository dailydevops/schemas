namespace NetEvolve.Schemas.XPath.Tests.Unit.Errors;

using System;
using NetEvolve.Schemas.XPath.Errors;

public class XPathErrorCodeTests
{
    [Test]
    public async Task Constructor_LocalNameOnly_UsesStandardErrorNamespace()
    {
        var code = new XPathErrorCode("XPST0003");

        await Assert.That(code.Namespace).IsEqualTo("http://www.w3.org/2005/xqt-errors");
        await Assert.That(code.LocalName).IsEqualTo("XPST0003");
        await Assert.That(code.ToString()).IsEqualTo("XPST0003");
    }

    [Test]
    public async Task Equals_SameLocalNameInDifferentNamespaces_AreNotEqual()
    {
        var standard = new XPathErrorCode("XPST0003");
        var custom = new XPathErrorCode("urn:example:errors", "XPST0003");

        await Assert.That(standard.Equals(custom)).IsFalse();
        await Assert.That(standard == custom).IsFalse();
        await Assert.That(standard != custom).IsTrue();
        await Assert.That(custom.ToString()).IsEqualTo("{urn:example:errors}XPST0003");
    }

    [Test]
    public async Task Equals_SameNamespaceAndLocalName_AreEqualWithSameHashCode()
    {
        var left = new XPathErrorCode("XPST0003");
        var right = new XPathErrorCode(XPathErrorCode.StandardNamespace, "XPST0003");

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.Equals((object)right)).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        await Assert.That(left.Equals((object?)null)).IsFalse();
    }

    [Test]
    public async Task Default_HasEmptyParts()
    {
        var code = default(XPathErrorCode);

        await Assert.That(code.Namespace).IsEmpty();
        await Assert.That(code.LocalName).IsEmpty();
    }

    [Test]
    public async Task Constructor_NullOrEmptyArguments_Throw()
    {
        await Assert.That(() => new XPathErrorCode(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new XPathErrorCode(null!, "X")).Throws<ArgumentNullException>();
        await Assert.That(() => new XPathErrorCode("urn:x", null!)).Throws<ArgumentNullException>();
        await Assert.That(() => new XPathErrorCode(string.Empty)).Throws<ArgumentException>();
    }
}
