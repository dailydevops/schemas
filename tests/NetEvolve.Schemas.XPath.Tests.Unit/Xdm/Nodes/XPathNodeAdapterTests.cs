namespace NetEvolve.Schemas.XPath.Tests.Unit.Xdm.Nodes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class XPathNodeAdapterTests
{
    private static readonly Uri DocumentUri = new("file:///data/doc.xml");

    private static XmlName Name(XmlTreeBuilder b, string local, string prefix = "", string uri = "") =>
        b.Names.Intern(prefix, local, uri);

    // <a id="1" p:lang="en"><b>t1</b><!--c--><?pi d?><b>t2<i>x</i>t3</b></a>
    private static XmlDocumentTree Build()
    {
        using var b = new XmlTreeBuilder(documentUri: DocumentUri);
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var a = Name(b, "a");
        b.StartElement(a, default);
        b.NamespaceDeclaration(b.Names.Intern("p"), b.Names.Intern("urn:p"));
        b.Attribute(Name(b, "id"), "1");
        b.Attribute(Name(b, "lang", "p", "urn:p"), "en");
        b.EndAttributes();
        var bn = Name(b, "b");
        b.StartElement(bn, default);
        b.EndAttributes();
        b.Text("t1", XmlTextKind.Text);
        b.EndElement(bn);
        b.Comment("c");
        b.ProcessingInstruction(b.Names.Intern("pi"), "d");
        b.StartElement(bn, default);
        b.EndAttributes();
        b.Text("t2", XmlTextKind.Text);
        var i = Name(b, "i");
        b.StartElement(i, default);
        b.EndAttributes();
        b.Text("x", XmlTextKind.Text);
        b.EndElement(i);
        b.Text("t3", XmlTextKind.Text);
        b.EndElement(bn);
        b.EndElement(a);
        b.EndDocument();
        return b.Complete();
    }

    private static List<XPathNode> Children(XPathNode node)
    {
        var list = new List<XPathNode>();
        for (var c = node.FirstChild(); !c.IsEmpty(); c = c.NextSibling())
        {
            list.Add(c);
        }

        return list;
    }

    [Test]
    public async Task Identity_SameNodeByTwoRoutes_IsEqual()
    {
        var tree = Build();
        var root = new XPathNode(tree.Document);
        var a = Children(root)[0];
        var byParent = Children(a)[0].Parent();
        var byTree = new XPathNode(new XmlNode(tree, a.Node.Index));

        await Assert.That(byParent.IsSameNode(a)).IsTrue();
        await Assert.That(byTree.IsSameNode(a)).IsTrue();
        await Assert.That(byTree == a).IsTrue();
    }

    [Test]
    public async Task Identity_EqualLookingNodes_AreUnequal()
    {
        var a = Children(Children(new XPathNode(Build().Document))[0]);
        var b2 = Children(Children(new XPathNode(Build().Document))[0]);

        await Assert.That(a[0].IsSameNode(b2[0])).IsFalse();
        await Assert.That(a[0].IsSameNode(a[3])).IsFalse();
        await Assert.That(a[0].StringValue()).IsEqualTo("t1");
    }

    [Test]
    public async Task Order_AttributesFollowElementAndPrecedeChildren()
    {
        var root = new XPathNode(Build().Document);
        var a = Children(root)[0];
        var id = a.FirstAttribute();
        var lang = id.NextAttribute();
        var firstChild = a.FirstChild();

        await Assert.That(a.CompareDocumentOrder(id)).IsLessThan(0);
        await Assert.That(id.CompareDocumentOrder(lang)).IsLessThan(0);
        await Assert.That(lang.CompareDocumentOrder(firstChild)).IsLessThan(0);
        await Assert.That(firstChild.CompareDocumentOrder(lang)).IsGreaterThan(0);
        await Assert.That(id.CompareDocumentOrder(id)).IsEqualTo(0);
        await Assert.That(lang.NextAttribute().IsEmpty()).IsTrue();
    }

    [Test]
    public async Task Order_DescendantsFollowAncestorsAndKeepSiblingOrder()
    {
        var a = Children(new XPathNode(Build().Document))[0];
        var kids = Children(a);
        var inner = Children(kids[3])[1];

        await Assert.That(kids[0].CompareDocumentOrder(kids[1])).IsLessThan(0);
        await Assert.That(kids[3].CompareDocumentOrder(inner)).IsLessThan(0);
        await Assert.That(inner.CompareDocumentOrder(kids[0])).IsGreaterThan(0);
        await Assert.That(a.CompareDocumentOrder(inner)).IsLessThan(0);
    }

    [Test]
    public async Task Order_DifferentTrees_IsStableAndAntisymmetric()
    {
        var first = new XPathNode(Build().Document);
        var second = new XPathNode(Build().Document);

        await Assert.That(first.CompareDocumentOrder(second)).IsLessThan(0);
        await Assert.That(second.CompareDocumentOrder(first)).IsGreaterThan(0);
    }

    [Test]
    public async Task StringValue_ForEachNodeKind()
    {
        var root = new XPathNode(Build().Document);
        var a = Children(root)[0];
        var kids = Children(a);

        await Assert.That(root.StringValue()).IsEqualTo("t1t2xt3");
        await Assert.That(a.StringValue()).IsEqualTo("t1t2xt3");
        await Assert.That(kids[3].StringValue()).IsEqualTo("t2xt3");
        await Assert.That(a.FirstAttribute().StringValue()).IsEqualTo("1");
        await Assert.That(kids[0].FirstChild().StringValue()).IsEqualTo("t1");
        await Assert.That(kids[1].StringValue()).IsEqualTo("c");
        await Assert.That(kids[2].StringValue()).IsEqualTo("d");
    }

    [Test]
    public async Task StringValueSpan_LeafKinds_SpanWithoutString()
    {
        var a = Children(new XPathNode(Build().Document))[0];
        var kids = Children(a);

        await Assert.That(a.TryGetStringValueSpan(out _)).IsFalse();
        await Assert.That(a.FirstAttribute().TryGetStringValueSpan(out var attribute)).IsTrue();
        await Assert.That(attribute.ToString()).IsEqualTo("1");
        await Assert.That(kids[1].TryGetStringValueSpan(out var comment)).IsTrue();
        await Assert.That(comment.ToString()).IsEqualTo("c");
        await Assert.That(kids[2].TryGetStringValueSpan(out var pi)).IsTrue();
        await Assert.That(pi.ToString()).IsEqualTo("d");
        await Assert.That(kids[0].FirstChild().TryGetStringValueSpan(out var text)).IsTrue();
        await Assert.That(text.ToString()).IsEqualTo("t1");
    }

    [Test]
    public async Task Accessors_NameParentChildrenAttributesBaseUri()
    {
        var tree = Build();
        var root = new XPathNode(tree.Document);
        var a = Children(root)[0];
        var lang = a.FirstAttribute().NextAttribute();
        var kids = Children(a);

        await Assert.That(tree.Names.GetString(a.Name().Local)).IsEqualTo("a");
        await Assert.That(tree.Names.GetString(lang.Name().Local)).IsEqualTo("lang");
        await Assert.That(tree.Names.GetString(lang.Name().NamespaceUri)).IsEqualTo("urn:p");
        await Assert.That(tree.Names.GetString(kids[2].Name().Local)).IsEqualTo("pi");
        await Assert.That(root.Parent().IsEmpty()).IsTrue();
        await Assert.That(a.Parent().IsSameNode(root)).IsTrue();
        await Assert.That(lang.Parent().IsSameNode(a)).IsTrue();
        await Assert.That(kids).Count().IsEqualTo(4);
        await Assert.That(kids[0].FirstAttribute().IsEmpty()).IsTrue();
        await Assert.That(a.BaseUri()).IsEqualTo(DocumentUri);
        await Assert.That(root.BaseUri()).IsEqualTo(DocumentUri);
    }

    [Test]
    public async Task TypedValue_Default_IsUntypedAtomicForElementsAttributesAndText()
    {
        var a = Children(new XPathNode(Build().Document))[0];
        var kids = Children(a);

        foreach (var node in new[] { a, a.FirstAttribute(), kids[0].FirstChild(), new XPathNode(a.Node.Tree!.Document) })
        {
            var item = node.TypedValue();
            await Assert.That(item.Kind).IsEqualTo(XdmItemKind.UntypedAtomic);
            await Assert.That(item.TypeId).IsEqualTo(XdmTypeId.UntypedAtomic);
            await Assert.That(item.TryGetUntypedAtomic(out var text)).IsTrue();
            await Assert.That(text).IsEqualTo(node.StringValue());
        }
    }

    [Test]
    public async Task TypedValue_CommentAndProcessingInstruction_AreStrings()
    {
        var kids = Children(Children(new XPathNode(Build().Document))[0]);

        await Assert.That(kids[1].TypedValue()).IsEqualTo(XdmItem.FromString("c"));
        await Assert.That(kids[2].TypedValue()).IsEqualTo(XdmItem.FromString("d"));
    }

    [Test]
    public async Task TypedValue_FakeAnnotation_FollowsTheSlot()
    {
        var a = Children(new XPathNode(Build().Document))[0];
        var id = a.FirstAttribute();
        var source = new FakeAnnotations(id, XdmTypeId.XsInteger, XdmItem.FromInteger(1));

        await Assert.That(id.TypeAnnotation(source)).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(id.TypedValue(source)).IsEqualTo(XdmItem.FromInteger(1));
        await Assert.That(a.TypeAnnotation(source)).IsEqualTo(XdmTypeId.Untyped);
        await Assert.That(a.TypedValue(source).Kind).IsEqualTo(XdmItemKind.UntypedAtomic);
    }

    [Test]
    public async Task FromUntypedAtomic_Null_Throws() =>
        await Assert.That(() => XdmItem.FromUntypedAtomic(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task FromUntypedAtomic_EqualityIgnoresStringInstance()
    {
        var left = XdmItem.FromUntypedAtomic("a");
        var right = XdmItem.FromUntypedAtomic(new string('a', 1));

        await Assert.That(left == right).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        await Assert.That(left == XdmItem.FromString("a")).IsFalse();
    }

    private sealed class FakeAnnotations(XPathNode annotated, XdmTypeId type, XdmItem value) : IXdmTypeAnnotationSource
    {
        public XdmTypeId GetTypeAnnotation(in XPathNode node) =>
            node.IsSameNode(annotated) ? type : node.TypeAnnotation;

        public bool TryGetTypedValue(in XPathNode node, out XdmItem typedValue)
        {
            typedValue = value;
            return node.IsSameNode(annotated);
        }
    }
}
