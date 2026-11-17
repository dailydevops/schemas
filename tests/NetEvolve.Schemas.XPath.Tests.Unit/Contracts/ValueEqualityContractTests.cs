namespace NetEvolve.Schemas.XPath.Tests.Unit.Contracts;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Tests.Unit.Fakes;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class ValueEqualityContractTests
{
    [Test]
    public async Task XdmItem_Equality_ComparesKindTypeAndValue()
    {
        var node = new XPathNode(new FakeNodeSource().Element);

        await Assert.That(XdmItem.FromInteger(1) == XdmItem.FromInteger(1)).IsTrue();
        await Assert.That(XdmItem.FromInteger(1) != XdmItem.FromInteger(2)).IsTrue();
        await Assert.That(XdmItem.FromInteger(1) == XdmItem.FromBoolean(true)).IsFalse();
        await Assert.That(XdmItem.FromString("a") == XdmItem.FromString(new string('a', 1))).IsTrue();
        await Assert.That(XdmItem.FromString("a") != XdmItem.FromString("b")).IsTrue();
        await Assert.That(XdmItem.FromDouble(double.NaN) == XdmItem.FromDouble(double.NaN)).IsTrue();
        await Assert.That(XdmItem.FromNode(node) == XdmItem.FromNode(node)).IsTrue();
        await Assert.That(XdmItem.FromNode(node).Equals((object)XdmItem.FromNode(node))).IsTrue();
        await Assert.That(XdmItem.FromNode(node).Equals("x")).IsFalse();
        await Assert.That(default(XdmItem) == default(XdmItem)).IsTrue();
        await Assert.That(XdmItem.FromString("a").GetHashCode()).IsEqualTo(XdmItem.FromString("a").GetHashCode());
        await Assert.That(XdmItem.FromInteger(7).GetHashCode()).IsEqualTo(XdmItem.FromInteger(7).GetHashCode());
        await Assert.That(XdmItem.FromNode(node).GetHashCode()).IsEqualTo(XdmItem.FromNode(node).GetHashCode());
    }

    [Test]
    public async Task XdmSequence_Equality_ComparesItemsInOrder()
    {
        var ab = XdmSequence.Create([XdmItem.FromInteger(1), XdmItem.FromString("b")]);
        var same = XdmSequence.Create([XdmItem.FromInteger(1), XdmItem.FromString("b")]);
        var other = XdmSequence.Create([XdmItem.FromString("b"), XdmItem.FromInteger(1)]);

        await Assert.That(ab == same).IsTrue();
        await Assert.That(ab != other).IsTrue();
        await Assert.That(ab == XdmSequence.Create(XdmItem.FromInteger(1))).IsFalse();
        await Assert.That(ab.Equals((object)same)).IsTrue();
        await Assert.That(ab.Equals("x")).IsFalse();
        await Assert.That(ab.GetHashCode()).IsEqualTo(same.GetHashCode());
        await Assert.That(XdmSequence.Empty == default).IsTrue();
    }

    [Test]
    public async Task XdmSequence_Enumerator_IsNotNestedAndDoesNotAllocate()
    {
        var enumerator = XdmSequence.Create([XdmItem.FromInteger(1), XdmItem.FromInteger(2)]).GetEnumerator();
        var sum = 0L;
        while (enumerator.MoveNext())
        {
            _ = enumerator.Current.TryGetInteger(out var value);
            sum += value;
        }

        await Assert.That(sum).IsEqualTo(3L);
        await Assert.That(typeof(XdmSequenceEnumerator).IsNested).IsFalse();
        await Assert.That(typeof(XdmSequenceEnumerator).IsByRefLike).IsTrue();
        await Assert.That(typeof(XdmSequence).GetNestedTypes().Length).IsEqualTo(0);
        await Assert.That(XdmSequence.Empty.GetEnumerator().MoveNext()).IsFalse();
    }

    [Test]
    public async Task XPathNode_Equality_IsNodeIdentity()
    {
        var source = new FakeNodeSource();
        var element = new XPathNode(source.Element);

        await Assert.That(element == new XPathNode(source.Element)).IsTrue();
        await Assert.That(element != new XPathNode(source.Text)).IsTrue();
        await Assert.That(element.Equals((object)new XPathNode(source.Element))).IsTrue();
        await Assert.That(element.Equals("x")).IsFalse();
        await Assert.That(element.GetHashCode()).IsEqualTo(new XPathNode(source.Element).GetHashCode());
    }

    [Test]
    public async Task XPathRegexGroups_Equality_ComparesSubjectAndRanges()
    {
        var a = new XPathRegexGroups("abcdef", [new TextRange(1, 3), new TextRange(2, 1)]);
        var same = new XPathRegexGroups("abcdef", [new TextRange(1, 3), new TextRange(2, 1)]);

        await Assert.That(a == same).IsTrue();
        await Assert.That(a != new XPathRegexGroups("abcdef", [new TextRange(1, 3), new TextRange(2, 2)])).IsTrue();
        await Assert.That(a != new XPathRegexGroups("abcdeg", [new TextRange(1, 3), new TextRange(2, 1)])).IsTrue();
        await Assert.That(a != new XPathRegexGroups("abcdef", [new TextRange(1, 3)])).IsTrue();
        await Assert.That(a.Equals((object)same)).IsTrue();
        await Assert.That(a.Equals("x")).IsFalse();
        await Assert.That(a.GetHashCode()).IsEqualTo(same.GetHashCode());
        await Assert.That(default(XPathRegexGroups) == default(XPathRegexGroups)).IsTrue();
    }

    [Test]
    public async Task Results_Equality_ComparesAllParts()
    {
        var diagnostic = XPathDiagnostic.Create(XPathErrorCodes.XPST0003);
        var ok = new XPathResult(XPathEvaluationStatus.Ok, XdmSequence.Create(XdmItem.FromInteger(1)), default);
        var sameOk = new XPathResult(XPathEvaluationStatus.Ok, XdmSequence.Create(XdmItem.FromInteger(1)), default);
        var failed = new XPathResult(XPathEvaluationStatus.Error, XdmSequence.Empty, diagnostic);
        var compile = new XPathCompileResult(null, diagnostic);

        await Assert.That(ok == sameOk).IsTrue();
        await Assert.That(ok != failed).IsTrue();
        await Assert.That(ok.Equals((object)sameOk)).IsTrue();
        await Assert.That(ok.Equals("x")).IsFalse();
        await Assert.That(ok.GetHashCode()).IsEqualTo(sameOk.GetHashCode());
        await Assert.That(compile == new XPathCompileResult(null, diagnostic)).IsTrue();
        await Assert.That(compile != new XPathCompileResult(null, default)).IsTrue();
        await Assert.That(compile.Equals((object)new XPathCompileResult(null, diagnostic))).IsTrue();
        await Assert.That(compile.Equals("x")).IsFalse();
        await Assert.That(compile.GetHashCode()).IsEqualTo(new XPathCompileResult(null, diagnostic).GetHashCode());
    }

    [Test]
    public async Task StaticContextBuilder_BaseUri_IsAUriAndCanBeCleared()
    {
        var uri = new Uri("urn:base");
        var context = XPathStaticContext.Default.ToBuilder().WithBaseUri(uri).Build();
        var relative = XPathStaticContext.Default.ToBuilder().WithBaseUri(new Uri("a/b", UriKind.Relative)).Build();
        var cleared = context.ToBuilder().WithBaseUri(null).Build();

        await Assert.That(context.BaseUri).IsEqualTo(uri);
        await Assert.That(relative.BaseUri!.IsAbsoluteUri).IsFalse();
        await Assert.That(cleared.BaseUri).IsNull();
    }
}
